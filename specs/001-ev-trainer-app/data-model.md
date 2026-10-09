# Data Model: EVs Trainer

**Feature**: [spec.md](./spec.md) · **Research**: [research.md](./research.md)

Convenções:

- Vetores de stats têm sempre **6 posições** na ordem `[HP, Ataque, Defesa, SpAtk, SpDef, Speed]`
  (rótulos curtos `HP ATK DEF SPA SPD SPE`). No C# são `int[]` validados com tamanho 6.
- Nomes JSON entre parênteses são os do formato persistido/backup (HANDOFF §3.1), definido em
  [contracts/backup-format.md](./contracts/backup-format.md).
- Tudo abaixo vive em `EvTrainer.Web/Domain/` (C# puro, sem I/O).

## Entidades persistidas (dados do usuário)

### TrainingPokemon (`documents.<id>`)

| Campo | JSON | Tipo | Regras |
|---|---|---|---|
| Id | chave do dicionário | string | Não vazio. Novos: GUID sem hífens. Importados: preservados (`"squirtle"`). |
| Name | `name` | string | Obrigatório ao salvar ajustes; trim; exibido no menu e nos títulos. |
| Nature | `nature` | string | Nome de uma das 25 natures ou `""` (= neutra no cálculo). |
| Species | `species` | List\<SpeciesEntry\> | 1 a 3 entradas, na ordem evolutiva. |
| Targets | `targets` | int[6] | Cada valor 0–255. Soma > 510 é permitida, com aviso (FR-002). |
| Events | `events` | List\<BattleEvent\> | Ordem é significativa; nunca reordenar. |
| IvLog | `ivLog` | List\<LevelEntry\> | Mantida ordenada por `Level` (estável). |
| Pokerus | `pokerus` | bool | Vale para as próximas batalhas. |
| MachoBrace | `macho` | bool | Vale para as próximas batalhas. |
| Order | `order` | int | Posição no menu; novo Pokémon = max + 1. |

### SpeciesEntry

| Campo | JSON | Tipo | Regras |
|---|---|---|---|
| Name | `name` | string | Obrigatório, único dentro do Pokémon. |
| Base | `base` | int[6] | Cada valor 1–255. |

### BattleEvent

| Campo | JSON | Tipo | Regras |
|---|---|---|---|
| Opponent | `o` | string | Nome gravado como veio (pode ser alias ou desconhecido). |
| Count | `n` | int | ≥ 1 (ausente/0 ⇒ 1 na leitura). |
| Multiplier | `m` | int | 1, 2 ou 4 (ausente ⇒ 1). Ignorado em vitaminas. |

### LevelEntry

| Campo | JSON | Tipo | Regras |
|---|---|---|---|
| Level | `lv` | int | 1–100. |
| Species | `sp` | string | Deveria ser um `Species[].Name`; se não for, a linha mostra "?". |
| Stats | `st` | int[6] | Stats exibidos na tela, cada ≥ 1. |
| Evs | `ev` | int[6] | EVs no momento, 0–255 cada. |

### UiPreferences (chave `evtrainer:v1:prefs`, fora do backup)

| Campo | Tipo | Regras |
|---|---|---|
| SelectedId | string? | Id do Pokémon ativo; inválido ⇒ primeiro por `Order`. |
| Theme | `system` \| `light` \| `dark` | Padrão `system`. |
| Filter | `useful` \| `all` | Padrão `useful`. |
| Language | tag BCP-47 (`pt-BR`) | Padrão `pt-BR`; precisa ser um idioma disponível, senão volta ao padrão (FR-035). |

## Entidades de referência (somente leitura, `reference-data.json` embutido)

### ReferenceData

| Campo | Fonte JSON | Tipo |
|---|---|---|
| Opponents | `opponents[]` | List\<Opponent\> (151 Pokémon + 6 vitaminas) |
| Aliases | `aliases` | Dictionary\<string,string\> (nome antigo → canônico) |
| TrainersByLocation | `trainersByLocation` | List\<TrainerLocation(Name, Opponents)\>, ordem do arquivo (34 locais) |
| Natures | `natures[]` | List\<Nature\> (25) |
| StatNames / StatsShort | `stats`, `statsShort` | string[6] |

`_fonte` é ignorado.

### Opponent

`Dex` (int?, null em vitaminas), `Name`, `EvYield` (int[6]), `WildLocation` (string, pode ser
vazio), `IsVitamin` (bool).

### Nature

`Name`, `Increases`, `Decreases` (nome completo do stat — `"Ataque"`, `"SpAtk"`… — ou null =
neutra). Derivado: `Multiplier(statIndex)` → 11, 9 ou 10 (HP sempre 10).

### PokedexSpecies (`pokedex-gen3.json` embutido)

`Dex` (1–386), `Name` (nomes de Kanto iguais aos do `reference-data.json`), `Base` (int[6], stats
base da **Geração III**), `EvolvesFrom` (dex da pré-evolução ou null; pré-evoluções posteriores à
Gen III são descartadas). Derivados: `Search(query)` (nome dobrado ou número), `LineFor(species)`
(ancestrais + a espécie + evoluções enquanto houver uma só opção, até 3) e `EvolutionsOf(species)`
(opções em ramificações). O usuário nunca digita stats base: `SpeciesEntry` é preenchido a partir
daqui.

## Valores derivados (nunca persistidos)

| Derivado | Fonte | Regra (HANDOFF) |
|---|---|---|
| `EvTotals(p)` → int[6] | `Events` + ReferenceData | §4.1: reprocessa em ordem; 255/stat, 510 total; vitamina +10 só < 100, teto 100, sem multiplicador. Eventos desconhecidos não somam. |
| `Multiplier(p)` | `Pokerus`, `MachoBrace` | §4.2: `(pokerus?2:1)*(macho?2:1)`. |
| `CountFor(p, opponent)` | `Events` | §4.3: soma de `n` por nome canônico. |
| `IsUseful(o, ev, targets)` | totais + metas | §4.4. |
| `Recent(p, 6)` | `Events` | §4.6: últimos 6 nomes canônicos distintos. |
| `History(p, 12)` | `Events` | últimos 12 eventos, mais novo primeiro (FR-012). |
| `IvRange(entry, species, nature)` → `IvCell[6]` | `LevelEntry` | §4.7: IVs 0–31 que reproduzem o stat. `IvCell` é hierarquia fechada (C# 15 `closed`): `IvSpan(min,max)` ou `IvInconsistent` ("?"). |
| `FinalRanges(p)` → `FinalCell[6]` | `IvLog` | §4.8: interseção. `FinalCell` é hierarquia fechada: `FinalSpan(lo,hi)`, `FinalSingle(v)`, `FinalConflict` ("conferir"), `FinalNoData` ("—"). |
| `StatValue(base, iv, ev, level, natMul, isHp)` | — | §2.2 com aritmética inteira. |

## Transições de estado (comandos do domínio)

Todos recebem um `TrainingPokemon` e devolvem o resultado + informação para "Desfazer". O
`TrainerStore` aplica, grava e emite o aviso.

| Comando | Efeito | Desfazer |
|---|---|---|
| `RecordBattle(o)` | Se o último evento tem mesmo nome canônico e mesmo `m`, `n++`; senão anexa `{o,1,m}`. Retorna ganho (diff dos totais). | Decrementa `n` do último evento; remove se 0. |
| `RemoveEvent(index)` | Remove o evento. | Reinsere no mesmo índice. |
| `AddLevel(entry)` | Insere e reordena por nível. | Remove a entrada. |
| `RemoveLevel(index)` | Remove. | Reinsere. |
| `SaveSettings(name, nature, targets, species)` | Substitui os campos. | — (sem desfazer; o usuário edita de novo). |
| `TogglePokerus` / `ToggleMacho` | Inverte o bool; não toca em eventos. | — |
| `AddPokemon()` | Novo Pokémon vazio (1 espécie em branco, metas 0), selecionado, abre Ajustes. | — |
| `DeletePokemon(id)` | Remove do conjunto. | Restaura o objeto e a ordem. |
| `ImportBattles(text)` / `ImportLevels(text)` | §4.9: prévia `{recognized, ignored, items}`; confirmar anexa/insere. | — (prévia + confirmação). |
| `ImportBackup(json)` | Prévia `{novos, substituídos}`; confirmar faz merge por id. | — (prévia + confirmação). |

## Textos e idioma

O domínio não contém texto de interface (constituição, Princípio VII). Resultados como
`RecordResult.Gain`, `IvCell`, `FinalCell`, `ImportPreview` e erros de backup são dados
estruturados (incluindo códigos de erro como `BackupError.InvalidJson`, `WrongCollection`,
`MissingDocuments`); a UI escolhe a chave de texto e formata no idioma ativo. Os nomes de
adversários, natures e locais são dados de referência, não textos de UI.

## Validações e mensagens

- Metas fora de 0–255 ou stats base fora de 1–255: campo inválido, "Salvar ajustes" desabilitado.
- Soma de metas > 510: aviso, salvar permitido.
- Nível sem espécie ou com stat vazio: "Salvar nível" desabilitado.
- Backup inválido (JSON quebrado, `collection` ≠ `"pokemon"`, `documents` ausente): erro, nada muda.
