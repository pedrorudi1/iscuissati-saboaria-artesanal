using Iscuissatis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Iscuissatis.Web.Infrastructure;

public static class DatabaseMigration
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Aplica as migrations pendentes no start. Em Docker/Coolify o banco pode
    /// demorar alguns segundos para aceitar conexões, por isso as tentativas.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseMigration));
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations aplicadas com sucesso.");
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(exception, "Banco indisponível (tentativa {Attempt}/{MaxAttempts}).", attempt, MaxAttempts);
                await Task.Delay(Delay);
            }
        }
    }
}
