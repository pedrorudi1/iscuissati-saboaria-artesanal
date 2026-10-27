# Spec 06 — Frete

## Objetivo

Cobrar o frete correto no checkout, com regras que a dona da loja consiga operar sozinha e sem
depender de conta em agregador para começar a vender.

## Fora de escopo

Etiqueta e rastreio automáticos (dependem de conta no Melhor Envio ou contrato com os Correios),
cálculo de frete na página do produto, e frete internacional.

## Estratégia

Duas camadas, com o provedor atrás de interface:

1. **Tabela própria** (entra agora): faixas por região do CEP e peso, retirada local gratuita e
   frete grátis acima de um valor. Não depende de serviço externo, funciona no primeiro pedido.
2. **Melhor Envio** (entra depois, sem reescrever o checkout): cotação real com desconto e etiqueta.
   O adaptador só troca a implementação de `IShippingProvider`.

Se o provedor externo falhar, a tabela própria é o **fallback** — a loja nunca fica sem opção de entrega.

## Regras de negócio

1. Faixa de preço é definida por **região (1º dígito do CEP)** e **peso total** do pedido.
2. Peso total = soma de `WeightGrams` das variações no carrinho.
3. **Frete grátis** quando o subtotal atinge o valor configurado; a opção aparece com preço 0.
4. **Retirada local** é sempre gratuita quando habilitada, com endereço e horário nas instruções.
5. CEP fora das faixas cadastradas não inventa preço: o checkout informa que ainda não entrega ali.
6. O prazo exibido é **dias úteis** e já inclui o prazo de preparo configurado.
7. O servidor **recalcula o frete** no POST; o navegador manda apenas o **código** da opção escolhida,
   nunca o preço.
8. Pedido sem frete escolhido não é criado.

## Configuração (appsettings, seção `Shipping`)

```
Shipping:
  OriginZip: "18300000"
  FreeShippingThreshold: 150
  HandlingDays: 2
  PickupEnabled: true
  PickupLabel: "Retirar em Tatuí"
  PickupInstructions: "Rua ..., Tatuí/SP — combinar horário por WhatsApp"
  DefaultItemWeightGrams: 150
  Bands:
    - { CepPrefix: "1", MaxWeightGrams: 500,  Price: 18.90, Days: 3 }
    - { CepPrefix: "1", MaxWeightGrams: 2000, Price: 24.90, Days: 3 }
    - { CepPrefix: "0", MaxWeightGrams: 500,  Price: 22.90, Days: 5 }
```

## Critérios de aceite (Dado / Quando / Então)

1. Dado um carrinho de R$ 160 e frete grátis acima de R$ 150, quando abro o checkout, então
   aparece opção de entrega com preço R$ 0,00.
2. Dado um carrinho de R$ 40 para CEP de São Paulo (prefixo 1), então o valor vem da faixa de
   peso correspondente.
3. Dado que escolho a opção de frete X, quando envio o checkout, então o pedido é criado com
   `Shipping` igual ao preço **recalculado** no servidor.
4. Dado um POST com `shippingCode` inexistente, então nenhum pedido é criado.
5. Dado CEP de região não cadastrada, então o checkout avisa e não cria pedido.
6. Dado retirada local habilitada, então a opção aparece com preço 0 e instruções visíveis.

## Testes a escrever primeiro

1. Unidade: frete grátis acima do limite.
2. Unidade: faixa por prefixo de CEP e peso (borda de peso inclusa).
3. Unidade: CEP sem faixa devolve lista vazia (sem exceção).
4. Unidade: retirada local presente e gratuita.
5. Integração: checkout cria pedido com `Shipping` recalculado e total correto.
6. Integração: código de frete inválido não cria pedido.

## Fatias de entrega

1. **Tabela própria + checkout** — provedor, regras, tela e recálculo no servidor.
2. **Melhor Envio** — adaptador com OAuth, cotação real e fallback para a tabela.
3. **Etiqueta e rastreio** — quando houver conta/contrato, com código no admin.

## Definição de pronto

- CI verde com unidade e integração.
- Nenhum preço de frete aceito do navegador.
- Configuração alterável sem deploy de código.
- PR aberto pelo agente, merge manual.
