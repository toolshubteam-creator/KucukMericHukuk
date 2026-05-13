namespace KucukMericHukuk.Infrastructure.Initialization;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string? AdminFullName { get; set; }

    public bool SeedDemoContent { get; set; } = false;

    /// <summary>
    /// Production environment'ta admin seed alanları zorunludur. Eksikse startup fail.
    /// Dev'de kullanıcı sessiz warn ile devam edebilir (DbInitializer.SeedAdminUserAsync skip).
    /// Çağrı: Program.cs'te <c>app.Environment.IsProduction()</c> kontrolünden sonra.
    /// </summary>
    public void ValidateForProduction()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(AdminEmail)) missing.Add("Seed__AdminEmail");
        if (string.IsNullOrWhiteSpace(AdminPassword)) missing.Add("Seed__AdminPassword");
        if (string.IsNullOrWhiteSpace(AdminFullName)) missing.Add("Seed__AdminFullName");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Production environment'ta şu environment variable'lar zorunludur: " +
                string.Join(", ", missing) +
                ". appsettings.Production.json secret içermez; bu değerler env-var ile set edilir.");
        }
    }
}
