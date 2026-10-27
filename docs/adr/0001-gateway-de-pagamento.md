# ADR 0001 — Gateway de pagamento

**Status:** proposta (ainda não decidido) · **Data:** 27/10/2026

## Contexto
Loja pequena de sabonetes, ticket médio provavelmente baixo, operada por uma pessoa, com Pix como forma de pagamento prioritária. Critérios: menores taxas e integração mais simples em .NET.

## Dados pesquisados
Taxas publicadas em comparativo de 17/10/2026 (dardo.studio/pt/blog/gateway-de-pagamento), que se baseia nos sites oficiais. **Confirme nas páginas oficiais antes de contratar:** as taxas mudam e dependem da análise da conta.

| Gateway | Pix | Cartão de crédito | Boleto |
|---|---|---|---|
| Mercado Pago | 0,99% | 3,98% (30 dias) ou 4,98% (na hora) | R$ 3,49 |
| Asaas | R$ 1,99 fixo | 2,99% + R$ 0,49 (cerca de 32 dias) | R$ 1,99 |
| Pagar.me | 0,99% | 4,19% (1 dia) | não levantado |
| InfinitePay | grátis | 4,20% (1 dia) | não tem no checkout |
| Efí | 1,19% | 3,49% (até 31 dias) | não levantado |

O Pix fixo do Asaas só fica mais barato que 0,99% em vendas acima de R$ 201; num pedido de R$ 60, são R$ 0,59 (Mercado Pago) contra R$ 1,99 (Asaas).

## Decisão proposta
Começar com o **Mercado Pago** (Checkout Pro, redirecionamento, sem tocar em dados de cartão) atrás de `IPaymentGateway`, trocando para Asaas ou outro se o ticket médio se mostrar alto. Validar o SDK .NET e a documentação atuais na Fase 5.

## Consequências
- A interface `IPaymentGateway` isola o provedor; trocar de gateway não reescreve o checkout.
- Considerar a taxa de pagamento na precificação dos produtos.
- Conta nova pode ter retenção de saldo em alguns provedores: conferir antes do lançamento.
