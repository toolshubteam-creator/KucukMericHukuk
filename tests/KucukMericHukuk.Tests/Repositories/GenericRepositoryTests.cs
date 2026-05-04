using FluentAssertions;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.DataAccess.Repositories;
using KucukMericHukuk.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Repositories;

public class GenericRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public GenericRepositoryTests()
    {
        _factory = new TestDbContextFactory();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntity()
    {
        await using var context = _factory.CreateContext();
        var repo = new GenericRepository<Page>(context);

        var page = new Page { PageKey = "test-page", IsSystem = false };

        await repo.AddAsync(page);
        await context.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync(page.Id);
        fetched.Should().NotBeNull();
        fetched!.PageKey.Should().Be("test-page");
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteEntity_AndHideFromQueries()
    {
        await using var context = _factory.CreateContext();
        var repo = new GenericRepository<Page>(context);

        var page = new Page { PageKey = "soft-test", IsSystem = false };
        await repo.AddAsync(page);
        await context.SaveChangesAsync();

        repo.Delete(page);
        await context.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync(page.Id);
        fetched.Should().BeNull();

        var withDeleted = await repo.QueryWithDeleted().ToListAsync();
        withDeleted.Should().HaveCount(1);
        withDeleted[0].IsDeleted.Should().BeTrue();
        withDeleted[0].DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HardDelete_ShouldRemoveEntityCompletely()
    {
        await using var context = _factory.CreateContext();
        var repo = new GenericRepository<Page>(context);

        var page = new Page { PageKey = "hard-test", IsSystem = false };
        await repo.AddAsync(page);
        await context.SaveChangesAsync();

        repo.HardDelete(page);
        await context.SaveChangesAsync();

        var withDeleted = await repo.QueryWithDeleted().ToListAsync();
        withDeleted.Should().BeEmpty();
    }

    [Fact]
    public async Task Restore_ShouldUndoSoftDelete()
    {
        await using var context = _factory.CreateContext();
        var repo = new GenericRepository<Page>(context);

        var page = new Page { PageKey = "restore-test", IsSystem = false };
        await repo.AddAsync(page);
        await context.SaveChangesAsync();

        repo.Delete(page);
        await context.SaveChangesAsync();

        var deleted = (await repo.QueryWithDeleted().ToListAsync()).First();
        repo.Restore(deleted);
        await context.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync(page.Id);
        fetched.Should().NotBeNull();
        fetched!.IsDeleted.Should().BeFalse();
        fetched.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage()
    {
        await using var context = _factory.CreateContext();
        var repo = new GenericRepository<Page>(context);

        for (int i = 1; i <= 25; i++)
        {
            await repo.AddAsync(new Page { PageKey = $"page-{i:D2}", IsSystem = false });
        }
        await context.SaveChangesAsync();

        var page2 = await repo.GetPagedAsync(2, 10);

        page2.TotalCount.Should().Be(25);
        page2.PageNumber.Should().Be(2);
        page2.PageSize.Should().Be(10);
        page2.TotalPages.Should().Be(3);
        page2.Items.Should().HaveCount(10);
        page2.HasPrevious.Should().BeTrue();
        page2.HasNext.Should().BeTrue();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
