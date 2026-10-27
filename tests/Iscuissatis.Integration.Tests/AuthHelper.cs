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

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}
