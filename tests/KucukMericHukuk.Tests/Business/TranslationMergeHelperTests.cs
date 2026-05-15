using FluentAssertions;
using KucukMericHukuk.Business.Helpers;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.1.2 — TranslationMergeHelper unit testleri. EF/DbContext'ten bağımsız;
/// helper saf in-memory koleksiyon manipülasyonu üzerinde doğrulanır.
/// </summary>
public class TranslationMergeHelperTests
{
    private sealed class FakeTranslation : ITranslation
    {
        public int Id { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Payload { get; set; } = string.Empty;
    }

    private static void Copy(FakeTranslation target, FakeTranslation source)
        => target.Payload = source.Payload;

    [Fact]
    public void Merge_EmptyExisting_AllIncomingAdded()
    {
        var existing = new List<FakeTranslation>();
        var incoming = new[]
        {
            new FakeTranslation { LanguageCode = "tr-TR", Payload = "TR" },
            new FakeTranslation { LanguageCode = "en-US", Payload = "EN" },
        };

        TranslationMergeHelper.Merge(existing, incoming, Copy);

        existing.Should().HaveCount(2);
        existing.Select(e => e.LanguageCode).Should().BeEquivalentTo(new[] { "tr-TR", "en-US" });
    }

    [Fact]
    public void Merge_MatchingLanguage_PreservesIdAndCreatedAt_UpdatesPayload()
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = new List<FakeTranslation>
        {
            new() { Id = 42, LanguageCode = "tr-TR", Payload = "eski", CreatedAt = createdAt }
        };
        var incoming = new[]
        {
            new FakeTranslation { Id = 0, LanguageCode = "tr-TR", Payload = "yeni", CreatedAt = DateTime.UtcNow }
        };

        TranslationMergeHelper.Merge(existing, incoming, Copy);

        existing.Should().HaveCount(1);
        existing[0].Id.Should().Be(42, "mevcut translation Id'si KORUNMALI");
        existing[0].CreatedAt.Should().Be(createdAt, "mevcut translation CreatedAt'i KORUNMALI");
        existing[0].Payload.Should().Be("yeni", "scalar field güncellenmeli");
    }

    [Fact]
    public void Merge_MissingLanguageInIncoming_RemovedFromExisting()
    {
        var existing = new List<FakeTranslation>
        {
            new() { Id = 1, LanguageCode = "tr-TR", Payload = "TR" },
            new() { Id = 2, LanguageCode = "en-US", Payload = "EN" },
        };
        var incoming = new[]
        {
            new FakeTranslation { LanguageCode = "tr-TR", Payload = "TR güncel" }
        };

        TranslationMergeHelper.Merge(existing, incoming, Copy);

        existing.Should().HaveCount(1);
        existing[0].LanguageCode.Should().Be("tr-TR");
        existing[0].Id.Should().Be(1);
        existing[0].Payload.Should().Be("TR güncel");
    }

    [Fact]
    public void Merge_MixedAddUpdateRemove_HandlesAllThreeOperations()
    {
        var existing = new List<FakeTranslation>
        {
            new() { Id = 1, LanguageCode = "tr-TR", Payload = "TR eski" },
            new() { Id = 2, LanguageCode = "en-US", Payload = "EN eski" }, // bu silinecek
        };
        var incoming = new[]
        {
            new FakeTranslation { LanguageCode = "tr-TR", Payload = "TR yeni" },    // update
            new FakeTranslation { LanguageCode = "de-DE", Payload = "DE yeni" },    // add
        };

        TranslationMergeHelper.Merge(existing, incoming, Copy);

        existing.Should().HaveCount(2);
        existing.Single(e => e.LanguageCode == "tr-TR").Id.Should().Be(1, "update — Id korunur");
        existing.Single(e => e.LanguageCode == "tr-TR").Payload.Should().Be("TR yeni");
        existing.Single(e => e.LanguageCode == "de-DE").Payload.Should().Be("DE yeni");
        existing.Should().NotContain(e => e.LanguageCode == "en-US", "incoming'te yok — kaldırıldı");
    }

    [Fact]
    public void Merge_LanguageCodeMatch_IsCaseInsensitive()
    {
        var existing = new List<FakeTranslation>
        {
            new() { Id = 1, LanguageCode = "tr-TR", Payload = "eski" }
        };
        var incoming = new[]
        {
            new FakeTranslation { LanguageCode = "TR-tr", Payload = "yeni" }
        };

        TranslationMergeHelper.Merge(existing, incoming, Copy);

        existing.Should().HaveCount(1);
        existing[0].Id.Should().Be(1, "case-insensitive match — Id korunur");
        existing[0].Payload.Should().Be("yeni");
    }

    [Fact]
    public void Merge_EmptyIncoming_ClearsExisting()
    {
        var existing = new List<FakeTranslation>
        {
            new() { Id = 1, LanguageCode = "tr-TR" },
            new() { Id = 2, LanguageCode = "en-US" },
        };

        TranslationMergeHelper.Merge(existing, Array.Empty<FakeTranslation>(), Copy);

        existing.Should().BeEmpty();
    }

    [Fact]
    public void Merge_NullArguments_Throws()
    {
        var list = new List<FakeTranslation>();
        var ok = Array.Empty<FakeTranslation>();
        Action a1 = () => TranslationMergeHelper.Merge<FakeTranslation>(null!, ok, Copy);
        Action a2 = () => TranslationMergeHelper.Merge(list, null!, Copy);
        Action a3 = () => TranslationMergeHelper.Merge(list, ok, null!);
        a1.Should().Throw<ArgumentNullException>();
        a2.Should().Throw<ArgumentNullException>();
        a3.Should().Throw<ArgumentNullException>();
    }
}
