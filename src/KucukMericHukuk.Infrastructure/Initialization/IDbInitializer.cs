namespace KucukMericHukuk.Infrastructure.Initialization;

public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
