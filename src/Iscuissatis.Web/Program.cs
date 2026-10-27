using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Application.Catalog;
using Iscuissatis.Application.Catalog.Public;
using Iscuissatis.Application.Sales;
using Iscuissatis.Application.Storage;
using Iscuissatis.Infrastructure.Persistence;
using Iscuissatis.Infrastructure.Persistence.Repositories;
using Iscuissatis.Infrastructure.Integrations;
using Iscuissatis.Infrastructure.Storage;
using System.Globalization;
using Iscuissatis.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não configurada. Veja .env.example.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/entrar";
    options.LogoutPath = "/sair";
    options.AccessDeniedPath = "/acesso-negado";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Usuário autenticado sem o papel exigido recebe 403 (e não um redirect),
    // para distinguir "não logado" (302 para /entrar) de "sem permissão".
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ICatalogQueryService, CatalogQueryService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddHttpClient<IZipCodeLookup, ViaCepLookup>(client =>
{
    client.BaseAddress = new Uri("https://viacep.com.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
builder.Services.AddSingleton(TimeProvider.System);

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

app.UseStatusCodePagesWithReExecute("/acesso-negado");

// Loja brasileira: preços, datas e números em pt-BR, independentemente do host.
var ptBr = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(ptBr),
    SupportedCultures = [ptBr],
    SupportedUICultures = [ptBr],
});

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<RequireAdminTwoFactorMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

var storageOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
Directory.CreateDirectory(storageOptions.RootPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.GetFullPath(storageOptions.RootPath)),
    RequestPath = storageOptions.PublicPrefix,
});
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await app.ApplyMigrationsAsync();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
}

await app.RunAsync();

public partial class Program;
