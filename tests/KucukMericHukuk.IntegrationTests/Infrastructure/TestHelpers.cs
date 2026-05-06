using System.Text.RegularExpressions;

namespace KucukMericHukuk.IntegrationTests.Infrastructure;

public static class TestHelpers
{
    private static readonly Regex AntiForgeryRegex = new(
        @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
        RegexOptions.Compiled);

    public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = AntiForgeryRegex.Match(html);
        if (!match.Success)
            throw new InvalidOperationException(
                $"AntiForgery token not found in {url}");
        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email = IntegrationTestFactory.AdminEmail,
        string password = IntegrationTestFactory.AdminPassword)
    {
        var token = await GetAntiForgeryTokenAsync(client, "/admin/account/login");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        return await client.PostAsync("/admin/account/login", content);
    }
}
