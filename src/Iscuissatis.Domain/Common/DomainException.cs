namespace Iscuissatis.Domain.Common;

/// <summary>
/// Violação de regra de negócio. Diferente de erro de programação: é esperada,
/// deve ser exibida ao usuário e nunca vira 500.
/// </summary>
public class DomainException(string message) : Exception(message);
