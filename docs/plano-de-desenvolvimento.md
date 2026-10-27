# Plano de Desenvolvimento — E-commerce Iscuissati’s Saboaria Artesanal

*Plano de produto e técnico em .NET 10, pensado para ser executado com agentes de IA · v1.1 · 27/10/2026 · com TDD*

## 1. Visão geral

**Objetivo:** ter uma loja virtual própria da Iscuissati’s Saboaria Artesanal, começando com sabonetes artesanais e preparada para receber novas linhas de produtos (sabonetes líquidos, velas, kits, presentes…) sem refazer o sistema.

**Dois sistemas em um só projeto:**

- **Loja (cliente):** vitrine, busca, página de produto, carrinho, checkout, conta do cliente e acompanhamento do pedido.
- **Painel administrativo (dono):** produtos, estoque, pedidos, clientes, cupons, frete, conteúdo e relatórios.

**Princípios:** simples de operar por uma pessoa; mobile-first (boa parte do tráfego virá do Instagram, no celular); rápido e bom em SEO; Pix como forma de pagamento prioritária; arquitetura simples (monólito) que um agente de IA consiga entender por inteiro; e TDD em todas as fases, com todo código novo nascendo de um teste que falha.

## 2. Identidade visual (a partir do logotipo)

**Leitura do logo:** selo circular com textura de papel kraft, borda dupla, folhas verde-oliva simétricas, nome em caixa alta condensada na cor terracota, “Saboaria Artesanal” em script caligráfico e o lema “Produtos naturais & feitos à mão”. Transmite artesanal, natural e acolhedor.

**Paleta** (valores aproximados — confirme com conta-gotas no arquivo original):

| Uso | Cor | Hex |
|---|---|---|
| Primária (títulos, botões, links) | Terracota | #A9482B |
| Secundária (detalhes, ícones, divisores) | Verde-oliva | #6B7A45 |
| Fundo principal | Creme kraft | #F3EAD8 |
| Fundo de cartões/seções | Creme claro | #FBF7EF |
| Texto | Marrom escuro | #3A2A20 |
| Bordas e linhas | Bege | #D9C9AA |

Cuidado de acessibilidade: terracota sobre creme e texto branco sobre terracota passam no contraste mínimo (WCAG AA); o verde-oliva sobre creme não passa para texto pequeno, então use-o só em elementos decorativos e títulos grandes.

**Tipografia:** títulos em sans condensada bold e caixa alta (Oswald ou Barlow Condensed); destaques em script caligráfico (Yellowtail ou Courgette), com moderação, só em subtítulos e slogans; texto corrido em Lato ou Nunito Sans. Hospede as fontes no próprio site (desempenho e LGPD).

**Elementos de design:** textura sutil de papel kraft no fundo; ornamento de folhas como divisor de seções; selo circular como favicon e badge “feito à mão”; cantos levemente arredondados; fotos com luz natural em fundo cru.

**Ação antes de começar:** peça o logo em SVG ou PNG de alta resolução com fundo transparente. A imagem enviada é raster e traz a marca “Conteúdo gerado por IA” no canto; para cabeçalho, favicon e redes sociais convém ter a versão original, limpa.

**Regra para os agentes:** cores, fontes e espaçamentos existem só como tokens (variáveis CSS e tema do Tailwind). Nenhum valor solto no código.

## 3. Stack recomendada

| Camada | Escolha | Por quê |
|---|---|---|
| Plataforma | .NET 10 (LTS), C# 14, ASP.NET Core | Requisito do projeto; suporte de longo prazo |
| Web | ASP.NET Core MVC + Razor, com Areas `Storefront` e `Admin` | Renderização no servidor: SEO e velocidade no celular |
| Interatividade | HTMX + Alpine.js | Mini-carrinho, filtros e formulários dinâmicos sem SPA |
| Estilo | Tailwind CSS (CLI standalone) + tokens da marca | Design system consistente; sem exigir Node |
| Banco | PostgreSQL + EF Core 10 (migrations) | Robusto, gratuito, ótimo com .NET |
| Autenticação | ASP.NET Core Identity; papéis Admin, Operador, Cliente | Padrão do ecossistema; 2FA para o admin |
| Validação / mapeamento | FluentValidation, Mapperly | Evita bibliotecas que migraram para licença comercial (MediatR, AutoMapper); confira licenças ao escolher pacotes |
| Segundo plano | Hangfire (ou BackgroundService + tabela outbox) | E-mails, expiração de pedidos, retentativas de webhook |
| Imagens | Upload com redimensionamento e WebP; armazenamento atrás de `IFileStorage` | Disco/volume no início; S3 compatível (ex.: Cloudflare R2) depois |
| E-mail | MailKit/SMTP ou Resend/Brevo atrás de `IEmailSender` | Templates Razor com a identidade da marca |
| Pagamentos | Mercado Pago ou Asaas atrás de `IPaymentGateway` | Pix, cartão e boleto; troca de provedor sem reescrever |
| Frete | Melhor Envio atrás de `IShippingProvider` | Correios e transportadoras por uma API; fallback com tabela própria |
| CEP | ViaCEP ou BrasilAPI | Preenchimento automático de endereço |
| Observabilidade | Serilog, health checks, Sentry (ou OpenTelemetry) | Diagnóstico em produção |
| Testes | xUnit, Shouldly, NSubstitute, Testcontainers (Postgres real), WebApplicationFactory, Playwright, Coverlet | TDD em todas as camadas: unidade, integração e ponta a ponta |
| Infra | Docker, GitHub Actions, VPS com Docker (ex.: Coolify), Cloudflare | Deploy repetível, CDN e proteção |

É uma recomendação, não uma imposição: Blazor Server no admin é alternativa viável, mas mistura dois paradigmas de interface; para agentes de IA, um estilo só reduz erros.

## 4. Arquitetura

**Monólito modular em camadas** — um único deploy, fácil de operar e de depurar.

```
Iscuissatis.sln
├─ src/
│  ├─ Iscuissatis.Domain/          entidades, enums, regras de negócio (sem dependências)
│  ├─ Iscuissatis.Application/     casos de uso, DTOs, validações, interfaces
│  ├─ Iscuissatis.Infrastructure/  EF Core, Identity, gateways, e-mail, storage, jobs
│  └─ Iscuissatis.Web/             MVC, Areas (Storefront, Admin), webhooks, wwwroot
├─ tests/
│  ├─ Iscuissatis.Domain.Tests/
│  ├─ Iscuissatis.Application.Tests/
│  └─ Iscuissatis.E2E.Tests/       Playwright
├─ docs/                           specs por fase, ADRs, este plano
├─ AGENTS.md
├─ docker-compose.yml
└─ .github/workflows/
```

**Módulos de negócio** (pastas dentro de Domain e Application): Catálogo, Carrinho, Pedidos, Pagamentos, Frete, Clientes, Marketing (cupons e banners), Conteúdo, Configurações, Relatórios.

**Regras de arquitetura:**

- Dependências apontam para dentro: Web → Application → Domain; Infrastructure implementa as interfaces.
- Controllers finos; regra de negócio em Domain/Application.
- Dinheiro sempre em `decimal` (BRL) e datas em UTC; nunca `float` ou `double` para valores.
- Toda integração externa (pagamento, frete, e-mail, storage) fica atrás de interface, com implementação fake para testes.
- Webhooks são idempotentes e verificam assinatura.
- Preços e totais são recalculados no servidor; nunca confie em valores vindos do navegador.

## 5. Modelo de domínio (resumo)

**Catálogo:** `Category`, `Product` (nome, slug, descrição, ingredientes, modo de uso, precauções, status rascunho/publicado), `ProductVariant` (SKU, fragrância/tamanho, preço, preço promocional, estoque, peso e dimensões da embalagem para o frete), `ProductImage`.

**Vendas:** `Cart`/`CartItem`, `Order`/`OrderItem` (com preço e nome congelados no momento da compra), `Payment`, `Shipment`, `Coupon`, `StockMovement`.

**Pessoas:** `ApplicationUser` (Identity), `Customer`, `Address`.

**Conteúdo e operação:** `Banner`, `ContentPage`, `StoreSetting`, `AuditLog`.

**Status do pedido:** `PendingPayment` → `Paid` → `Preparing` → `Shipped` → `Delivered`; além de `Canceled` e `Refunded`.

**Estoque:** reservar ao criar o pedido; liberar se o Pix expirar sem pagamento; baixar definitivamente ao confirmar o pagamento. Todo movimento gera um `StockMovement`.

**Campos específicos de cosmético:** lista de ingredientes (nomenclatura INCI), modo de uso, precauções e peso líquido aparecem na página do produto.

## 6. Funcionalidades por prioridade

### MVP (versão 1.0)

**Loja**

- Home com banner, destaques, categorias e selo “feito à mão”
- Catálogo com categorias, ordenação e busca simples
- Página de produto: galeria, variações, ingredientes, modo de uso, disponibilidade e cálculo de frete por CEP
- Carrinho persistente (mini-carrinho + página)
- Checkout como visitante ou com conta, endereço por CEP, frete, cupom e pagamento (Pix e cartão)
- Conta do cliente: pedidos, endereços e dados
- Páginas institucionais: Sobre, Contato, FAQ, Trocas e devoluções, Privacidade e Termos
- Botão de WhatsApp, link para o Instagram, SEO básico e e-mails transacionais

**Painel admin**

- Login seguro com 2FA
- Dashboard: vendas do dia e do mês, pedidos pendentes, estoque baixo
- CRUD de produtos, variações, categorias e imagens
- Estoque: ajustes manuais e histórico
- Pedidos: lista com filtros, detalhe, mudança de status, código de rastreio, cancelamento e reembolso
- Clientes, cupons, configurações da loja, banners e páginas
- Usuários e papéis; log de auditoria

### Fase 2 (após o lançamento)

Avaliações de clientes, kits e combos, produtos relacionados, aviso de reposição, carrinho abandonado por e-mail, emissão de nota fiscal integrada, relatórios avançados, login com Google, indicação/fidelidade, blog, feed de catálogo para Instagram/Facebook Shops, notificações por WhatsApp.

### Futuro

PWA, integração com marketplaces (Mercado Livre, Shopee), assinatura/recompra recorrente, multi-idioma.

## 7. Roadmap por fases

Cada fase vira uma spec em `docs/specs/` e uma série de tarefas pequenas para os agentes, todas em TDD (regra abaixo).

### Prática obrigatória: TDD (Red → Green → Refactor)

Nenhum código de produção entra sem um teste que falhou antes. A regra vale para pessoas e agentes, em todas as fases.

1. **Red:** escreva o teste a partir do critério de aceite da spec e veja-o falhar pelo motivo certo.
2. **Green:** escreva o mínimo de código para o teste passar.
3. **Refactor:** limpe o código com todos os testes verdes, sem mudar o comportamento.
4. **Repita** em ciclos curtos, com commits pequenos (`test:`, `feat:`, `refactor:`).

| Nível | O que cobre | Ferramentas | Quando escrever |
|---|---|---|---|
| Unidade (Domain e Application) | Regras de preço, estoque, cupom, frete e status do pedido | xUnit, Shouldly, NSubstitute | Sempre primeiro; é a maioria dos testes |
| Integração | EF Core com PostgreSQL real, Identity, endpoints, webhooks | xUnit, Testcontainers, WebApplicationFactory | Antes de cada endpoint, consulta ou migration |
| Contrato das integrações | Pagamento e frete respeitam `IPaymentGateway` e `IShippingProvider` | Fakes em memória; sandbox do provedor | Antes de cada adaptador |
| Ponta a ponta | Fluxos críticos no navegador: compra com Pix e cartão, cupom, frete, admin cria produto | Playwright | O teste do fluxo nasce vermelho no início da fase e fecha na entrega |

**Regras de TDD:**

- Bug novo = primeiro um teste que o reproduz, depois a correção.
- Nunca apagar nem enfraquecer um teste para fazê-lo passar.
- Testes rápidos e determinísticos: sem rede nem relógio real (use `TimeProvider`) nos testes de unidade.
- Dinheiro, estoque e webhooks exigem casos de borda: arredondamento, estoque zero, duas compras da última unidade, webhook repetido ou fora de ordem.
- Cobertura mínima de 80% em Domain e Application, como alarme e não como meta. O CI bloqueia o merge se algum teste falhar ou a cobertura cair.

**Fase 0 — Definições (você, antes do código)**

Decisões da seção 11, logo vetorial, fotos dos produtos, lista inicial de produtos (nome, preço, peso, ingredientes), textos institucionais, contas no gateway e no Melhor Envio, domínio.

**Fase 1 — Fundação**

- Solution com os 4 projetos e os 3 projetos de teste (unidade, integração com Testcontainers e E2E com Playwright), Docker Compose (app + Postgres) e EF Core com migration inicial
- Identity com papéis e usuário admin via seed; 2FA
- Layouts base da loja e do admin com tokens da marca e Tailwind
- Serilog, health checks, CI (build + testes + cobertura, bloqueando o merge se algo falhar), AGENTS.md
- *Aceite:* `docker compose up` sobe o sistema, o login do admin funciona e o CI fica verde, com o primeiro teste escrito em ciclo Red → Green → Refactor.

**Fase 2 — Catálogo e admin de produtos**

- Entidades e migrations; CRUD de categorias, produtos e variações
- Upload de imagens com redimensionamento e WebP; slug automático; rascunho/publicado
- *Aceite:* cadastrar um sabonete completo, com fotos, leva poucos minutos; testes de domínio passando.

*Testes primeiro:* unidade das regras de `Product` e `ProductVariant` (slug único, preço não negativo, estoque não negativo); integração do CRUD com PostgreSQL real; upload que rejeita arquivo que não é imagem.

**Fase 3 — Vitrine**

- Home, listagem com filtros e ordenação, busca, página de produto, páginas de categoria
- Responsivo, mobile-first, com SEO (títulos, meta, schema.org Product, Open Graph)
- *Aceite:* Lighthouse mobile ≥ 90 em desempenho e acessibilidade nas páginas principais.

*Testes primeiro:* integração das consultas do catálogo (filtro, ordenação, busca, só produtos publicados); testes de página com `WebApplicationFactory` para título, meta tags e schema.org; E2E que abre um produto a partir da home.

**Fase 4 — Carrinho e checkout**

- Carrinho (cookie → conta ao logar), validação de estoque, cupons
- Endereço com ViaCEP; checkout como visitante ou cliente; criação do pedido com reserva de estoque
- *Aceite:* fluxo completo até “aguardando pagamento”, com totais recalculados no servidor.

*Testes primeiro:* unidade do carrinho (item repetido soma quantidade, limite de estoque, cupom válido e expirado, total recalculado); integração da criação do pedido com reserva de estoque, incluindo duas compras disputando a última unidade.

**Fase 5 — Pagamentos**

- `IPaymentGateway` com Pix (QR code e copia-e-cola), cartão (tokenização, sem tocar em dados do cartão) e boleto opcional
- Webhooks com verificação de assinatura e idempotência; expiração de pedidos não pagos
- *Aceite:* pagamento aprovado no sandbox muda o pedido para `Paid` e baixa o estoque; webhooks repetidos não duplicam efeitos.

*Testes primeiro:* contrato do `IPaymentGateway` com um fake; webhook com assinatura inválida rejeitado, repetido ignorado e fora de ordem tratado; pedido não pago expira e libera o estoque; E2E de compra com Pix no sandbox.

**Fase 6 — Frete**

- `IShippingProvider` (Melhor Envio), cálculo por CEP no produto, carrinho e checkout
- Regras: frete grátis acima de um valor, retirada local, prazo de preparo
- Geração de etiqueta e código de rastreio no admin
- *Aceite:* frete correto para CEPs de teste em diferentes regiões; o fallback funciona se a API cair.

*Testes primeiro:* unidade das regras de frete (frete grátis, retirada local, peso total das variações); contrato do `IShippingProvider` com fake; teste do fallback quando a API de frete falha.

**Fase 7 — Gestão de pedidos e comunicação**

- Máquina de estados do pedido, notas internas, reembolso/cancelamento
- E-mails: confirmação, pagamento aprovado, enviado (com rastreio), entregue
- Histórico de pedidos na conta do cliente
- *Aceite:* cada mudança de status dispara o e-mail correto, uma única vez.

*Testes primeiro:* unidade da máquina de estados (transições válidas e inválidas); integração garantindo um único e-mail por mudança de status.

**Fase 8 — Dashboard e relatórios**

- Vendas por período, ticket médio, produtos mais vendidos, estoque baixo, exportação CSV
- *Aceite:* os números do dashboard batem com os pedidos pagos no banco.

*Testes primeiro:* integração dos relatórios contra um banco com pedidos conhecidos (totais, ticket médio, ranking); só pedidos pagos entram na conta.

**Fase 9 — Conteúdo, SEO e conformidade**

- Páginas institucionais e banners editáveis no admin; sitemap.xml e robots.txt
- LGPD: banner de cookies com consentimento, política de privacidade, exportação e exclusão de dados do cliente
- Termos, política de trocas (direito de arrependimento) e dados da empresa no rodapé
- Meta Pixel e GA4 só após consentimento

*Testes primeiro:* nenhum script de analytics carrega antes do aceite de cookies; exportação e exclusão de dados do cliente; geração correta do `sitemap.xml`.

**Fase 10 — Qualidade e lançamento**

- Testes ponta a ponta dos fluxos críticos (escritos fase a fase, agora rodando no CI contra homologação): compra com Pix, compra com cartão, cupom, frete, admin criando produto e despachando pedido
- Revisão de segurança, backups com restore testado, monitoramento e alertas
- Deploy de produção (HTTPS, Cloudflare), soft launch com pedidos reais de teste e, depois, divulgação no Instagram

## 8. Como conduzir o desenvolvimento com agentes de IA

1. **Prepare o repositório:** crie o `AGENTS.md` (modelo abaixo) e a pasta `docs/specs/` com uma spec por fase (objetivo, escopo, fora de escopo, modelo de dados, telas, regras, critérios de aceite em formato Dado/Quando/Então, lista de testes a escrever primeiro). Use este documento como fonte.
2. **Uma tarefa = uma branch = um PR pequeno** (idealmente menos de 400 linhas), sempre com o teste escrito antes do código e o build verde.
3. **Plano antes do código:** o agente escreve um plano curto, você aprova, e só então ele implementa, em ciclos de TDD: teste que falha, código mínimo, refatoração.
4. **Ordem:** fases 1 a 5 em sequência (uma depende da outra). Depois, 6, 8 e 9 podem andar em paralelo com a 7, com agentes em branches separadas.
5. **Revisão humana obrigatória** em: pagamentos e webhooks, autenticação e autorização, migrations, cálculo de valores e frete, LGPD.
6. **Segredos fora do repositório:** user-secrets e variáveis de ambiente, com um `.env.example` documentado.
7. **Registre decisões** em `docs/adr/` (um arquivo curto por decisão), para os agentes não reabrirem discussões já resolvidas.
8. **Valide visualmente:** peça capturas de tela (Playwright) a cada tarefa de interface e compare com a identidade da seção 2.

**Modelo de `AGENTS.md`:**

```
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
- Cores, fontes e espaçamentos só via tokens; nunca valores soltos.
- Nunca editar migration já aplicada; criar uma nova.
- Sem segredos no código. Todo PR com testes e build verde.
- Dúvida de negócio: perguntar, não presumir.
```

**Prompt-modelo para cada tarefa:**

```
Leia AGENTS.md e docs/specs/NN-nome-da-fase.md. Antes de codar, apresente um plano curto
(arquivos a criar/alterar, riscos e a lista de testes que serão escritos primeiro). Aguarde
minha aprovação. Depois trabalhe em ciclos de TDD: escreva um teste, mostre-o falhando,
implemente o mínimo para passar e refatore. Rode build e todos os testes e descreva no PR
o que mudou, quais testes foram adicionados e como validar.
```

**Prompt de partida (Fase 1):**

```
Crie a solution .NET 10 conforme docs/specs/01-fundacao.md, seguindo TDD (veja AGENTS.md).
Comece pelos projetos de teste (unidade, integração com Testcontainers e E2E com Playwright)
e escreva o primeiro teste vermelho antes do código que o faz passar. Depois: projetos Domain,
Application, Infrastructure e Web, Docker Compose com PostgreSQL, EF Core com migration
inicial, Identity com papéis Admin/Operador/Cliente e seed do admin, layouts base da loja e
do admin usando os tokens de design de docs/design-tokens.md, Serilog, health checks e
pipeline de CI que roda todos os testes e bloqueia o merge se algo falhar.
```

## 9. Segurança e conformidade

- HTTPS obrigatório, HSTS, cookies `Secure` e `HttpOnly`, proteção antiforgery em todos os formulários
- Limite de tentativas de login (rate limiting), 2FA no admin, política mínima de senha
- Cartão: nunca armazenar nem trafegar dados de cartão pelo seu servidor; use a tokenização do gateway
- Webhooks: validar assinatura, ser idempotente e registrar tudo em log
- Autorização por papel em cada ação do admin; log de auditoria para alterações de preço, estoque e status
- Upload de imagem: validar tipo e tamanho e regerar o arquivo (não confiar na extensão)
- LGPD: coletar só o necessário, política de privacidade, consentimento de cookies, exportação e exclusão de dados
- Defesa do consumidor e e-commerce: identificação da empresa (razão social, CNPJ, endereço) no site, informação clara de preço, prazo e frete, política de trocas e direito de arrependimento de 7 dias
- Cosméticos: confirme com contador e com a vigilância sanitária/ANVISA as exigências de regularização e de rotulagem dos sabonetes antes de vender. Este plano não substitui orientação de contador ou advogado
- Backups diários do banco, com cópia fora do servidor e teste de restauração

## 10. Infraestrutura e operação

- **Ambientes:** local (Docker Compose), homologação e produção, cada um com seus segredos.
- **CI/CD:** GitHub Actions — build, testes (unidade, integração e E2E) com cobertura, imagem Docker, deploy automático para homologação e manual para produção.
- **Hospedagem:** VPS com Docker (por exemplo via Coolify) atrás do Cloudflare (DNS, CDN, WAF); imagens em volume persistente ou bucket S3 compatível.
- **Monitoramento:** health checks, logs estruturados, alerta de erro (Sentry) e de indisponibilidade.
- **Desempenho:** cache de páginas de catálogo, imagens em WebP com tamanhos responsivos, compressão e cache de arquivos estáticos.

## 11. Decisões em aberto

1. Regime tributário e nota fiscal: MEI ou outro? Quando passar a emitir NF-e?
2. Gateway de pagamento: Mercado Pago, Asaas ou outro? Quais formas (Pix, cartão, boleto) e haverá parcelamento? (veja `docs/adr/0001-gateway-de-pagamento.md`)
3. Frete: de qual CEP despacha? Melhor Envio, Correios direto ou tabela própria? Haverá retirada ou entrega local?
4. Quantos produtos e variações no lançamento? Haverá kits e presentes?
5. Domínio e e-mail da loja.
6. Quem opera o painel no dia a dia: só o dono ou mais pessoas?
7. Política de trocas, prazo de preparo do pedido e modelo de estoque (produção sob encomenda ou pronta entrega).
8. Quem fotografa e edita as imagens, e quem escreve as descrições dos produtos?
9. Onde hospedar: VPS própria ou serviço gerenciado?
10. Orçamento mensal para infraestrutura e ferramentas.
