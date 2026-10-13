# Research: Guia de treino com VS Seeker

## R1 — Fonte dos dados

- **Decision**: decompilação `pret/pokefirered` (commit fixo no gerador): `src/vs_seeker.c` (`sRematches`),
  `src/data/trainers.h`, `src/data/trainer_parties.h`.
- **Rationale**: é o código do próprio jogo — times, níveis, batalha dupla e regra de progresso das revanches exatos
  (Princípio IV). Conferido: Engineer Bernie = estágio 0 `Magnemite 18, Magnemite 18, Magneton 18` e estágio 3
  `Magneton 28 ×3` (confirma o relato do jogador); Lass Julia só tem estágio 0 (a revanche repete os 2 Clefairy).
- **Alternatives considered**: Dragonfly Cave / Serebii / Marriland — úteis, mas incompletas, divergentes e sem a regra
  de progresso; Bulbapedia — bloqueada para busca automática.

## R2 — Regra de progresso dos estágios

- **Decision**: `TryGetRematchTrainerIdGivenGameState` define o índice do estágio e a condição para ele valer:

  | Índice | Condição no jogo | Rótulo no app |
  |---|---|---|
  | 0 | — (batalha original) | Original |
  | 1 | Ter o VS Seeker | Revanche |
  | 2 | Ter visitado Celadon | Após Celadon |
  | 3 | Ter visitado Fuchsia | Após Fuchsia |
  | 4 | Ter derrotado a Elite Four | Após a Elite Four |
  | 5 | Conexão com Ruby/Sapphire liberada (fim da missão das Ilhas Sevii) | Após as Ilhas Sevii |

  Índices com `SKIP` (ou repetindo o id original) não criam estágio novo: a revanche repete o estágio anterior. Sem a
  condição, o jogo usa o maior estágio anterior disponível.
- **Mapeamento para as fases da spec**: "antes da Elite Four" = estágios 0–3; "depois da Elite Four" = 0–5, com 4 e 5
  marcados (o 5 com o rótulo "após as Ilhas Sevii"). Cada estágio mostra seu rótulo, o que cobre jogadores que ainda
  não visitaram Celadon/Fuchsia.
- **Rationale**: cumpre a clarificação (todos os estágios em ordem, filtro só pela Elite Four) sem perder a precisão do
  jogo.
- **Alternatives considered**: um seletor com as 6 condições — mais preciso, mas mais pesado para o jogador; a spec pede
  antes/depois da Elite Four.

## R3 — O que entra no guia

- **Decision**: todo treinador de `sRematches` cujo mapa é de Kanto (`MAP_ROUTE*`, sem ilhas/`*_ISLAND*`/`SEVAULT`/
  `THREE_ISLE`…); classes/nomes como no jogo (classe em inglês legível, ex.: `TRAINER_CLASS_COOLTRAINER` → "Cooltrainer").
  Como `sRematches` só contém treinadores de rotas, prédios/ginásios ficam de fora naturalmente.
- **Rationale**: FR-003, FR-008; o VS Seeker só funciona com quem está nessa tabela.

## R4 — EVs calculados, não gravados

- **Decision**: o JSON guarda espécie + nível; `TrainingGuide` soma o `evYield` da Pokédex Gen III (já embutida) para
  cada estágio. Nomes do decomp (`NIDORAN_F`, `MR_MIME`, `FARFETCHD`) são mapeados para os nomes da Pokédex no gerador;
  o teste de integridade garante que todos existem.
- **Rationale**: SC-005 vira propriedade do código; uma fonte de verdade para rendimento.

## R5 — Notas de acesso

- **Decision**: o gerador marca `double` (de `doubleBattle`) e uma lista de condições por mapa: Rotas 19, 20, 21 →
  "precisa de Surf"; Rota 17 (e trecho de Cycling Road da 16/18) → "Cycling Road (bicicleta)"; Rota 23 → "Rota da
  Vitória (8 insígnias)". Referências finas de posição ("abaixo do degrau") ficam fora desta versão.
- **Rationale**: FR-004 com dados objetivos; posição exata exigiria curadoria manual por treinador.

## R6 — Ordenação e atributo inicial

- **Decision**: para o atributo A e a fase F, cada treinador entra se algum estágio disponível em F dá A > 0; chave =
  maior A entre esses estágios; desempate: menor nível máximo desse estágio, depois ordem do mapa/nome. Atributo
  inicial = entre HP/Atk/SpA/Spe, o de maior (meta − atual) > 0 do Pokémon selecionado; senão HP.
- **Rationale**: FR-003, FR-006b, FR-010.

## R7 — Página

- **Decision**: `TrainingGuideView` aberta pelo menu ("Onde treinar") e por um link na aba EVs, no padrão de
  `SyncView`/`CaptureView` (`Home` alterna a vista). Topo: seletor segmentado de atributo (HP/ATK/SPA/SPE), seletor de
  fase (antes/depois da Elite Four), nota de multiplicadores e VS Seeker, nota "Ilhas Sevii não incluídas". Lista de
  cartões: cabeçalho (classe + nome, local, marcas), estágios em ordem com time, níveis, EVs (atributo em destaque,
  "também dá" para os outros) e rótulo do estágio; o melhor estágio destacado; aviso de EV desperdiçado. Rodapé com
  fontes.
- **Rationale**: FR-001, FR-002, FR-004, FR-009, FR-011, FR-012.
