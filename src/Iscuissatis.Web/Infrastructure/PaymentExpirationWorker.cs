using Iscuissatis.Application.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Iscuissatis.Web.Infrastructure;

/// <summary>
/// Verifica a cada minuto se algum Pix venceu sem pagamento e devolve o estoque.
/// Não roda durante os testes (registro condicionado por configuração).
/// </summary>
public sealed class PaymentExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentExpirationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<PaymentExpirationService>();

                var expired = await service.ExpireOverdueAsync(asOf: null, stoppingToken);

                if (expired > 0)
                {
                    logger.LogInformation("Pix expirados: {Count} pedido(s) cancelado(s) e estoque devolvido.", expired);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha ao expirar pagamentos Pix.");
            }
        }
    }
}
