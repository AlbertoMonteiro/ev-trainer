# Data Model: Avaliar IVs na captura

## Entidades de domínio (C# puro, `Domain/Capture.cs`)

### CaptureInput (o que o jogador digita)

| Campo | Tipo | Regras |
|---|---|---|
| `Species` | `string` | nome de uma espécie do Pokédex (#1–386); obrigatório |
| `Level` | `int` | 1–100 |
| `Nature` | `string` | uma das 25 natures de `ReferenceData` |
| `Stats` | `int[6]` | cada um 1–999 |

Válido quando todos os campos respeitam as regras. Os EVs são sempre zero (assumption da spec).

### CaptureResult (derivado, nunca gravado)

| Campo | Tipo | Origem |
|---|---|---|
| `Cells` | `IvCell[6]` | `Ivs.RangeFor(species, nature, level, stats, evs: 0)` (R2) |
| `Grades` | `GradeSpan?[6]` | por stat com `IvSpan`; `null` para `IvInconsistent`/`IvUnknowable` |
| `Verdict` | `Verdict` | média ponderada pela importância (R4, R10) |
| `NatureEffect` | `NatureEffect` | Favorece / Neutra / Prejudica (R4) |
| `Score` | `int?` | `Σ peso × (Min + Max)`; `null` se indisponível (R6, R10) |

### Tipos auxiliares

- `enum IvGrade { Poor, Fair, Good, Great }` — 0–10, 11–19, 20–25, 26–31.
- `record GradeSpan(IvGrade Low, IvGrade High)`.
- `closed record Verdict` com os casos `VerdictUnavailable(VerdictProblem Reason)` (nenhum stat conta /
  stat que conta com "?"), `VerdictKnown(VerdictGrade Grade)` e
  `VerdictUncertain(VerdictGrade Low, VerdictGrade High)`;
  `enum VerdictGrade { Weak, Fair, Good, Excellent }`.
- `enum NatureEffect { Harms, Neutral, Helps }` (ordem = ordem de preferência no desempate).
- `enum CaptureComparison { Better, Equivalent, Worse }` (não `Comparison`, que colide com `System.Comparison<T>`).
- `IvUnknowable : IvCell` — novo caso (R3): HP de espécie com base de HP 1.

### StatImportance (emenda R10)

`enum StatImportance { Ignored = 0, Counts = 1, Main = 3 }` — o valor é o peso. Seis por espécie. Padrão
`Capture.DefaultImportance(int[] base)`: o maior entre `base[1]` (Ataque) e `base[3]` (SpAtk) é `Main`,
o menor é `Ignored`, os demais `Counts`; empate ⇒ ambos `Counts`. Stats com `IvUnknowable` são tratados
como `Ignored`, qualquer que seja a escolha.

## Persistência (`localStorage`, fora do backup)

### CaptureNotes — chave `evtrainer:v1:capture`

```json
{
  "importance": { "Abra": ["counts", "ignored", "counts", "main", "counts", "counts"] },
  "best": {
    "Abra": { "lv": 8, "nature": "Timid", "st": [21, 9, 10, 20, 15, 21] }
  }
}
```

| Campo | Regras de leitura (tolerante) |
|---|---|
| `importance[species]` | exatamente 6 valores entre `main`, `counts`, `ignored`, senão ignorado (volta ao padrão) |
| `counted[species]` (legado) | 6 booleanos, lidos só sem `importance` para a espécie: `false` ⇒ `ignored`; `true` ⇒ padrão da espécie, ou `counts` se o padrão for `ignored`; nunca mais gravado |
| `best[species]` | `lv` 1–100, `nature` conhecida, `st` com 6 valores 1–999, espécie existente no Pokédex; senão a entrada é descartada |
| documento ausente ou inválido | `CaptureNotes` vazio |

Chaves de espécie usam o nome canônico do Pokédex.

### Mudanças em entidades existentes

- `IPokemonRepository`: `CaptureNotes LoadCaptureNotes()` e `void SaveCaptureNotes(CaptureNotes)`.
- `TrainingPokemon`: nenhuma mudança de formato; nasce de uma avaliação com `IvLog` de uma anotação.
- Backup: **sem mudanças**.

## Transições de estado

```text
Vazio ──(espécie+nível+nature+6 stats válidos)──▶ Resultado ao vivo
Resultado ao vivo ──(Nova avaliação | Adicionar | Fechar)──▶ Concluída
Concluída ──(válida, sem "?", melhor ou primeira)──▶ vira "melhor até agora" (Desfazer)
Melhor até agora ──(Esquecer)──▶ nenhum (Desfazer)
Concluída via Adicionar ──▶ novo TrainingPokemon selecionado (Desfazer)
```
