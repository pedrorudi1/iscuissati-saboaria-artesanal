# Spec 04 — Carrinho e checkout

## Objetivo

Permitir que o cliente monte a sacola, informe o endereço e gere um pedido aguardando pagamento,
com totais e estoque conferidos **no servidor**. É a fase em que a loja deixa de ser catálogo
e passa a registrar vendas.

## Fora de escopo

Cobrança (Fase 5), frete contratado (Fase 6), nota fiscal, e-mails transacionais (Fase 7) e
programa de fidelidade.

## Entregáveis

### Carrinho

- Sacola acessível por `/sacola`, funcionando para visitante (cookie) e para cliente logado.
- Ao entrar na conta, o carrinho anônimo **migra** para o carrinho do cliente, somando itens iguais.
- Item repetido soma quantidade; adicionar acima do estoque é recusado com mensagem.
- Alterar quantidade para zero remove o item.
- Subtotal, desconto e total recalculados **sempre no servidor**; nenhum valor vindo do navegador é aceito.
- Miniatura, variação e preço unitário visíveis em cada linha.

### Checkout

- Endereço com preenchimento por CEP (ViaCEP), com fallback de digitação manual.
- Checkout como visitante (e-mail obrigatório) ou cliente logado.
- Criação do pedido com **reserva de estoque**; a reserva é liberada se o pagamento não ocorrer.
- Resumo do pedido com itens, frete (quando configurado) e total.
- Pedido nasce em `PendingPayment`.

### Pedido

- Máquina de estados: `PendingPayment` → `Paid` → `Preparing` → `Shipped` → `Delivered`,
  com `Canceled` e `Refunded` como estados terminais alternativos.
- Transições inválidas são recusadas no domínio, não no controller.
- Histórico na conta do cliente.

## Modelo de dados

**Cart:** `Id`, `AnonymousId` (cookie, único quando presente), `CustomerId?` (Identity),
`CreatedAtUtc`, `UpdatedAtUtc`.

**CartItem:** `Id`, `CartId`, `ProductVariantId`, `ProductName` (congelado), `VariantName`,
`UnitPrice` (congelado no momento da adição), `Quantity`.

**Order:** `Id`, `Number` (sequencial legível), `CustomerId?`, `GuestEmail?`, `Status`,
`Subtotal`, `Shipping`, `Discount`, `Total`, `ShippingAddress` (value object), `CreatedAtUtc`,
`PaidAtUtc?`, `CanceledAtUtc?`.

**OrderItem:** `Id`, `OrderId`, `ProductVariantId`, `ProductName`, `VariantName`, `UnitPrice`,
`Quantity` — **preço e nome congelados** no momento da compra.

**StockReservation:** `Id`, `OrderId`, `ProductVariantId`, `Quantity`, `ExpiresAtUtc`, `ReleasedAtUtc?`.

## Regras de negócio

1. Quantidade nunca ultrapassa o estoque disponível no momento da adição.
2. Carrinho não compra variação inativa ou produto não publicado.
3. Preço do carrinho é o `EffectivePrice` da variação no momento da adição; o **checkout
   reconfere** e, se mudou, avisa o cliente antes de fechar o pedido.
4. Reserva de estoque acontece na criação do pedido, com prazo (padrão 30 minutos para Pix).
5. Duas compras simultâneas da última unidade: uma vence, a outra recebe erro claro de estoque.
6. Pedido só vira `Paid` por confirmação do gateway (Fase 5), nunca por ação do navegador.
7. Cancelamento libera a reserva e devolve o estoque.
8. E-mail do visitante é obrigatório para contato e é confirmado no pedido.

## Critérios de aceite (Dado / Quando / Então)

1. Dado um produto com estoque 3, quando adiciono 2 e depois mais 2, então o segundo pedido
   é recusado e a sacola continua com 2.
2. Dado o mesmo produto adicionado duas vezes, então existe **uma linha** com quantidade somada.
3. Dado um carrinho com 3 itens, quando altero a quantidade para zero, então o item sai.
4. Dado um preço adulterado no HTML, quando envio o formulário, então o total é recalculado
   pelo servidor e o valor do navegador é ignorado.
5. Dado um visitante que monta a sacola e depois entra na conta, então os itens aparecem na conta.
6. Dado duas sessões comprando a última unidade, quando ambas finalizam, então só uma cria o pedido.
7. Dado um pedido recém-criado, então o status é `PendingPayment` e o estoque está reservado.

## Testes a escrever primeiro

1. Unidade do carrinho: soma de item repetido, limite de estoque, quantidade zero remove.
2. Unidade: subtotal e total recalculados, arredondamento em duas casas.
3. Unidade: máquina de estados do pedido (transições válidas e inválidas).
4. Integração: adicionar/alterar/remover via endpoints com cookie de visitante.
5. Integração: migração do carrinho ao logar.
6. Integração: criação de pedido com reserva e recusa da segunda compra da última unidade.
7. E2E: da home até "aguardando pagamento".

## Fatias de entrega

1. **Carrinho** — domínio, persistência, endpoints e página da sacola.
2. **Checkout e pedido** — endereço por CEP, criação do pedido com reserva e máquina de estados.
3. **Conta do cliente** — histórico de pedidos e endereços.

## Definição de pronto

- CI verde com unidade e integração.
- Nenhum total confiado ao navegador.
- Estoque consistente após concorrência (teste com duas compras simultâneas).
- PR aberto pelo agente, merge manual.
