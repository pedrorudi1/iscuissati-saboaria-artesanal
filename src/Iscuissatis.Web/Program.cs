using Iscuissatis.Application.Identity;
using Iscuissatis.Infrastructure.Identity;
using Iscuissatis.Application.Catalog;
using Iscuissatis.Application.Catalog.Public;
using Iscuissatis.Application.Sales;
using Iscuissatis.Application.Reporting;
using Iscuissatis.Application.Shipping;
using Iscuissatis.Application.Storage;
using Iscuissatis.Infrastructure.Persistence;
using Iscuissatis.Infrastructure.Persistence.Repositories;
using Iscuissatis.Infrastructure.Integrations;
using Iscuissatis.Application.Notifications;
using Iscuissatis.Application.Payments;
using Iscuissatis.Infrastructure.Notifications;
using Iscuissatis.Infrastructure.Payments;
using Iscuissatis.Infrastructure.Reporting;
using Iscuissatis.Infrastructure.Shipping;
using Iscuissatis.Infrastructure.Storage;
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddScoped<DemoDataSeeder>();
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
builder.Services.Configure<ShippingOptions>(builder.Configuration.GetSection(ShippingOptions.SectionName));
builder.Services.AddSingleton<IShippingProvider, TableShippingProvider>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

if (string.Equals(builder.Configuration["Email:Provider"], "resend", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>((provider, client) =>
    {
        var emailOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
        client.BaseAddress = new Uri(emailOptions.ResendBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    });
}
else
{
    builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
}
builder.Services.AddScoped<IOrderEmailNotifier, OrderEmailNotifier>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddHttpClient<IZipCodeLookup, ViaCepLookup>(client =>
{
    client.BaseAddress = new Uri("https://viacep.com.br/");
    client.Timeout = TimeSpan.FromSeconds(10); // ViaCEP pode passar de 4s em rede lenta
});
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

// Chaves de criptografia em volume: sem isso, todo deploy desloga admin e clientes
// e invalida os cookies de 2FA.
var keysPath = builder.Configuration["Security:DataProtectionPath"];

if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("Iscuissatis");
}

// Atrás do Cloudflare Tunnel / proxy reverso: confiar nos cabeçalhos de encaminhamento
// para que a aplicação veja https e o IP real do cliente.
if (builder.Configuration.GetValue("Security:TrustForwardedHeaders", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Configuration.GetValue("Security:TrustForwardedHeaders", false))
{
    app.UseForwardedHeaders();
}

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
// Em ambientes sem ICU (containers mínimos), cai para a cultura invariante em vez de quebrar.
CultureInfo ptBr;
try
{
    ptBr = new CultureInfo("pt-BR");
}
catch (CultureNotFoundException)
{
    ptBr = CultureInfo.InvariantCulture;
}

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

    // Homologação/E2E: cria um produto publicado se não houver nenhum.
    // Nunca ligar em produção (Database__SeedDemo=false por padrão).
    if (app.Configuration.GetValue("Database:SeedDemo", false))
    {
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
    }
}

await app.RunAsync();

public partial class Program;
