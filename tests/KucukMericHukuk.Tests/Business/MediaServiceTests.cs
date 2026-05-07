using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Media;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KucukMericHukuk.Tests.Business;

public class MediaServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<MediaUploadInputDto> _uploadValidator = new MediaUploadInputValidator();
    private readonly IValidator<MediaUpdateInputDto> _updateValidator = new MediaUpdateInputValidator();

    public MediaServiceTests()
    {
        _factory = new TestDbContextFactory();
        var config = new TypeAdapterConfig();
        config.Scan(typeof(MediaFileMappingConfig).Assembly);
        _mapper = new Mapper(config);
    }

    private static Mock<IFileStorageService> NewStorageMock()
    {
        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Stream _, string p, CancellationToken _) => p);
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        storage.Setup(s => s.GetPublicUrl(It.IsAny<string>()))
               .Returns<string>(p => "/" + p);
        return storage;
    }

    private static Mock<IImageProcessor> NewProcessorMock(int width = 800, int height = 600)
    {
        var processor = new Mock<IImageProcessor>();
        processor.Setup(p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(() => new ProcessedImageResult
                 {
                     MainImage = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }),
                     Thumbnail = new MemoryStream(new byte[] { 9, 8, 7 }),
                     Width = width,
                     Height = height,
                 });
        return processor;
    }

    private MediaService CreateSut(AppDbContext context, Mock<IFileStorageService> storage, Mock<IImageProcessor> processor)
    {
        var uow = new UnitOfWork(context);
        return new MediaService(
            uow,
            storage.Object,
            processor.Object,
            _mapper,
            _uploadValidator,
            _updateValidator,
            NullLogger<MediaService>.Instance);
    }

    private static MediaUploadInputDto BuildUpload(
        string contentType = "image/jpeg",
        long size = 1024,
        string fileName = "test.jpg",
        byte[]? content = null)
    {
        var bytes = content ?? new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // JPEG magic
        return new MediaUploadInputDto
        {
            FileStream = new MemoryStream(bytes),
            ContentType = contentType,
            FileSizeBytes = size,
            OriginalFileName = fileName,
            AltText = "alt",
        };
    }

    [Fact]
    public async Task UploadAsync_ValidFile_Success()
    {
        await using var context = _factory.CreateContext();
        var storage = NewStorageMock();
        var processor = NewProcessorMock();
        var sut = CreateSut(context, storage, processor);

        var result = await sut.UploadAsync(BuildUpload());

        result.IsSuccess.Should().BeTrue();
        result.Value.Width.Should().Be(800);
        result.Value.Height.Should().Be(600);
        result.Value.ContentType.Should().Be("image/webp");
        result.Value.Url.Should().StartWith("/uploads/");
        result.Value.ThumbnailUrl.Should().StartWith("/uploads/").And.Contain("_thumb");

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        await using var verify = _factory.CreateContext();
        verify.Set<MediaFile>().Should().HaveCount(1);
    }

    [Fact]
    public async Task UploadAsync_DuplicateHash_ReturnsExistingNoDiskWrite()
    {
        await using var context = _factory.CreateContext();
        var storage = NewStorageMock();
        var processor = NewProcessorMock();
        var sut = CreateSut(context, storage, processor);

        var content = new byte[] { 0xAA, 0xBB, 0xCC };
        var first = await sut.UploadAsync(BuildUpload(content: content));
        first.IsSuccess.Should().BeTrue();

        // İkinci upload (aynı içerik = aynı hash)
        storage.Invocations.Clear();
        processor.Invocations.Clear();

        var second = await sut.UploadAsync(BuildUpload(content: content, fileName: "second.jpg"));

        second.IsSuccess.Should().BeTrue();
        second.Value.Id.Should().Be(first.Value.Id);

        // Dedup: işlem ve kaydetme tekrar çağrılmaz
        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        processor.Verify(p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadAsync_FileTooLarge_Failure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context, NewStorageMock(), NewProcessorMock());

        var input = BuildUpload(size: 11 * 1024 * 1024); // 11 MB

        var result = await sut.UploadAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Field == nameof(MediaUploadInputDto.FileSizeBytes) &&
            e.Message.Contains("10 MB"));
    }

    [Fact]
    public async Task UploadAsync_UnsupportedMime_Failure()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context, NewStorageMock(), NewProcessorMock());

        var input = BuildUpload(contentType: "application/pdf");

        var result = await sut.UploadAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Field == nameof(MediaUploadInputDto.ContentType) &&
            e.Message.Contains("JPEG"));
    }

    [Fact]
    public async Task UploadAsync_ProcessingFails_StorageNotCalled()
    {
        await using var context = _factory.CreateContext();
        var storage = NewStorageMock();
        var processor = new Mock<IImageProcessor>();
        processor.Setup(p => p.ProcessAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("decode fail"));

        var sut = CreateSut(context, storage, processor);

        var result = await sut.UploadAsync(BuildUpload());

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Media.ProcessingFailed);

        storage.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        await using var verify = _factory.CreateContext();
        verify.Set<MediaFile>().Should().BeEmpty();
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesFromDbAndDisk()
    {
        await using var context = _factory.CreateContext();
        var storage = NewStorageMock();
        var processor = NewProcessorMock();
        var sut = CreateSut(context, storage, processor);

        var upload = await sut.UploadAsync(BuildUpload());
        upload.IsSuccess.Should().BeTrue();
        var id = upload.Value.Id;

        // Önce soft-delete (HardDelete soft-deleted kaydı da bulur)
        await sut.DeleteAsync(id);

        storage.Invocations.Clear();

        var hardDelete = await sut.HardDeleteAsync(id);

        hardDelete.IsSuccess.Should().BeTrue();

        // Hem main hem thumb için DeleteAsync çağrıldı
        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        await using var verify = _factory.CreateContext();
        verify.Set<MediaFile>().IgnoreQueryFilters().Any(m => m.Id == id).Should().BeFalse();
    }

    public void Dispose() => _factory.Dispose();
}
