using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Iscuissatis.Integration.Tests;

/// <summary>
/// Faz login de verdade: busca o formulário, extrai o token antiforgery e posta.
/// Assim os testes exercitam o mesmo caminho do navegador.
/// </summary>
public static partial class AuthHelper
{
    public static async Task<HttpClient> SignInAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var loginPage = await client.GetAsync("/entrar");
        loginPage.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await loginPage.Content.ReadAsStringAsync();
        var token = AntiforgeryToken().Match(html).Groups["token"].Value;
        token.ShouldNotBeNullOrWhiteSpace();

        var response = await client.PostAsync("/entrar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
        }));

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        return client;
    }


    public static async Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        HttpClient client,
        string pageUrl,
        string postUrl,
        IDictionary<string, string> fields)
    {
        var page = await client.GetAsync(pageUrl);
        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        var token = AntiforgeryToken().Match(await page.Content.ReadAsStringAsync()).Groups["token"].Value;
        token.ShouldNotBeNullOrWhiteSpace();

        fields["__RequestVerificationToken"] = token;
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
    }

    public static HttpClient CreateBrowser(Func<HttpClient> factory)
        => factory();


    public static async Task<HttpResponseMessage> PostMultipartAsync(
        HttpClient client,
        string pageUrl,
        string postUrl,
        string fileField,
        string fileName,
        byte[] fileContent,
        string contentType,
        IDictionary<string, string> extraFields)
    {
        var page = await client.GetAsync(pageUrl);
        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        var token = AntiforgeryToken().Match(await page.Content.ReadAsStringAsync()).Groups["token"].Value;
        token.ShouldNotBeNullOrWhiteSpace();

        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
        };

        foreach (var (key, value) in extraFields)
        {
            form.Add(new StringContent(value), key);
        }

        var file = new ByteArrayContent(fileContent);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, fileField, fileName);

        return await client.PostAsync(postUrl, form);
    }

    /// <summary>O middleware usa redirect relativo; o MVC usa absoluto. Normaliza para o caminho.</summary>
    public static string PathOf(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        location.ShouldNotBeNull();
        return location!.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString.Split('?')[0];
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}
