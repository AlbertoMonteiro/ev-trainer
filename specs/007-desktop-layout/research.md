# Research: Layout para desktop

Sem itens "NEEDS CLARIFICATION": a única dúvida (painel lateral) foi respondida (Q1 = A) e a
constituição foi emendada (v1.2.0, Princípio VI).

## R1. Pontos de quebra

- **Decision**: dois pontos de quebra por largura (`min-width`), só em CSS:
  - `< 1024 px`: layout atual, inalterado (coluna de até 560 px);
  - `≥ 1024 px` (**desktop**): conteúdo em até **1440 px**, centralizado acima disso; blocos lado a
    lado por tela;
  - `≥ 1280 px` (**desktop com painel**): painel lateral de Pokémon fixo à esquerda (240 px).
- **Rationale**: 1024 é a menor largura comum de notebook/janela meio-a-meio em monitor grande; abaixo
  disso, dois blocos lado a lado ficariam apertados. O painel só a partir de 1280 deixa ≥ 1000 px para
  o conteúdo. Constituição VI: adaptação pela largura, nunca por dispositivo.
- **Alternatives considered**: ponto intermediário de tablet (768) — mais CSS para pouco ganho; o
  tablet em pé fica bem no layout de celular (edge case da spec).

## R2. Só CSS, sem re-renderizar por largura

- **Decision**: nenhum componente decide o que renderizar pela largura da janela. Os componentes
  ganham classes de região (ex.: `ev-layout`, `ev-summary-area`) e o CSS reorganiza com
  `display: grid` / `grid-template-areas` dentro de `@media (min-width: 1024px)`. O painel lateral é
  sempre renderizado e fica `display: none` abaixo de 1280 px.
- **Rationale**: redimensionar não desmonta nada, então dados digitados, aba e seleção são preservados
  por construção (FR-009). Sem JavaScript novo (Restrições Técnicas) e sem `matchMedia` via interop.
- **Alternatives considered**: componente de "breakpoint" com `matchMedia` via JS — viola a regra de
  interop mínimo e re-renderiza ao redimensionar, perdendo estado.

## R3. Ordem no celular preservada

- **Decision**: os filhos de cada região continuam na mesma ordem do DOM de hoje; no desktop, as
  posições vêm de `grid-template-areas`. Ex.: aba EVs mantém a ordem resumo → busca → recentes → lista
  → histórico; no desktop, o resumo vai para uma coluna à esquerda (fixa com `position: sticky`) e o
  resto à direita.
- **Rationale**: SC-004 (celular idêntico) e leitura por tecnologia assistiva na ordem lógica.

## R4. Aba EVs

- **Decision**: grade `340px 1fr`; coluna esquerda = resumo de EVs, `position: sticky` abaixo do
  cabeçalho; coluna direita = busca, recentes, lista principal e histórico. A grade de adversários
  passa de 2 colunas fixas para `repeat(auto-fill, minmax(160px, 1fr))`, o que dá ≥ 4 colunas em
  1366 px com o painel lateral (≈ 750 px úteis).
- **Rationale**: FR-003 / SC-002; 160 px é a largura que um card já ocupa num celular de 360 px.

## R5. Aba IVs, Ajustes e Avaliar captura

- **Decision**:
  - IVs: `iv-layout` com tabela + IVs conhecidos à esquerda e o formulário de anotação à direita.
  - Ajustes: `settings-layout` com o formulário do Pokémon à esquerda e backup / importação / apagar à
    direita.
  - Avaliar captura: `CaptureView` ganha `capture-inputs` (campos e ações) e `capture-output`
    (resultado e melhor até agora), lado a lado; o cabeçalho do card ocupa as duas colunas.
- **Rationale**: FR-004–FR-006 / SC-003. Os wrappers são divs neutras no celular.
- **Risco**: o analisador de closures do Blazor no .NET 11 RC1 (AD0001) quebra a compilação completa
  quando um `@for` e lambdas posteriores dividem o mesmo método de render; mexer no markup de
  `CaptureView` exige validar com `dotnet build --no-incremental` (ver memória do projeto).

## R6. Painel lateral (US3)

- **Decision**: extrair a lista de Pokémon do `PokemonMenu` para `Shell/PokemonList.razor` (item com
  sprite, forma atual, total de EVs, selecionado com `aria-current`), usada pelo menu e pelo novo
  `Shell/PokemonSidebar.razor` (`<aside>` com título, a lista e "+ Adicionar Pokémon"). Clicar
  seleciona (`TrainerStore.Select`), mantém a aba e fecha a avaliação de captura se estiver aberta.
  A partir de 1280 px a lista dentro do menu ☰ fica oculta (o resto do menu continua).
- **Rationale**: uma só marcação de item para menu e painel; trocar de Pokémon com 1 clique (FR-011).
- **Alternatives considered**: painel recolhível (opção C) — descartado pela resposta Q1 = A.

## R7. Aviso (toast), hover e foco

- **Decision**: no desktop o toast fica no canto inferior direito, com largura de 420 px, sem cobrir a
  grade central (FR-007). Estados de hover só com `@media (hover: hover)`; foco com o
  `:focus-visible` existente reforçado nos cards e botões (FR-008).

## R8. Como verificar (Princípio III)

- **Decision**: o que é estrutura vira teste bUnit escrito antes (regiões e ordem do DOM, painel
  lateral: lista, destaque, clique mantém a aba, fecha a captura, adicionar; menu e painel usam a
  mesma lista). O que é puramente visual (pontos de quebra, colunas, sticky, ausência de rolagem
  horizontal) é verificado no navegador com medições objetivas do [quickstart](./quickstart.md)
  (largura usada, cards por linha, `scrollWidth`), registradas na tarefa de validação.
- **Rationale**: bUnit não aplica CSS; medições no navegador transformam os SCs em números.
