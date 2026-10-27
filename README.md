# Iscuissati’s Saboaria Artesanal — e-commerce (.NET 10)

Kit inicial para construir a loja virtual e o painel administrativo com agentes de IA, seguindo TDD.

## O que tem aqui

```
├─ README.md
├─ AGENTS.md                         regras para os agentes (inclui TDD)
├─ CLAUDE.md                         importa o AGENTS.md (Claude Code)
├─ assets/logo-original.jpeg         logotipo enviado (tem a marca "Conteúdo gerado por IA")
└─ docs/
   ├─ plano-de-desenvolvimento.md    plano completo: stack, arquitetura, fases, TDD
   ├─ design-tokens.md               cores, fontes e componentes da marca
   ├─ specs/01-fundacao.md           primeira spec (modelo para as próximas)
   ├─ adr/0001-gateway-de-pagamento.md   proposta de gateway (não decidido)
   └─ design/                        protótipos: home (desktop e mobile) e página do produto
```

## Rodando localmente

Pré-requisitos: .NET SDK 10 e Docker.

```bash
cp .env.example .env        # ajuste POSTGRES_PASSWORD e Seed__AdminPassword
docker compose up --build   # aplicação em http://localhost:8080, health em /health
```

Sem Docker, com um PostgreSQL acessível:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=iscuissatis;Username=iscuissatis;Password=..."
dotnet run --project src/Iscuissatis.Web
```

Testes:

```bash
dotnet test                                  # unidade + integração (exige Docker)
dotnet test --collect:"XPlat Code Coverage"   # com cobertura
```

## Deploy (Coolify + Cloudflare Tunnel)

- A aplicação escuta em `http://+:8080` e não redireciona para HTTPS por conta própria:
  o TLS termina no Cloudflare (`Security__UseHttpsRedirection=false`), evitando loop de redirect.
- Cada variável do `.env.example` vira uma variável de ambiente do recurso no Coolify.
- Migrations rodam no start (`Database__MigrateOnStartup=true`); o Postgres do Coolify
  deve ser um serviço separado ou um banco gerenciado, com backup diário.

## Como começar

1. Crie o repositório e copie o conteúdo desta pasta para a raiz. Faça o commit inicial.
2. Abra o repositório no seu agente de IA. O `AGENTS.md` é lido como instrução permanente.
3. **Fase 0 (você):** responda as decisões em aberto (seção 11 do plano) e reúna logo vetorial, fotos dos produtos e a lista inicial de produtos.
4. **Fase 1:** cole o prompt de partida abaixo, revise o plano que o agente apresentar e aprove antes de ele codar.
5. **Próximas fases:** crie `docs/specs/NN-nome.md` a partir do plano (use `01-fundacao.md` como modelo) e use o prompt-modelo da seção 8 do plano.

## Prompt de partida (Fase 1)

```
Crie a solution .NET 10 conforme docs/specs/01-fundacao.md, seguindo TDD (veja AGENTS.md).
Comece pelos projetos de teste (unidade, integração com Testcontainers e E2E com Playwright)
e escreva o primeiro teste vermelho antes do código que o faz passar. Depois: projetos Domain,
Application, Infrastructure e Web, Docker Compose com PostgreSQL, EF Core com migration
inicial, Identity com papéis Admin/Operador/Cliente e seed do admin, layouts base da loja e
do admin usando os tokens de design de docs/design-tokens.md, Serilog, health checks e
pipeline de CI que roda todos os testes e bloqueia o merge se algo falhar.
```

## Sobre os protótipos (`docs/design/`)

Os arquivos `.dc.html` são do Claude Design e dependem de um `support.js` que não vem no pacote, então não abrem como páginas avulsas. Use-os como referência de estrutura, textos, cores e medidas para montar as views Razor e o Tailwind. Para ver o resultado renderizado, abra o protótipo online: https://claude.ai/artifact/8AN5Dyxyrov7XRAWxmDu7u

Os textos entre colchetes (preços, descrições, ingredientes, dados da empresa) são placeholders. Os nomes dos sabonetes são exemplos.

## Pendências conhecidas

- Logo limpo, em SVG ou PNG de alta resolução com fundo transparente.
- Gateway de pagamento: proposta no ADR 0001, ainda não decidida.
- Regularização dos sabonetes (ANVISA), nota fiscal e regime tributário: validar com contador.
