# Research: Avaliar IVs na captura

Decisões de design para a feature 005. Não há itens "NEEDS CLARIFICATION" no Technical Context:
as duas dúvidas de escopo foram respondidas na spec (Q1 = D, Q2 = C).

## R1. Onde fica a tela de avaliação

- **Decision**: uma **tela própria** dentro de `Home`, aberta pelo item **Avaliar captura** do menu e
  por um botão na tela vazia; ela substitui as abas enquanto aberta e tem **Fechar** (volta para a
  aba EVs, ou para a tela vazia). Implementado como um estado de visão em `Home` (`View.Capture`),
  não como uma 4ª aba.
- **Rationale**: as abas EVs/IVs/Ajustes pertencem ao Pokémon selecionado; a avaliação é global e
  precisa funcionar sem nenhum Pokémon (edge case "Sem nenhum Pokémon na lista").
- **Alternatives considered**: rota `/captura` — o fallback do GitHub Pages (`404.html` →
  `NotFound` → home) perderia o deep link e o estado; 4ª aba — confunde "dados do Pokémon" com
  "ferramenta de captura" e não existe na tela vazia.

## R2. Reaproveitar o cálculo de IV (SC-002)

- **Decision**: extrair de `Ivs.RangeFor(LevelEntry, TrainingPokemon, ReferenceData)` uma sobrecarga
  pura `Ivs.RangeFor(SpeciesEntry? species, Nature? nature, int level, int[] stats, int[] evs)`; a
  versão antiga passa a delegar para ela. A avaliação chama a nova sobrecarga com EVs zero.
- **Rationale**: uma única implementação garante que as faixas da avaliação e da tabela de IVs do
  Pokémon adicionado sejam idênticas (SC-002) por construção.
- **Alternatives considered**: montar um `TrainingPokemon` temporário — funciona, mas esconde a
  dependência e aloca à toa; duplicar a fórmula — viola o Princípio IV (uma fonte das regras).

## R3. Shedinja (HP sempre 1)

- **Decision**: novo caso `IvUnknowable : IvCell` quando o stat é HP e o base de HP é 1 (só
  Shedinja). Exibido como "não dá para saber" (avaliação) e com o mesmo texto curto na tabela de IVs;
  `Final` ignora o stat (fica `FinalNoData`), a avaliação o trata como "não conta".
- **Rationale**: na Gen 3 o HP de Shedinja é sempre 1, então nenhum IV o reproduz pela fórmula e hoje
  a tabela mostraria "?" (falso alarme). `IvCell` é `closed`: o compilador aponta todos os `switch`.
- **Alternatives considered**: tratar como `IvNoData` — esconderia a razão; ignorar o caso — o
  jogador veria "?" e acharia que digitou errado.

## R4. Classificação, veredito e efeito da nature

- **Decision** (tudo em `Domain/Capture.cs`, C# puro):
  - `IvGrade` por IV: Ruim 0–10, Médio 11–19, Bom 20–25, Ótimo 26–31 (FR-005). Uma faixa
    `Min–Max` vira `GradeSpan(Grade(Min), Grade(Max))`; "Médio a Bom" quando diferem.
  - Veredito (FR-006) sobre os stats que contam e têm faixa: média dos mínimos e média dos máximos
    (aritmética inteira, arredondando para baixo) → classe de cada média (Fraco < 11 ≤ Médio < 20 ≤
    Bom < 26 ≤ Excelente). Mesma classe ⇒ esse veredito; classes diferentes ⇒ **Incerto** ("entre X e
    Y"). Algum stat que conta com "?" ou nenhum stat que conta ⇒ veredito indisponível.
  - Efeito da nature (FR-007): neutra (sem stat que sobe) ⇒ **Neutra**; desce um stat que conta ⇒
    **Prejudica**; senão sobe um stat que conta ⇒ **Favorece**; senão ⇒ **Neutra**.
- **Rationale**: regras curtas, explicáveis na tela e testáveis com tabelas; "Incerto" cai
  naturalmente de faixas largas sem um limiar arbitrário de largura.
- **Alternatives considered**: pontuação em stat final no nível 100 — mais "exata", porém opaca para
  o jogador e dependente de base stats; limiar fixo de largura para "Incerto" — arbitrário.

## R5. Stats que contam (Q1 = D)

- **Decision**: padrão `CountedStats.Default(base)`: todos contam, exceto o menor entre Ataque (1) e
  SpAtk (3) base; empate ⇒ ambos contam. O jogador alterna cada stat com um botão
  (`aria-pressed`); a escolha é gravada por espécie e volta a ser usada nas próximas avaliações. Um
  botão **Restaurar padrão** apaga a escolha da espécie.
- **Rationale**: acerta o atacante principal sem toques (SC-007) e deixa o jogador corrigir casos
  especiais (ex.: Pokémon misto).

## R6. Melhor até agora e o momento de guardar (Q2 = C)

- **Decision**: o resultado e a comparação com o melhor guardado aparecem **ao vivo** enquanto o
  jogador digita, mas a avaliação só é **concluída** — e só então pode virar o melhor até agora —
  quando o jogador toca **Nova avaliação**, **Adicionar à minha lista** ou **Fechar**. Ao concluir uma
  avaliação válida, sem "?", que seja melhor (ou a primeira da espécie), ela substitui o melhor com
  aviso e Desfazer. Enquanto não concluída, o cartão diz "Melhor que o melhor até agora — será
  guardado ao concluir".
- **Rationale**: guardar a cada tecla gravaria estados intermediários (digitar "2" antes de "20"
  produz uma avaliação válida e errada). Os três botões são exatamente os pontos onde o jogador
  terminou de olhar o exemplar.
- **Alternatives considered**: botão "Guardar como melhor" — mais um toque e contraria FR-015;
  debounce por tempo — imprevisível no celular.
- **Comparação (FR-014)**: pontuação = soma de `Min + Max` das faixas dos stats que contam (o dobro
  da soma dos pontos médios, para ficar em inteiros); diferença ≤ 4 (2 pontos) ⇒ empate; no empate
  decide a nature (Favorece > Neutra > Prejudica); persistindo ⇒ **Equivalente**. Os dois lados usam a
  marcação de stats que contam atual da espécie.

## R7. Onde guardar o melhor até agora e a marcação

- **Decision**: um documento separado no `localStorage`, chave `evtrainer:v1:capture`, lido e gravado
  por dois métodos novos de `IPokemonRepository` (`LoadCaptureNotes` / `SaveCaptureNotes`), com JSON
  tolerante via o mesmo source generator. Fica **fora do backup**, como tema e filtro.
- **Rationale**: `UiPreferences` é um `record` comparado por valor (teste de ida e volta); dicionários
  quebrariam a igualdade. Um documento próprio mantém as preferências simples e não mexe no formato
  do backup do HANDOFF.
- **Alternatives considered**: dentro do backup — exigiria estender o formato do HANDOFF por algo que
  é conveniência de caça; dentro de `UiPreferences` — ver acima.

## R8. Escolha da espécie

- **Decision**: extrair a busca do `SpeciesPicker` (campo + lista de opções com sprite e número) para
  `Components/Shared/SpeciesSearch.razor`, que emite `PokedexSpecies`; `SpeciesPicker` e a avaliação
  passam a usá-la. A espécie escolhida aparece como chip com sprite e botão **Trocar**.
- **Rationale**: mesma experiência de busca (nome, número, acentos) em todo o app sem duplicar
  marcação.

## R9. Adicionar à lista

- **Decision**: `TrainerStore.AddFromCapture(CaptureInput input, Pokedex dex)` cria o Pokémon com
  forma atual (`CurrentSpecies`, feature 006) = espécie capturada, `Species = dex.LineFor(espécie)`, nature, metas zero e
  `IvLog = [LevelEntry { Level, Species, Stats, Evs = 0 }]`, seleciona-o e retorna um Desfazer que o
  remove e restaura a seleção anterior. A tela de avaliação fecha e abre a aba IVs do novo Pokémon.
- **Rationale**: reaproveita o mesmo formato de anotação de nível; a aba IVs mostra imediatamente as
  faixas da avaliação (SC-002).

## R10. Emenda: importância em 3 níveis (2026-10-11)

- **Problema**: com peso igual, o Machop Docile Nv 16 (`50/31/25/21/19/20`, ATK 3–8) somou 187 e o Hasty
  Nv 17 (`52/37/21/19/16/22`, ATK 29–31) somou 152; o app apontou o Docile como melhor, embora o ataque
  dele seja inaceitável para um atacante físico.
- **Decision**: `enum StatImportance { Ignored = 0, Counts = 1, Main = 3 }`, com o valor do enum sendo
  o próprio peso. Padrão `Capture.DefaultImportance(base)`: o maior entre Ataque (1) e SpAtk (3) base é
  `Main`, o menor é `Ignored`, os demais `Counts`; empate ⇒ ambos `Counts`. Toque alterna
  `Ignored → Counts → Main → Ignored`. Substitui R5 (`bool[]`).
  - Veredito (R4): média **ponderada** dos mínimos e dos máximos (`Σ peso × Min / Σ peso`, divisão
    inteira) sobre os stats com peso > 0.
  - Pontuação (R6): `Σ peso × (Min + Max)`; margem de empate e desempate pela nature inalterados.
  - Efeito da nature: "conta" = peso > 0 (regra de R4 inalterada).
  - Caso Machop: Docile 209 × Hasty 272 ⇒ Hasty **Melhor**.
- **Rationale**: peso 3 faz um ataque claramente melhor vencer diferenças moderadas nos outros stats;
  peso 2 venceria por pouco (212 × 198), frágil a variações. Um nível explícito "principal" deixa o
  motivo visível na tela e permite a Pokémon de suporte rebaixar o ataque com um toque.
- **Alternatives considered**: peso numérico livre por stat — flexível, mas exige explicar números ao
  jogador; regra "ataque é eliminatório" — rígida demais (descartaria um exemplar com ataque Bom por um
  com Ótimo e o resto péssimo).
- **Compatibilidade**: o documento `evtrainer:v1:capture` passa a gravar `importance` (`"main"`,
  `"counts"`, `"ignored"`); a leitura aceita o antigo `counted` (`false` ⇒ ignora; `true` ⇒ o padrão da
  espécie, ou `counts` quando o padrão for `ignored`).
