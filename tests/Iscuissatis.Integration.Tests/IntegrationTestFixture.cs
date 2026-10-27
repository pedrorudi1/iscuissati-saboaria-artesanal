using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Iscuissatis.Integration.Tests;

/// <summary>
/// Sobe um PostgreSQL real (Testcontainers) por coleção de testes e uma
/// aplicação Web apontando para ele. As migrations rodam no start da app.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("iscuissatis_tests")
        .WithUsername("iscuissatis")
        .WithPassword("tests-only")
        .Build();

    public const string AdminEmail = "admin@teste.local";

    /// <summary>Pasta de uploads isolada por execução do conjunto de testes.</summary>
    public static string StorageRoot { get; } =
        Path.Combine(Path.GetTempPath(), "iscuissatis-tests", Guid.NewGuid().ToString("N"));
    public const string AdminPassword = "SenhaDeTeste123";

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string ConnectionString => _database.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("Security:UseHttpsRedirection", "false");
            builder.UseSetting("Storage:RootPath", StorageRoot);
            builder.UseSetting("Storage:PublicPrefix", "/media");
            builder.UseSetting("Seed:AdminEmail", AdminEmail);
            builder.UseSetting("Seed:AdminPassword", AdminPassword);
            builder.UseSetting("Seed:AdminFullName", "Administrador de Teste");
        });

        // Dispara o start do host (e as migrations) antes do primeiro teste.
        using var client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "integration";
}
