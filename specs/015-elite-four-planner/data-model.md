# Data Model: Planejador da Elite Four

## Dados de referência (`league.json`, embutido)

- **LeagueMove**: `Name`, `Type` (ex.: "Electric"), `Power` (0 status; 1 especial), `Accuracy` (0 = nunca erra),
  `Pp`, `Damage`: `status` | `normal` | `fixed` | `variable`. Categoria derivada: `TypeChart.IsPhysical(Type)`.
- **LeagueSpecies**: `Name` (como o Pokédex do app), `Types` (1 ou 2), `Abilities` (1 ou 2), `Learnset`: lista de
  `(Move, Source)` com `Source` = `level` + nível | `tm` + número (`TM24`, `HM03`) | `tutor`. Um golpe aprendido de
  várias formas aparece uma vez por forma.
- **LeagueTrainer**: `Id` (`lorelei`, `bruno`, `agatha`, `lance`, `champion`), `Name`, `Teams`: por luta
  (`first`, `rematch`) e, no Campeão, por inicial do jogador (`bulbasaur`, `charmander`, `squirtle`).
- **LeagueMon**: `Species`, `Level`, `Item` (ou nulo), `Ability`, `Moves` (4 nomes).

## Regras (domínio)

- **TypeChart**: multiplicador Gen 3 `Multiplier(attackType, defenderTypes)` ∈ {0, ¼, ½, 1, 2, 4}.
- **Effectiveness** (`LeaguePlanner.Effectiveness(move, defenderTypes, defenderAbility)`): tipo + habilidade (R5);
  `Damage = fixed` → 1 ou 0 (imunidade). Status → não se aplica.
- **Coverage** por adversário: melhor `(membro, golpe, multiplicador)` da equipe entre golpes de dano; classe
  `SuperEffective` (≥ 2), `Neutral` (1), `Resisted` (0 < x < 1), `None` (sem golpe com efeito > 0, ou equipe vazia).
- **Summary**: contagens de `SuperEffective`, `Neutral` e "sem resposta" (`Resisted` + `None`) na luta escolhida.
- **Threat**: adversário × membro: golpes de dano do adversário com multiplicador ≥ 2 contra o membro (tipos +
  habilidade escolhida do membro). **Walls**: adversários cujos golpes de dano todos têm multiplicador < 1 contra o
  membro (e que têm ao menos um golpe de dano).
- **Suggest** (R6): retorna o plano com as vagas livres preenchidas.

## Equipe planejada (`LeaguePlan`, estado do usuário)

| Campo | Tipo | Regra |
|---|---|---|
| `Round` | `first` \| `rematch` | padrão `first`; inválido → `first` |
| `Starter` | `bulbasaur` \| `charmander` \| `squirtle` | padrão `bulbasaur`; inválido → `bulbasaur` |
| `Members` | lista (≤ 6) de `PlanMember` | entradas com espécie desconhecida descartadas na leitura |
| `UpdatedAt` | data | carimbada a cada mudança local (sync) |

**PlanMember**: `Species`, `Ability` (uma das habilidades da espécie; inválida → a primeira), `Moves` (≤ 4 nomes,
sem repetição, só aprendíveis; os demais descartados na leitura), `Locked` (subconjunto de `Moves`).

Trocar a espécie de um membro remove os golpes que a nova espécie não aprende e reajusta a habilidade.
