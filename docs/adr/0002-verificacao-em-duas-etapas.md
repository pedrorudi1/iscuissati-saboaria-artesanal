# ADR 0002 — Verificação em duas etapas para o painel

**Status:** aceito · **Data:** 27/10/2026 · **Relacionado:** spec 01, PR #6

## Contexto

O painel administra preços, estoque e pedidos de uma loja real. Uma senha vazada ou reutilizada
daria acesso total ao negócio. A spec 01 exige 2FA para o admin e diz explicitamente que, sem
2FA configurado, o usuário deve ser levado a configurá-lo.

## Decisão

- **TOTP (RFC 6238)**, o padrão do `Microsoft.AspNetCore.Identity`, com 6 dígitos e janela de
  30 segundos. Funciona com Google Authenticator, Microsoft Authenticator, 1Password, Bitwarden
  e afins — nenhum fornecedor fica acoplado.
- **Obrigatório para `Admin` e `Operador`**, não para `Cliente`. Cliente não acessa o painel.
- **Bloqueio por middleware**, não apenas no login: qualquer rota `/admin` (exceto
  `/admin/seguranca`) redireciona para a configuração enquanto o 2FA estiver desligado.
  Assim um link direto ou uma sessão antiga também ficam contidos.
- **Códigos de recuperação**: 10, de uso único, exibidos uma única vez e regeráveis no painel.
- **QR code gerado no servidor** com `QRCoder` (PNG em memória, sem dependência nativa), para
  funcionar no container Alpine sem `libgdiplus`.
- **"Lembrar este dispositivo" desligado** por enquanto: `rememberClient: false`. Reavaliar se
  o uso diário incomodar; a decisão precisa de prazo de validade explícito.

## Consequências

- Todo login do painel tem dois passos; o primeiro acesso tem um passo extra de configuração.
- Perda do celular exige um código de recuperação; sem eles, o acesso só volta com intervenção
  no banco (procedimento a documentar antes do lançamento).
- A chave do autenticador fica no banco (`AspNetUsers`), nunca no repositório.
- Testes de integração calculam TOTP de verdade (`tests/.../Totp.cs`), então o fluxo é
  exercitado como o aplicativo faria — não há atalho que mascare regressão.
- Se o projeto passar a ter mais operadores, convém registrar auditoria de ativação/desativação
  de 2FA (hoje só o `AuditLog` da Fase 7 cobre isso).

## Alternativas descartadas

- **SMS**: custo por mensagem, depende de operadora e é vulnerável a SIM swap.
- **E-mail com código**: mais simples, porém transforma o e-mail em fator único de fato e
  adiciona latência ao login.
- **Passkeys/WebAuthn**: melhor segurança e UX, mas exige suporte e recuperação mais elaborados;
  fica como evolução futura, sem bloquear o lançamento.
