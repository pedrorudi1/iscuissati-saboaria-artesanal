namespace Iscuissatis.Application.Sales;

public sealed record ZipCodeAddress(
    string ZipCode,
    string Street,
    string District,
    string City,
    string State);

/// <summary>
/// Consulta de CEP. Fica atrás de interface para o checkout não depender
/// de rede nos testes e para permitir trocar de provedor (ViaCEP, BrasilAPI).
/// </summary>
public interface IZipCodeLookup
{
    /// <summary>Retorna null quando o CEP não existe ou o serviço está fora.</summary>
    Task<ZipCodeAddress?> FindAsync(string zipCode, CancellationToken cancellationToken = default);
}
