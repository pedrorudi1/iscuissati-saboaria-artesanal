# Spec 02 — Catálogo e admin de produtos

## Objetivo

Permitir que o dono cadastre categorias, produtos e variações com fotos, e que o catálogo
alimente a vitrine da Fase 3. Ao final desta fase, cadastrar um sabonete completo — nome,
descrição, ingredientes, preço, estoque e fotos — leva poucos minutos no painel.

## Fora de escopo

Vitrine pública (Fase 3), carrinho, checkout, frete, cupons, avaliações e kits.

## Entregáveis

- Entidades de domínio: `Category`, `Product`, `ProductVariant`, `ProductImage`, com regras
  testadas em unidade.
- Migration `CatalogoInicial` com as tabelas e índices únicos de `Slug` e `Sku`.
- Admin: CRUD de categorias, produtos e variações; publicação/rascunho.
- Upload de imagens com validação de tipo e tamanho, redimensionamento e conversão para WebP,
  atrás de `IFileStorage`.
- Slug gerado automaticamente a partir do nome, com sufixo numérico em caso de colisão.

## Modelo de dados (resumo)

**Category:** `Id`, `Name`, `Slug` (único), `Description?`, `SortOrder`, `IsActive`,
`CreatedAtUtc`, `UpdatedAtUtc`.

**Product:** `Id`, `Name`, `Slug` (único), `Description`, `Ingredients` (nomenclatura INCI),
`HowToUse`, `Precautions`, `NetWeightGrams`, `Status` (`Draft`/`Published`), `CategoryId`,
`CreatedAtUtc`, `UpdatedAtUtc`.

**ProductVariant:** `Id`, `ProductId`, `Sku` (único), `Name` (fragrância/tamanho), `Price`,
`PromotionalPrice?`, `StockQuantity`, `WeightGrams`, `LengthCm`, `WidthCm`, `HeightCm`,
`IsActive`.

**ProductImage:** `Id`, `ProductId`, `Path`, `AltText`, `SortOrder`, `IsPrimary`,
`Width`, `Height`.

Regras de dinheiro: `decimal(18,2)` em BRL. Datas em UTC (`DateTimeOffset`/`DateTime` UTC).

## Telas (Area Admin)

- `/admin/categorias` — lista, criar, editar, ativar/desativar, reordenar.
- `/admin/produtos` — lista com busca por nome/SKU, filtro por status e categoria.
- `/admin/produtos/novo` e `/admin/produtos/{id}` — dados do produto, variações e imagens.
- Publicar só é permitido com ao menos uma variação ativa e uma imagem.

## Regras de negócio

1. `Slug` é derivado do nome; colisão recebe sufixo (`-2`, `-3`, …).
2. Preço e estoque nunca são negativos.
3. `PromotionalPrice`, quando informado, precisa ser maior que zero e menor que `Price`.
4. `Sku` é obrigatório, único e normalizado em caixa alta.
5. Não é possível publicar produto sem variação ativa ou sem imagem.
6. Baixa de estoque nunca deixa `StockQuantity` negativo; o ajuste manual registra o motivo.
7. Produto publicado com estoque zero continua visível, mas marcado como esgotado na vitrine.
8. Imagem: aceitar apenas `image/jpeg`, `image/png` e `image/webp`, até 5 MB; o arquivo é
   reprocessado (nunca servido como chegou) e convertido para WebP.

## Critérios de aceite (Dado / Quando / Então)

1. Dado um nome "Sabonete de Lavanda", quando cadastro o produto, então o slug é
   `sabonete-de-lavanda`; cadastrando outro com o mesmo nome, o slug vira `sabonete-de-lavanda-2`.
2. Dado um produto sem variações, quando tento publicá-lo, então recebo um erro de negócio
   explicando o que falta.
3. Dado um produto com uma variação ativa e uma imagem, quando publico, então o status é
   `Published` e ele passa a aparecer nas consultas da vitrine.
4. Dado um preço promocional maior que o preço, quando salvo, então a validação falha.
5. Dado um upload de arquivo PDF renomeado para `.jpg`, quando envio, então o arquivo é
   rejeitado (validação por conteúdo, não por extensão).
6. Dado um produto com estoque 3, quando o admin registra saída de 5, então a operação é
   recusada e o estoque continua 3.
7. Dado um produto publicado e estoque zero, quando a vitrine consulta, então ele aparece
   como esgotado e não pode ser adicionado à sacola na Fase 4.

## Testes a escrever primeiro (ordem sugerida)

1. Unidade: `Product.Create` gera slug e recusa nome vazio.
2. Unidade: preço negativo e preço promocional inválido são recusados.
3. Unidade: `ProductVariant.AdjustStock` recusa deixar estoque negativo.
4. Unidade: `Product.Publish` exige variação ativa e imagem.
5. Unidade: `Sku` normalizado e obrigatório.
6. Integração: slug duplicado recebe sufixo ao persistir (constraint + serviço).
7. Integração: CRUD de categoria e produto via endpoints do admin, com autorização.
8. Integração: upload rejeita arquivo cujo conteúdo não é imagem e converte JPEG em WebP.
9. E2E (Playwright): admin cadastra um produto completo e o vê na lista.

## Fatias de entrega (uma branch e um PR por fatia)

1. **Domínio do catálogo** — entidades, regras e testes de unidade; migration inicial.
2. **Admin de categorias e produtos** — CRUD, validações, publicação e testes de integração.
3. **Imagens** — `IFileStorage`, upload, redimensionamento/WebP e testes.

## Definição de pronto

- Testes de unidade e integração verdes no CI, com cobertura de Domain/Application ≥ 80%.
- Nenhuma regra de negócio em controller.
- Cores e espaçamentos apenas via tokens.
- `AGENTS.md` respeitado: PR aberto pelo agente, merge manual.
