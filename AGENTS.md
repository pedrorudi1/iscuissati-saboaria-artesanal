# AGENTS.md — Iscuissati’s Saboaria Artesanal (e-commerce)

## Contexto
Loja virtual de sabonetes artesanais. .NET 10, ASP.NET Core MVC (Areas Storefront e Admin),
EF Core + PostgreSQL, Tailwind, HTMX. Documentação em docs/.

## Comandos
- dotnet build
- dotnet test
- dotnet test --collect:"XPlat Code Coverage"
- docker compose up -d
- dotnet ef migrations add NOME -p src/Iscuissatis.Infrastructure -s src/Iscuissatis.Web

## TDD (obrigatório)
- Nenhum código de produção sem um teste que falhou antes: Red, Green, Refactor.
- Fluxo de cada tarefa: (1) escreva o teste a partir do critério de aceite da spec;
  (2) rode e veja falhar pelo motivo certo; (3) escreva o mínimo para passar;
  (4) refatore com tudo verde; (5) repita.
- No PR, mostre a saída do teste vermelho antes da implementação.
- O nome do teste descreve o comportamento: Order_marked_as_paid_decrements_stock.
- Domain e Application: testes de unidade, sem rede nem banco. Persistência, endpoints
  e webhooks: integração com PostgreSQL real (Testcontainers). Fluxos críticos: Playwright.
- Sem relógio nem aleatoriedade reais: use TimeProvider e fakes.
- Bug novo: primeiro um teste que o reproduz, depois a correção.
- Nunca apague nem enfraqueça um teste para fazê-lo passar. Se o teste estiver errado,
  explique no PR.
- Cobertura mínima de 80% em Domain e Application.

## Regras
- Interface em pt-BR; código e identificadores em inglês.
- Dinheiro: decimal (BRL). Datas: UTC.
- Controllers finos; regra de negócio em Domain/Application.
- Integração externa só via interface, com fake nos testes.
- Cores, fontes e espaçamentos só via tokens (docs/design-tokens.md); nunca valores soltos.
- Nunca editar migration já aplicada; criar uma nova.
- Sem segredos no código. Todo PR com testes e build verde.
- Dúvida de negócio: perguntar, não presumir.
- O agente abre o PR e para: o merge na `main` é sempre manual do responsável.
  Nunca faça merge, squash ou push direto na `main`.

## Referências
- Plano completo: docs/plano-de-desenvolvimento.md
- Specs por fase: docs/specs/
- Decisões: docs/adr/
- Protótipos de design (referência de estrutura, textos e estilos): docs/design/
