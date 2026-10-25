# Research: Equipe atual e plano de batalhas com Exp. Share

Fonte do jogo: pret/pokefirered@037335f4c725d7c9aecdac87066f2002b4bd7e14.

## R0 — Quem ganha EV por adversário derrotado (Gen 3)

`Cmd_getexp` (`src/battle_script_commands.c`) percorre a equipe a cada Pokémon adversário derrotado. Um Pokémon da
equipe chama `MonGainEVs(mon, espécie derrotada)` quando todas estas condições valem:

- ele **lutou contra o adversário**, ou seja, tem o bit em `gSentPokesToOpponent`, ou **segura o Exp. Share**;
- **não** está no nível 100;
- tem HP > 0.

`MonGainEVs` (`src/pokemon.c`) usa o rendimento **inteiro** da espécie, com estas regras:

- ×2 se **o próprio** Pokémon tem ou já teve Pokérus;
- ×2 se **ele** segura o Macho Brace.

Consequências para o plano:

- Num treinador, cada Pokémon derrotado conta separadamente. O ganho por batalha é a soma dos rendimentos do time, com
  os multiplicadores de cada um, como `TrainingGuide.StageEvs` já faz.
- Quem segura o Exp. Share não segura o Macho Brace, então ganha só ×Pokérus. Isso já é a regra de
  `BattleLog.RecordShared` (spec 012).
- Em batalha dupla, `gSentPokesToOpponent` inclui os dois Pokémon da frente, e quem participa depende de cada
  adversário. Fica fora do plano (FR-018).

**Decision**: os ganhos por batalha do plano são `rendimento do time × multiplicador`, com o mesmo `Evs.Multiplier` do
app. A batalha inteira registra cada espécie do time por `RecordBattle`, com o Exp. Share e os limites como hoje.

## R1 — Representação da equipe

**Decision**: `TrainingPokemon.Party` é `bool?`, salvo no JSON do Pokémon como `party` (omitido quando `null`). A ordem
da equipe é a `Order` que já existe; a equipe é `Ordered` filtrada por `Party == true`.

O domínio `Party.Normalize(all)` devolve os ids que mudaram e roda ao carregar, importar backup e aplicar a
sincronização:

1. Os marcados `true` ficam na equipe.
2. Se forem mais de 6, ficam os 6 com `UpdatedAt` mais recente (empate: menor `Order`, depois id). Os demais passam a
   `false`.
3. As vagas restantes são preenchidas pelos `null`, do mais recente para o mais antigo (mesmo desempate). Os `null`
   que sobrarem passam a `false`.

Isso cobre:

- dados antigos com até 6 Pokémon: todos ficam na equipe;
- dados antigos com mais de 6: ficam os 6 mais recentes (FR-007);
- o merge da sincronização com 7 ou mais marcados (FR-008).

A normalização **não** carimba `UpdatedAt`. É o mesmo comportamento de `KeepOneExpShareHolder`: um ajuste de
consistência que qualquer aparelho refaz igual, para não disputar com edições reais.

**Rationale**:

- Um campo por Pokémon entra de graça no backup, na sincronização (merge por Pokémon, mais recente vence) e na exclusão
  com desfazer, sem mudar o formato do documento nem a regra de merge.
- `null` separa "dado antigo" de "guardado de propósito".

**Alternatives considered**:

- Lista de ids da equipe no topo do documento: exigiria uma nova seção e uma nova regra de merge, como o plano da
  spec 015.
- Preferência do aparelho: a equipe não seria sincronizada, contra FR-008.
- Ordem própria da equipe: duplicaria a `Order`, que já é a ordem do usuário.

## R2 — Algoritmo do plano

O plano envolve os dois Pokémon A e B, o rendimento por batalha `Y` (vetor de 6) e os multiplicadores de Pokérus
`pA` e `pB` (1 ou 2).

As configurações e o que cada uma dá por batalha:

| # | Na frente com Macho Brace | Exp. Share | A ganha | B ganha |
|---|---|---|---|---|
| 1 | A | B | 2·pA·Y | pB·Y |
| 2 | B | A | pA·Y | 2·pB·Y |
| 3 | A | ninguém | 2·pA·Y | 0 |
| 4 | B | ninguém | 0 | 2·pB·Y |

A **capacidade** de cada Pokémon só conta os atributos em que `Y > 0`. Em cada um, ela é
`max(0, min(meta, 255) − atual)`. Há também o teto do total: a soma dos ganhos não pode passar de `510 − total atual`.

A busca:

- percorre `x1` e `x2` de 0 até o máximo que cabe;
- para cada par, `x3` é o maior número que cabe no que sobrou de A, e `x4` o maior que cabe no que sobrou de B. Mais
  batalhas sozinho só reduzem o restante, e o restante é o critério principal;
- escolhe pela ordem: menor restante somado (A + B, nos atributos do adversário), menos batalhas, menos configurações
  usadas, e por fim preferência pela configuração 1 (A é o Pokémon selecionado).

O tamanho:

- `x1` e `x2` ficam em 0…255 quando o menor ganho por batalha é 1, então são no máximo 65.536 pares de contas de 6
  posições;
- no caso típico (ganho de 6 por batalha), são cerca de 43², e o plano sai instantâneo.

A comparação "cada um sozinho" usa só as configurações 3 e 4, cada uma no máximo que cabe.

Com só 1 Pokémon, ou com B igual a A, o plano usa só a configuração 3.

**Rationale**: o restante mínimo vem primeiro porque cada ponto de restante custa pelo menos uma batalha selvagem. Os
testes conferem o resultado contra uma força bruta nas 4 dimensões em casos pequenos (SC-004).

**Alternatives considered**:

- Programação linear: o resultado precisa ser inteiro, e o espaço é pequeno.
- Guloso (alternar até acabar): não garante o mínimo; erra nos casos com Pokérus em um só.

## R3 — Busca por treinador

**Decision**: `OpponentSearch` recebe `VsSeekerGuide` e pré-normaliza, para cada treinador, os textos
`"{classe} {nome}"`, `"{nome}"` e `"{classe}"`, com `TextFold` e sem expandir rotas.

- `FindTrainers(query)` devolve os treinadores em que algum desses textos casa, pela mesma regra de substring e
  fronteira de dígito da busca atual, na ordem do guia.
- `Find(query, version)` passa a incluir as espécies dos times desses treinadores, de todas as revanches, como
  `SearchHit` com `TrainerLocations = [local do treinador]`. Uma espécie já presente apenas recebe o local, sem
  duplicar.

O cartão mostra todas as revanches. As que exigem a Elite Four levam a marca "após a Elite Four", como no guia.

**Rationale**:

- As classes e os nomes do guia estão em inglês, como no jogo, e o jogador digita como vê na tela ("Engineer Bernie").
- Sem expandir rotas, um nome nunca casa com "Rota 11"; a busca por local continua como está.

**Alternatives considered**:

- Mostrar cartões também na busca por local: a busca "Rota 4" traria dezenas de cartões e esconderia a grade.

## R4 — Registrar a batalha inteira

**Decision**: `TrainerStore.RecordTeam(IReadOnlyList<string> team)` chama `RecordBattle` para cada espécie, na ordem do
time, e devolve a lista de `SharedRecord`. Por isso o agrupamento de eventos funciona sozinho: "Magneton ×3" vira um
evento com `Count = 3`.

- O Desfazer chama `UndoBattle` para cada registro, **em ordem inversa**.
- O aviso soma os ganhos do principal e do portador do Exp. Share, com o texto da spec 012.

**Rationale**: zero regra nova de EV (Princípio IV), e o histórico fica igual ao de registrar um a um.

## R5 — Aplicar configuração

**Decision**: `TrainerStore.ApplyConfiguration(leadId, holderId?)` faz, num único `Commit`:

- seleciona `leadId`;
- marca o Macho Brace em `leadId`;
- tira o Macho Brace de `holderId` e do parceiro do plano;
- dá o Exp. Share a `holderId`, ou o tira de todos quando é `null`.

O método devolve um Desfazer que restaura a seleção, os dois Macho Brace e o portador anterior. Se o portador ou o da
frente estiver guardado, a página pergunta antes e chama `JoinParty`. Com a equipe cheia, a página leva à tela da
equipe.

**Rationale**: o Macho Brace é único no jogo, mas o app nunca o controlou como único (spec 012). Por isso só os dois do
plano são ajustados, sem mexer em outros Pokémon.

## R6 — Navegação e telas

**Decision**:

- `Home` ganha `_party` e `_expPlan`, no mesmo padrão de guia, liga e sincronização.
- Entradas para a tela da equipe: item "Equipe" no menu ☰, botão no painel lateral, e o aviso de equipe cheia ao
  cadastrar.
- Entradas para o plano:
  - o link "Planejar batalhas com Exp. Share" no `EvSummary`, ao lado do seletor do Exp. Share, que abre com o
    Pokémon selecionado e o portador;
  - o botão do `TrainerCard`, que abre com o treinador e a revanche já escolhidos.
- O `PokemonList` recebe a equipe. Se o selecionado estiver guardado, ele aparece depois, sob o rótulo "Aberto
  (guardado)".
- O seletor do Exp. Share no `EvSummary` lista só os Pokémon da equipe, mais o portador atual se ele estiver guardado,
  para não esconder o estado.

**Alternatives considered**: rota própria (`/party`). O app é uma página só, com painéis, e o 404.html já serve a raiz;
uma rota nova não traria ganho.
