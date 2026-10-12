# Research: Exp. Share

Não havia itens NEEDS CLARIFICATION no contexto técnico; abaixo, as decisões de desenho.

## R1 — Regra da Gen 3

- **Decision**: o portador recebe o rendimento de EV inteiro do adversário × `(portador.Pokerus ? 2 : 1)`; o Macho
  Brace (do principal ou do portador) não entra; vitaminas não são compartilhadas.
- **Rationale**: na Gen 3, EVs não são divididos entre participantes/portador do Exp. Share (só a experiência é);
  Pokérus é do Pokémon que o tem; Macho Brace é um item segurado — o portador está segurando o Exp. Share.
- **Alternatives considered**: dividir os EVs como a experiência — incorreto na Gen 3.

## R2 — Onde guardar o portador

- **Decision**: flag `ExpShare` em cada `TrainingPokemon` (JSON `"expShare"`, gravado só quando `true`); o
  `TrainerStore` garante no máximo um `true`.
- **Rationale**: segue o padrão de `Pokerus`/`MachoBrace`; entra no backup e na sincronização por Pokémon sem
  formato novo (FR-014); apagar o Pokémon apaga a marcação, desfazer a exclusão a devolve (edge case).
- **Alternatives considered**: id do portador nas preferências do aparelho — não iria no backup nem na sync; campo
  global no backup — exigiria mudar o formato de documentos e o merge por Pokémon.

## R3 — Portador único depois de sincronizar/importar

- **Decision**: após `Load`, `ImportBackup` e `ApplySync`, se mais de um Pokémon tiver `ExpShare`, fica o de
  `UpdatedAt` mais recente (empate: menor `Order`); os outros são desmarcados e salvos.
- **Rationale**: marcar o portador num aparelho carimba esse Pokémon; o mais recente reflete a última escolha.
- **Alternatives considered**: rejeitar o merge — inaceitável; manter dois portadores — viola FR-002.

## R4 — Marca da batalha compartilhada

- **Decision**: `BattleEvent.Shared` (JSON `"x": true`, só quando verdadeiro). Agrupa (`Count++`) só com o último
  evento se ele for compartilhado, do mesmo adversário canônico e mesmo multiplicador; senão cria evento novo.
- **Rationale**: FR-011 (histórico identifica "Exp. Share"); impede que batalhas próprias e compartilhadas se
  misturem num mesmo grupo; contagens ×N e Recentes continuam somando todos os eventos (edge case da spec).
- **Alternatives considered**: sem marca — o histórico não distinguiria; evento separado com referência à batalha
  original — complexidade sem ganho.

## R5 — Desfazer

- **Decision**: `RecordBattle` devolve `SharedRecord(Main, Holder?)` com os ids e ganhos; o Desfazer do aviso chama
  `UndoLast` no principal e `UndoLastShared` no portador (remove 1 unidade do último evento **compartilhado** do
  mesmo adversário, se ainda for o último evento dele).
- **Rationale**: FR-010; seguro mesmo se o portador mudar entre o registro e o Desfazer (o id do portador vem do
  resultado, não do estado atual).
- **Alternatives considered**: desfazer só o principal — deixaria EV fantasma no portador.

## R6 — Onde fica o seletor

- **Decision**: no cartão de resumo da aba EVs, abaixo da grade: `<label>Exp. Share</label><select>` com
  "Ninguém" + os outros Pokémon (nome/espécie atual); some com 1 Pokémon só. Mostrar o portador no seletor já
  atende FR-013 ("Exp. Share: Abra").
- **Rationale**: o cabeçalho (PKRS/MACHO) já está cheio a 320 px; o espaço abaixo da grade ficou livre com a
  feature 010; `<select>` é nativo, acessível e troca em 1–2 toques.
- **Alternatives considered**: botão no cabeçalho — não cabe; seletor em Ajustes — longe da contagem, e o portador
  é global, não do Pokémon selecionado.

## R7 — Aviso

- **Decision**: texto do principal como hoje + `Toast.ExpShareGain` "· {0} +{1}" (pt/en) ou
  `Toast.ExpShareNoGain` "· {0}: sem EV (limite)" quando o portador não ganhou.
- **Rationale**: FR-009 numa linha, mesmo componente de aviso.
