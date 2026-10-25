# Data Model: Equipe atual e plano de batalhas com Exp. Share

## TrainingPokemon (alterado)

| Campo | Tipo | Regra |
|---|---|---|
| `Party` | `bool?` | `true` na equipe, `false` guardado, `null` dado antigo (sem a informação). Depois de `Party.Normalize`, nunca é `null`. |

- O campo entra no `Clone()`.
- JSON: `"party": true | false`. Ele é omitido quando o valor é `null`.
- Documento sem o campo vira `null`, e um valor inválido também.
- A ordem da equipe é a `Order` que já existe.

## Party (domínio, novo, estático)

| Membro | Descrição |
|---|---|
| `MaxSize = 6` | |
| `Normalize(IDictionary<string, TrainingPokemon> all) → string[]` | Aplica R1: no máximo 6 `true`, os `null` preenchem as vagas pela recência e o resto vira `false`. Tira o `ExpShare` de quem ficou guardado. Devolve os ids alterados e não carimba. |
| `Members(all) → IReadOnlyList<string>` | Ids com `Party == true`, por `Order` e depois id. |
| `Recency` | Ordem usada no desempate: `UpdatedAt` desc (null por último), `Order` asc, id ordinal. |

## TrainerStore (alterado)

| Membro | Descrição |
|---|---|
| `PartyOrdered` | `(Id, Pokemon)` da equipe, na ordem. |
| `Stored` | `(Id, Pokemon)` guardados, por `Order`. |
| `IsInParty(id)` | |
| `JoinParty(id, replacing?)` | Põe `id` na equipe. Com a equipe cheia, exige `replacing`, que vai para os guardados. Devolve `false` se não couber. Carimba os alterados. |
| `LeaveParty(id)` | Vai para os guardados. Se segurava o Exp. Share, perde a marcação. Carimba. |
| `MoveInParty(id, delta)` | Troca a `Order` com o vizinho de equipe (−1/+1). Carimba os dois. |
| `AddPokemon` / `AddFromCapture` | O novo recebe `Party = vagas > 0`. |
| `DeletePokemon` → Desfazer | Ao restaurar, volta para a equipe se houver vaga; senão, para os guardados. |
| `SetExpShare(id)` | Ignora `id` fora da equipe (a UI não oferece). |
| `RecordTeam(team) → IReadOnlyList<SharedRecord>` | `RecordBattle` por espécie, na ordem (R4). |
| `UndoTeam(records)` | `UndoBattle` em ordem inversa. |
| `ApplyConfiguration(leadId, partnerId?, partnerShares) → Action?` | R5: o parceiro perde o Macho Brace e fica com o Exp. Share se `partnerShares` (e estiver na equipe). Devolve o Desfazer, ou null se `leadId` não existir. |
| `Load` / `ImportBackup` / `ApplySync` | Chamam `Party.Normalize` depois de `KeepOneExpShareHolder`. |

## ExpSharePlanner (domínio, novo)

### Entradas

```text
PlanMon(int[] Totals, int[] Targets, bool Pokerus)
ExpSharePlanner.Plan(PlanMon a, PlanMon? b, int[] yieldPerBattle) → SharePlan
```

### Tipos

| Tipo | Campos |
|---|---|
| `Setup` (enum) | `ALeadsBShares`, `BLeadsAShares`, `AAlone`, `BAlone` |
| `PlanPhase` | `Setup`, `Battles`, `GainA int[]` e `GainB int[]` (por batalha) |
| `SharePlan` | `Phases` (só as com `Battles > 0`, na ordem do enum) e `FinalA` e `FinalB` (EVs esperados). Também `LeftA` e `LeftB` (o que ainda falta nos atributos do adversário), `Battles` (total) e `AloneBattles` (configurações 3 e 4 sozinhas). Por fim, `Useless`: nenhum dos dois ganha nada com esse adversário. |

### Regras

- A capacidade por atributo (só onde `yield > 0`) é `max(0, min(meta, 255) − atual)`. A capacidade total é
  `max(0, 510 − soma atual)`.
- O multiplicador vem de `Evs.Multiplier(pokerus, machoBrace)`.
- Com `b` nulo, o plano usa só a configuração `AAlone`.
- Os critérios de escolha estão em research R2.

## Adversário do plano (UI)

| Variante | Rendimento por batalha |
|---|---|
| Treinador + revanche (`GuideTrainer`, `GuideStage`) | Soma de `ReferenceData.Lookup(espécie).EvYield` do time. Só treinadores com `Double = false`. |
| Selvagem (`Opponent`, não vitamina) | `EvYield` |

## Busca (alterado)

| Membro | Descrição |
|---|---|
| `OpponentSearch(ReferenceData, LocationNames, VsSeekerGuide)` | Novo parâmetro. |
| `FindTrainers(query) → IReadOnlyList<GuideTrainer>` | R3. |
| `Find(query, version)` | Inclui as espécies dos times de `FindTrainers(query)`, com o local do treinador. |

## Estados e transições da equipe

```text
null (antigo) ──Normalize──▶ true | false
false ──JoinParty (vaga ou troca)──▶ true
true ──LeaveParty / troca──▶ false   (perde o Exp. Share)
excluído ──Desfazer──▶ true se há vaga, senão false
```
