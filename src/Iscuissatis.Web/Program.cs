using Iscuissatis.Infrastructure.Persistence;
using Iscuissatis.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não configurada. Veja .env.example.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Atrás do Cloudflare Tunnel o TLS termina no Cloudflare; redirecionar aqui
// criaria loop. Desligue com Security:UseHttpsRedirection=false no container.
if (app.Configuration.GetValue("Security:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await app.ApplyMigrationsAsync();
}

await app.RunAsync();

public partial class Program;
