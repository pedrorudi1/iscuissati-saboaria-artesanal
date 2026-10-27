# Spec 01 — Fundação

## Objetivo
Ter a solution .NET 10 rodando em Docker, com autenticação do admin, layouts base e CI, tudo construído em TDD, para as fases seguintes se apoiarem nela.

## Fora de escopo
Catálogo, vitrine, carrinho, pagamentos e frete.

## Entregáveis
- Solution com os projetos `Domain`, `Application`, `Infrastructure` e `Web`, mais `Domain.Tests`, `Application.Tests` (integração com Testcontainers) e `E2E.Tests` (Playwright).
- `docker-compose.yml` com a aplicação e o PostgreSQL; `.env.example` documentado.
- EF Core com migration inicial; ASP.NET Core Identity com papéis Admin, Operador e Cliente; usuário admin criado por seed, com credenciais vindas de variáveis de ambiente; 2FA para o admin.
- Layouts base da loja (Area `Storefront`) e do admin (Area `Admin`) usando os tokens de `docs/design-tokens.md` e Tailwind.
- Serilog, health checks (`/health`) e pipeline de CI no GitHub Actions.

## Critérios de aceite (Dado / Quando / Então)
1. Dado um clone limpo, quando rodo `docker compose up`, então a aplicação e o PostgreSQL sobem e `/health` responde 200.
2. Dado o banco vazio, quando a aplicação inicia, então as migrations são aplicadas e o usuário admin existe com o papel Admin.
3. Dado um visitante anônimo, quando acessa `/admin`, então é redirecionado para o login.
4. Dado um usuário com papel Cliente, quando acessa `/admin`, então recebe acesso negado (403).
5. Dado o admin com credenciais corretas, quando faz login, então vê o layout do admin; sem 2FA configurado, é levado a configurá-lo.
6. Dado um push, quando o CI roda, então executa build e testes e falha se algum teste falhar.

## Testes a escrever primeiro (ordem sugerida)
1. Unidade: um primeiro teste simples de regra de domínio, por exemplo `Slug` normaliza "Sabonete de Lavanda" para `sabonete-de-lavanda` (será reaproveitado na Fase 2).
2. Integração: `/health` retorna 200 com PostgreSQL do Testcontainers.
3. Integração: seed cria o admin com o papel Admin.
4. Integração: `/admin` anônimo redireciona para o login; papel Cliente recebe 403.
5. E2E (Playwright): login do admin leva ao layout do admin.
6. CI: o workflow executa `dotnet test` e reprova com um teste quebrado de propósito (remova depois).

## Definição de pronto
- Todos os testes acima passando no CI, com cobertura medida.
- `AGENTS.md` na raiz e README com como rodar localmente.
- Nenhum segredo no repositório.
