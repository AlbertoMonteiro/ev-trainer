# Feature Specification: Avaliar IVs na captura

**Feature Branch**: `005-capture-iv-check`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "Eu quero evoluir a aplicação, eu quero que ela me ajude a capturar a
melhor versão daquele pokemon, onde eu vou caputar, inserir os valores de hp, atk, etc, com a nature e
ele vai me dizer se os ivs estão bons ou ruins, e se eu decidir ficar com aquele pokemon, ter a opção
de adicionar na minha lista de pokemons"

**Contexto**: hoje o app só estima IVs de um Pokémon que já está na lista, a partir das anotações de
nível. Antes de investir horas de treino, o jogador quer saber se o exemplar que acabou de capturar
vale a pena ou se é melhor soltar e capturar outro. Um Pokémon selvagem recém-capturado tem EVs zerados
na Gen 3, então nível + nature + os 6 stats da tela de resumo bastam para estimar os IVs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Avaliar um Pokémon recém-capturado (Priority: P1)

O jogador captura um Pokémon, abre a tela de resumo do jogo e, no app, abre **Avaliar captura**:
escolhe a espécie na lista (nunca digitada), informa o nível, a nature e os 6 stats que aparecem na
tela. O app mostra, para cada stat, a faixa de IV possível e uma classificação (Ótimo, Bom, Médio,
Ruim), além de um veredito geral do exemplar e do efeito da nature. Nada é gravado na lista de
Pokémon nesta etapa.

**Why this priority**: é o pedido central — decidir "fico ou solto" logo após a captura. Sozinha, já
entrega valor: o jogador pode avaliar quantas capturas quiser.

**Independent Test**: Squirtle, nível 5, Timid, stats `20/9/12/11/12/11` ⇒ faixas
`12–31, 4–31, 10–29, 20–31, 12–31, 14–31` (as mesmas da tabela de IVs atual) e, como as faixas são
largas, classificação "incerta" com a dica de que níveis mais altos dão resultado mais preciso.

**Acceptance Scenarios**:

1. **Given** a tela **Avaliar captura**, **When** o jogador a abre, **Then** vê: espécie (escolha na
   lista do Pokédex #1–386, com busca por nome ou número), nível (1–100), nature (lista das 25, com o
   stat que sobe e o que desce) e 6 campos de stat com teclado numérico.
2. **Given** espécie, nível, nature e os 6 stats preenchidos, **When** o último campo é preenchido,
   **Then** o app mostra imediatamente, para cada stat, a faixa de IV possível (calculada pelas mesmas
   regras da tabela de IVs, com EVs zero) e a classificação.
3. **Given** uma faixa que cabe inteira numa classe (ex.: `27–30`), **Then** a classificação é essa
   classe (Ótimo); **Given** uma faixa que atravessa classes (ex.: `18–23`), **Then** o app mostra as
   duas pontas ("Médio a Bom") em vez de inventar um valor.
4. **Given** um stat que nenhum IV de 0 a 31 reproduz, **Then** o app marca o stat com "?" e explica
   as causas prováveis (espécie, nível ou nature errados, ou o Pokémon já ganhou EVs em batalha).
5. **Given** uma espécie escolhida, **Then** os **stats que contam** para o julgamento já vêm marcados
   automaticamente pela espécie: todos, exceto o menor entre Ataque e SpAtk base (ex.: Alakazam
   desconsidera Ataque; Machamp desconsidera SpAtk; empate ⇒ os dois contam). O jogador pode marcar e
   desmarcar qualquer stat com um toque, e a escolha fica lembrada para as próximas avaliações daquela
   espécie.
6. **Given** os resultados, **Then** o app mostra um veredito geral do exemplar (FR-006) considerando só
   os stats que contam, e se a nature favorece (sobe um stat que conta e desce um que não conta),
   prejudica (desce um stat que conta) ou é neutra para eles; stats que não contam aparecem esmaecidos.
7. **Given** uma avaliação feita, **When** o jogador toca **Nova avaliação**, **Then** os stats são
   limpos mantendo espécie e nível, para avaliar a próxima captura da mesma espécie rapidamente.

---

### User Story 2 - Ficar com o Pokémon e adicioná-lo à lista (Priority: P2)

Satisfeito com o resultado, o jogador toca **Adicionar à minha lista**. O app cria um novo Pokémon
de treino com a espécie (e a linha evolutiva preenchida como no cadastro atual), a nature e o nível
avaliado já registrado como primeira anotação de IV — assim a tabela de IVs começa com o que foi
medido na captura e se refina com os próximos níveis.

**Why this priority**: fecha o ciclo "capturar → avaliar → treinar" sem redigitar nada; depende da
avaliação (US1).

**Independent Test**: avaliar Squirtle nível 5 Timid `20/9/12/11/12/11` e adicionar ⇒ a lista ganha
um Squirtle selecionado, com linha Squirtle → Wartortle → Blastoise, nature Timid, metas zeradas e uma
anotação de nível 5 com esses stats e EVs zero; a aba IVs mostra as mesmas faixas da avaliação.

**Acceptance Scenarios**:

1. **Given** uma avaliação com todos os campos válidos, **When** o jogador toca **Adicionar à minha
   lista**, **Then** um novo Pokémon é criado e selecionado, com a forma atual igual à espécie capturada (feature 006: sem apelido), linha
   evolutiva, nature e a anotação de nível; o aviso oferece Desfazer.
2. **Given** o Pokémon recém-adicionado, **When** o jogador abre a aba IVs, **Then** a linha do nível
   avaliado e a linha Final mostram as mesmas faixas que a avaliação mostrou.
3. **Given** uma espécie que já existe na lista, **When** o jogador adiciona, **Then** um novo Pokémon
   é criado (não substitui o existente).
4. **Given** uma avaliação com stats inválidos ou incompletos, **Then** **Adicionar à minha lista**
   fica desabilitado.
5. **Given** o Pokémon adicionado, **Then** ele entra no armazenamento local e no backup como qualquer
   outro, e o nome pode ser trocado depois em Ajustes.

---

### User Story 3 - Comparar com o melhor até agora (Priority: P3)

Para caçar o melhor exemplar, o jogador captura vários da mesma espécie. O app guarda, por espécie, o
**melhor até agora** e, a cada nova avaliação, diz se a captura atual é **melhor**, **pior** ou
**equivalente** a ele — o jogador solta as piores sem precisar anotar nada fora do app.

**Why this priority**: torna a caça repetida prática, mas a decisão "fico ou solto" já é possível com
a US1.

**Independent Test**: avaliar um Squirtle e depois um segundo com IVs maiores nos stats que contam ⇒ o
segundo é indicado como "Melhor que o melhor até agora" e passa a ser o novo melhor; um terceiro com
IVs menores é indicado como "Pior".

**Acceptance Scenarios**:

1. **Given** nenhuma avaliação guardada para a espécie, **When** a primeira avaliação válida é
   concluída (**Nova avaliação**, **Adicionar à minha lista** ou **Fechar**), **Then** ela passa a ser o
   melhor até agora (aviso com Desfazer).
2. **Given** um melhor até agora, **When** uma nova avaliação válida é feita, **Then** o app mostra a
   comparação (Melhor / Pior / Equivalente) pelo critério da FR-014 e o resumo do melhor (nível,
   nature, faixas e veredito).
3. **Given** uma avaliação melhor que a guardada, **When** ela é concluída, **Then** substitui o melhor
   até agora automaticamente, com Desfazer; enquanto não concluída, o app avisa que ela será guardada
   ao concluir; avaliações piores ou equivalentes não substituem.
4. **Given** um melhor até agora, **When** o jogador toca **Esquecer**, **Then** a espécie fica sem
   melhor guardado (com Desfazer).
5. **Given** o melhor até agora, **When** o jogador toca **Adicionar à minha lista** nele, **Then** o
   Pokémon é criado como na US2, a partir dos dados guardados.

---

### Edge Cases

- **Nível baixo**: em níveis baixos as faixas são largas (um ponto de stat cobre vários IVs); o app
  avisa que a classificação é incerta e sugere reavaliar depois de subir alguns níveis (o que, depois
  de adicionado à lista, acontece naturalmente pelas anotações de nível).
- **Pokémon que já batalhou**: se ele ganhou EVs antes da avaliação, os resultados podem sair errados
  ou com "?"; a explicação do "?" menciona essa causa. A avaliação sempre assume EVs zero.
- **HP de Shedinja** é sempre 1: o IV de HP não pode ser estimado; o app mostra "não dá para saber"
  nesse stat em vez de "?".
- **Nature com nome ou efeito confuso**: a lista mostra o stat que sobe e o que desce para evitar erro
  de escolha.
- **Espécie sem estar na linha da lista**: não se aplica — a espécie vem sempre do Pokédex embutido.
- **Avaliação abandonada**: fechar a tela sem adicionar não grava nada na lista de Pokémon (o melhor
  até agora da espécie continua guardado).
- **Stats que contam mudados depois**: a comparação com o melhor até agora usa sempre a marcação atual
  da espécie, para os dois lados.
- **Avaliação com "?"**: nunca vira melhor até agora nem é comparada; o app pede para conferir os dados.
- **Nenhum stat marcado como importante**: o veredito e a comparação ficam indisponíveis, com aviso
  pedindo para marcar ao menos um stat.
- **Sem nenhum Pokémon na lista**: a avaliação está disponível também a partir da tela vazia inicial.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O app DEVE oferecer a tela **Avaliar captura**, acessível a partir do menu e da tela
  inicial vazia, independente do Pokémon selecionado.
- **FR-002**: A espécie DEVE ser escolhida no Pokédex embutido (#1–386, busca por nome ou número),
  nunca digitada livremente; os stats base usados são os da Gen III.
- **FR-003**: O jogador DEVE informar nível (1–100), nature (uma das 25) e os 6 stats (1–999) com
  teclado numérico; campos inválidos são sinalizados e não geram resultado.
- **FR-004**: O app DEVE calcular a faixa de IV de cada stat com as mesmas regras e a mesma aritmética
  inteira da tabela de IVs existente, assumindo EVs zero.
- **FR-005**: Cada stat DEVE receber uma classificação pela faixa de IV: **Ótimo** (26–31), **Bom**
  (20–25), **Médio** (11–19), **Ruim** (0–10); faixas que atravessam classes mostram as duas pontas;
  stats impossíveis mostram "?" com explicação.
- **FR-006**: O app DEVE mostrar um veredito geral do exemplar (ex.: "Excelente", "Bom", "Fraco",
  "Incerto") derivado das classificações dos stats que contam, e DEVE indicar "Incerto" quando as
  faixas forem largas demais para decidir.
- **FR-007**: O app DEVE mostrar o efeito da nature (stat que sobe e que desce) e se ela favorece,
  prejudica ou é neutra para os stats que contam.
- **FR-013**: Os stats que contam DEVEM vir marcados automaticamente pela espécie (todos, exceto o
  menor entre Ataque e SpAtk base; empate ⇒ ambos contam), podem ser alterados pelo jogador com um
  toque por stat, e a marcação DEVE ser lembrada por espécie no aparelho.
- **FR-014**: O app DEVE guardar no aparelho, por espécie, o **melhor até agora** (espécie, nível,
  nature, stats) e comparar cada nova avaliação válida com ele: vence quem tem a maior soma dos pontos
  médios das faixas de IV dos stats que contam; diferença de até 2 pontos é "Equivalente", e no
  empate a nature favorável vence a neutra, que vence a prejudicial.
- **FR-015**: Uma avaliação válida, ao ser **concluída** (Nova avaliação, Adicionar à minha lista ou
  Fechar), DEVE virar o melhor até agora quando não houver um guardado ou quando for melhor que ele —
  nunca a cada tecla digitada; a substituição e o **Esquecer** oferecem Desfazer.
- **FR-008**: **Nova avaliação** DEVE limpar os stats mantendo espécie, nível e nature.
- **FR-009**: **Adicionar à minha lista** DEVE criar um novo Pokémon de treino com forma atual = espécie capturada,
  linha evolutiva preenchida a partir do Pokédex (como no cadastro atual), nature avaliada, metas
  zeradas e uma anotação de nível com nível, espécie, os 6 stats e EVs zero; o novo Pokémon fica
  selecionado e o aviso oferece Desfazer.
- **FR-010**: A avaliação em si NÃO DEVE gravar nada na lista de Pokémon nem no backup; só a ação
  **Adicionar à minha lista** grava. O melhor até agora e a marcação de stats que contam ficam guardados
  no aparelho como preferências, fora do backup.
- **FR-011**: Todos os textos novos DEVEM vir dos recursos de idioma (pt-BR), sem texto fixo.
- **FR-012**: A tela DEVE funcionar offline e em telas de 360 px de largura, sem rolagem horizontal,
  com alvos de toque de pelo menos 44 px.

### Key Entities

- **Avaliação de captura**: espécie (do Pokédex), nível, nature, 6 stats; resultado derivado: faixa
  de IV e classificação por stat, veredito geral, efeito da nature. Temporária.
- **Melhor até agora**: uma avaliação guardada por espécie (espécie, nível, nature, 6 stats); os
  resultados são recalculados ao exibir.
- **Stats que contam**: por espécie, 6 marcações (conta / não conta); padrão automático pela espécie.
- **Pokémon de treino** (existente): ganha uma forma nova de criação a partir de uma avaliação, com a
  primeira anotação de nível já preenchida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O jogador avalia uma captura (espécie, nível, nature e 6 stats) em menos de 45 segundos
  no celular, com o resultado aparecendo assim que o último campo é preenchido.
- **SC-002**: Para os mesmos dados, as faixas de IV da avaliação são idênticas às da tabela de IVs do
  Pokémon depois de adicionado à lista (100% dos casos de teste, incluindo o caso de interseção do
  HANDOFF).
- **SC-003**: Adicionar à lista leva um toque a partir do resultado e não exige redigitar nenhum dado.
- **SC-004**: Avaliar sem adicionar nunca altera a lista de Pokémon nem o backup.
- **SC-005**: Nenhum texto visível da tela nova fica fora dos recursos de idioma (verificado pelo
  teste de texto fixo).
- **SC-006**: Ao avaliar uma nova captura de uma espécie com melhor guardado, o jogador sabe se ela é
  melhor, pior ou equivalente sem nenhum toque extra.
- **SC-007**: A marcação automática dos stats que contam acerta o atacante principal (físico ou
  especial) de todas as espécies cujos Ataque e SpAtk base diferem.

## Assumptions

- Pokémon selvagens recém-capturados têm EVs zero na Gen 3; a avaliação não pede EVs.
- FireRed/LeafGreen não têm "juiz de IVs" nem "Characteristic"; a avaliação se baseia só em nível,
  nature e stats.
- As faixas de classificação (Ótimo 26–31, Bom 20–25, Médio 11–19, Ruim 0–10) são um padrão razoável
  e podem ser ajustadas no plano sem mudar a experiência.
- O Pokémon adicionado é identificado pela forma atual = espécie capturada (feature 006 removeu o
  apelido).
- O critério de comparação (soma dos pontos médios das faixas, margem de 2 pontos, nature como
  desempate) é um padrão simples e transparente; o plano pode refiná-lo sem mudar a experiência.
- O melhor até agora e a marcação de stats são conveniências do aparelho (como tema e filtro) e não
  entram no backup.
- O sprite da espécie aparece na avaliação como no resto do app (com o selo de fallback offline).
- Reaproveita o Pokédex embutido, o cálculo de IV e o armazenamento local já existentes; nada de rede.
