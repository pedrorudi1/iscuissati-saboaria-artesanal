# Spec 03 — Vitrine

## Objetivo

Transformar o catálogo cadastrado no painel em uma loja navegável: home, listagem por
categoria, busca e página de produto, mobile-first (a maior parte do tráfego vem do
Instagram, no celular), com SEO correto e boa performance.

## Fora de escopo

Carrinho, checkout, frete contratado, avaliações, kits, blog e personalização por cliente.
O cálculo de frete por CEP aparece aqui apenas como *estimativa* via `IShippingProvider`
(Fase 6); nesta fase a página mostra prazo e valor quando o provedor estiver configurado
e uma mensagem clara quando não estiver.

## Entregáveis

- Consultas públicas somente leitura, que **nunca** retornam produto em rascunho.
- Home com categorias ativas e produtos publicados (destaques + novidades).
- Listagem `/sabonetes` com filtro por categoria, busca por nome e ordenação.
  Paginação de 12 itens.
- Página `/produto/{slug}` com galeria, variações, preço, ingredientes (INCI), modo de uso,
  precauções, peso líquido e disponibilidade.
- SEO: `title` e `description` por página, canonical, Open Graph, `schema.org/Product` na
  página de produto e `sitemap.xml` (gerado na Fase 9, junto com robots.txt).
- Imagens com `loading="lazy"`, `width`/`height` para evitar layout shift e `alt` do cadastro.

## Regras de negócio

1. Só aparece o que está `Published` **e** com categoria ativa.
2. Produto publicado sem estoque aparece como **esgotado** e não pode ser comprado na Fase 4.
3. Preço exibido é o `EffectivePrice` da variação ativa mais barata ("a partir de").
4. Busca é insensível a acento e caixa (`unaccent`/`ILIKE`), no nome e na descrição.
5. Ordenação: relevância (padrão), menor preço, maior preço, novidades.
6. URL de categoria usa o slug; categoria inexistente ou inativa responde 404.
7. Produto em rascunho responde **404**, não 403 — não se revela o que ainda não existe.
8. Nenhuma página da vitrine acessa dados do cliente nem grava nada.

## Critérios de aceite (Dado / Quando / Então)

1. Dado um produto em rascunho, quando acesso `/produto/{slug}` direto, então recebo 404.
2. Dado um produto publicado com duas variações, quando abro a página, então vejo o menor
   preço como "a partir de" e as duas opções com estoque.
3. Dado um produto publicado com estoque zero em todas as variações, então a página mostra
   "esgotado" e o botão de compra fica desabilitado.
4. Dado que busco "calendula", quando envio a busca, então encontro "Sabonete de Calêndula".
5. Dado que filtro por uma categoria, então só aparecem produtos daquela categoria,
   publicados, ordenados conforme escolhido.
6. Dado a home, quando carrego no celular, então o Lighthouse mobile fica ≥ 90 em
   desempenho e acessibilidade.
7. Dado a página de um produto, quando inspeciono o HTML, então existe `schema.org/Product`
   com nome, imagem, preço e disponibilidade.

## Testes a escrever primeiro

1. Integração: consulta pública não retorna rascunho nem categoria inativa.
2. Integração: busca "calendula" acha "Calêndula" (acento) e ignora caixa.
3. Integração: ordenação por menor preço usa o `EffectivePrice` da variação mais barata.
4. Integração: paginação respeita 12 por página e o total.
5. Integração (página): 404 para rascunho, 200 para publicado, meta tags e JSON-LD presentes.
6. E2E (Playwright): da home até abrir um produto.

## Fatias de entrega

1. **Consultas públicas + home/listagem** — `ICatalogQueryService`, filtros, paginação e telas.
2. **Página de produto** — galeria, variações, dados técnicos e JSON-LD.
3. **Busca e SEO fino** — página de resultados, metadados por página, canonical e imagens.

## Definição de pronto

- CI verde com testes de integração das consultas e das páginas.
- Nenhuma consulta pública toca em dados de cliente.
- Tokens de design respeitados; nenhuma cor solta.
- PR aberto pelo agente, merge manual.
