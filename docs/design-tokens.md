# Design tokens — Iscuissati’s Saboaria Artesanal

Origem: logotipo (`assets/logo-original.jpeg`) e protótipos em `docs/design/`. Os hex são aproximados: confirme com conta-gotas no logo original e ajuste aqui, em um só lugar.

## Cores

| Token | Hex | Uso |
|---|---|---|
| `--color-primary` | #A9482B | Terracota: títulos de destaque, botões, links |
| `--color-olive` | #6B7A45 | Verde-oliva: ícones, divisores, ornamentos (não usar em texto pequeno) |
| `--color-olive-dark` | #4F5C30 | Faixa "Sobre" (texto creme por cima) e textos de apoio verdes |
| `--color-bg` | #F3EAD8 | Fundo principal (creme kraft) |
| `--color-surface` | #FBF7EF | Cartões, cabeçalho, seções alternadas |
| `--color-text` | #3A2A20 | Texto e rodapé (fundo) |
| `--color-text-muted` | #5E4A2E | Texto secundário |
| `--color-border` | #D9C9AA | Bordas e linhas |
| `--color-input-border` | #7A6544 | Borda de campos de formulário |

Contrastes calculados (WCAG): terracota sobre creme ≈ 4,8:1; branco sobre terracota ≈ 5,7:1; creme sobre verde-oliva escuro ≈ 6:1; verde-oliva #6B7A45 sobre creme ≈ 3,9:1 (só decorativo ou texto grande).

```css
:root {
  --color-primary: #A9482B;
  --color-olive: #6B7A45;
  --color-olive-dark: #4F5C30;
  --color-bg: #F3EAD8;
  --color-surface: #FBF7EF;
  --color-text: #3A2A20;
  --color-text-muted: #5E4A2E;
  --color-border: #D9C9AA;
  --color-input-border: #7A6544;
  --radius-button: 4px;
  --radius-card: 8px;
}
```

No Tailwind (v4), declare os mesmos valores em `@theme` para que as classes usem os tokens.

## Tipografia

| Papel | Fonte | Uso |
|---|---|---|
| Títulos | Oswald 500/600/700, caixa alta, `letter-spacing` .03–.08em | h1 `clamp(38px, 6vw, 60px)`, h2 34px (26px no mobile) |
| Destaque | Yellowtail (script) | Subtítulos e slogans, com moderação (24–36px) |
| Texto | Lato 400/700, 16px, `line-height` 1.55 | Corpo, preços (700) |

Hospede as fontes no próprio site.

## Componentes e medidas

- Container: `max-width` 1200px, padding lateral 24px (20px no mobile).
- Botão primário: fundo terracota, texto branco, Oswald caixa alta, `letter-spacing` .08em, raio 4px, altura mínima 48–54px. Botão secundário: borda 2px terracota, texto terracota.
- Cartão: fundo `--color-surface`, borda 1px `--color-border`, raio 8px, padding 22–28px.
- Card de produto: imagem 4:3 (1:1 no mobile) com fundo em tom pastel do produto, nome em Oswald caixa alta, preço em Lato 700, botão "Adicionar à sacola".
- Grades responsivas com `repeat(auto-fit, minmax(240px, 1fr))`, sem media queries para o básico.
- Seções: barra de aviso (terracota), cabeçalho (surface), hero com selo circular, faixa de confiança, categorias, destaques, "Sobre" (verde-oliva escuro), newsletter, rodapé (marrom escuro).
- Selo circular do logo: círculo com gradiente creme, borda dupla marrom, folha verde-oliva, nome em terracota e script embaixo.

## Acessibilidade

- Alvos de toque de no mínimo 44px; botões só com ícone levam `aria-label`.
- Todo campo tem `label` visível; estados de foco visíveis (adicione `outline` de 3px terracota com offset de 2px, que os protótipos ainda não definem).
- Imagens de produto com `alt` descritivo; ornamentos com `aria-hidden`.
