# Spec 05 — Pagamentos

## Objetivo

Cobrar os pedidos com Pix (prioridade) e cartão, sem que o servidor toque em dados de cartão e
sem depender de um provedor específico no checkout. A escolha do gateway é decisão de negócio
(ADR 0001); o código nasce preparado para qualquer um.

## Fora de escopo

Antifraude próprio, assinatura/recorrência, split de pagamento, boleto (avaliar depois) e
conciliação contábil.

## Contrato (independente de provedor)

```
IPaymentGateway
  Provider : string
  CreateChargeAsync(Order) -> PaymentCharge
  ParseWebhook(body, headers) -> PaymentWebhook?   // null = assinatura inválida

PaymentCharge(ProviderPaymentId, Status, QrCode?, CopyPaste?, CheckoutUrl?, ExpiresAtUtc?)
PaymentWebhook(Outcome, ProviderPaymentId, EventId, OrderId?, Amount?, RawStatus)
```

- `Outcome`: `Approved`, `Rejected`, `Pending`, `Refunded`, `Ignored`.
- Cada provedor valida **sua** assinatura dentro de `ParseWebhook`; o resto do sistema não sabe
  qual é o provedor.
- Nenhum valor vindo do navegador é aceito: o pedido é localizado pelo `ProviderPaymentId`
  gravado por nós.

## Regras de negócio

1. Pedido criado gera uma cobrança e fica `PendingPayment` até o webhook confirmar.
2. **Só o webhook** move o pedido para `Paid` — nunca uma tela de "obrigado" nem um retorno de navegador.
3. Webhook com assinatura inválida é **rejeitado** (401) e não altera nada.
4. Webhook repetido é **idempotente**: mesmo `EventId` processado duas vezes produz um único efeito.
5. Webhook fora de ordem (`Pending` depois de `Approved`) não regride o pedido.
6. Pagamento aprovado libera o pedido para preparo; a reserva de estoque deixa de ser temporária.
7. Pix não pago dentro da janela (30 min) cancela o pedido e devolve o estoque.
8. Cartão é tokenizado no provedor: o servidor nunca recebe número, validade ou CVV.

## Critérios de aceite (Dado / Quando / Então)

1. Dado um pedido `PendingPayment`, quando o provedor envia webhook assinado de aprovação, então
   o pedido vira `Paid` e o e-mail de pagamento é enviado **uma vez**.
2. Dado o mesmo webhook reenviado, então nada muda e nenhum e-mail novo sai.
3. Dado webhook com assinatura inválida, então recebo 401 e o pedido continua `PendingPayment`.
4. Dado webhook de `Pending` após `Paid`, então o status permanece `Paid`.
5. Dado Pix expirado, então o pedido é cancelado e o estoque volta.

## Testes a escrever primeiro

1. Contrato do gateway com implementação fake: cobrança criada com QR/copia-e-cola.
2. Webhook válido aprova o pedido e dispara e-mail único.
3. Webhook repetido não duplica efeito (idempotência).
4. Webhook inválido é rejeitado.
5. Expiração de Pix devolve estoque.

## Fatias de entrega

1. **Contrato + webhook idempotente** — interface, gateway fake, endpoint, tabela de eventos e testes.
2. **Pix no checkout** — QR/copia-e-cola na confirmação, com prazo e status.
3. **Adaptador do provedor escolhido** — Mercado Pago ou Asaas, com sandbox e testes de contrato.

## Definição de pronto

- CI verde com testes de webhook (válido, inválido, repetido, fora de ordem).
- Nenhum segredo no repositório; chave do webhook em variável de ambiente.
- PR aberto pelo agente, merge manual.
