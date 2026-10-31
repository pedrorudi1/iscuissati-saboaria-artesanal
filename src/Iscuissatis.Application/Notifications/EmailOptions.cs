namespace Iscuissatis.Application.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"log" (desenvolvimento) ou "resend" (produção).</summary>
    public string Provider { get; set; } = "log";

    /// <summary>Remetente verificado no provedor, ex.: "Iscuissati's &lt;no-reply@dominio.com.br&gt;".</summary>
    public string From { get; set; } = "Iscuissati's Saboaria <no-reply@exemplo.com.br>";

    /// <summary>Chave de API do Resend. Nunca no repositório: variável de ambiente.</summary>
    public string ResendApiKey { get; set; } = string.Empty;

    public string ResendBaseUrl { get; set; } = "https://api.resend.com/";
}
