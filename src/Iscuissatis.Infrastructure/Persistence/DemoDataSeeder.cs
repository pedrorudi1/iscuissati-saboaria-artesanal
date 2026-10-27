using Iscuissatis.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Iscuissatis.Infrastructure.Persistence;

/// <summary>
/// Dados de demonstração para homologação e testes E2E. Só roda quando
/// Database:SeedDemo=true — nunca em produção. Cria um produto publicado
/// apenas se ainda não existir nenhum.
/// </summary>
public sealed class DemoDataSeeder(AppDbContext dbContext, TimeProvider clock, ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Products.AnyAsync(product => product.Status == ProductStatus.Published, cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow();

        var category = Category.Create("Sabonetes em barra", now, "Barras artesanais", 1);
        dbContext.Categories.Add(category);

        var product = Product.Create(
            "Sabonete de Lavanda (demonstração)",
            category.Id,
            now,
            description: "Produto criado automaticamente para demonstração e testes ponta a ponta.",
            ingredients: "Aqua, Sodium Cocoate, Parfum, Lavandula Angustifolia Oil",
            howToUse: "Umedeça a barra, ensaboe as mãos e enxágue.",
            precautions: "Uso externo. Evite contato com os olhos.",
            netWeightGrams: 100);

        product.AddVariant("Lavanda 100 g", "DEMO-LAV-100", 24.90m, 10, 100, now, 8, 5, 2.5m);
        product.AddImage("/favicon.ico", "Sabonete de lavanda de demonstração", now, 32, 32);
        product.Publish(now);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dados de demonstração criados: {Slug}", product.Slug);
    }
}
