using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Iscuissatis.Application.Sales;
using Microsoft.Extensions.Logging;

namespace Iscuissatis.Infrastructure.Integrations;

/// <summary>
/// ViaCEP (https://viacep.com.br). Sem chave, sem custo.
/// Qualquer falha de rede devolve null e o checkout segue com digitação manual.
/// </summary>
public sealed class ViaCepLookup(HttpClient httpClient, ILogger<ViaCepLookup> logger) : IZipCodeLookup
{
    public async Task<ZipCodeAddress?> FindAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        var digits = new string((zipCode ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length != 8)
        {
            return null;
        }

        try
        {
            var response = await httpClient.GetAsync($"ws/{digits}/json/", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ViaCepResponse>(cancellationToken: cancellationToken);

            if (payload is null || payload.Error)
            {
                return null;
            }

            return new ZipCodeAddress(
                digits,
                payload.Street ?? string.Empty,
                payload.District ?? string.Empty,
                payload.City ?? string.Empty,
                payload.State ?? string.Empty);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Consulta de CEP {ZipCode} falhou; seguindo com digitação manual.", digits);
            return null;
        }
    }

    private sealed class ViaCepResponse
    {
        [JsonPropertyName("cep")]
        public string? ZipCode { get; set; }

        [JsonPropertyName("logradouro")]
        public string? Street { get; set; }

        [JsonPropertyName("bairro")]
        public string? District { get; set; }

        [JsonPropertyName("localidade")]
        public string? City { get; set; }

        [JsonPropertyName("uf")]
        public string? State { get; set; }

        [JsonPropertyName("erro")]
        public bool Error { get; set; }
    }
}
