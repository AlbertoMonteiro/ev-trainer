# Contract: API do domínio (`EvTrainer.Web.Domain`)

Fronteira entre UI e regras do jogo (constituição, Princípio II). C# puro: sem Blazor,
`IJSRuntime`, `HttpClient` ou armazenamento. Toda função abaixo é coberta por testes xUnit
**antes** de existir (Princípio III). As assinaturas são indicativas: nomes podem mudar durante o
TDD desde que o comportamento se mantenha.

## Referência

```csharp
sealed class ReferenceData
{
    static ReferenceData LoadEmbedded();                 // reference-data.json embutido
    IReadOnlyList<Opponent> Opponents { get; }
    IReadOnlyList<Nature> Natures { get; }
    IReadOnlyList<TrainerLocation> TrainersByLocation { get; } // ordem do arquivo preservada
    string Canon(string name);                           // aplica aliases; desconhecido → igual
    Opponent? Lookup(string name);                       // Canon + busca exata
    Nature? FindNature(string name);                     // "" ou desconhecida → null (neutra)
}
record TrainerLocation(string Name, IReadOnlyList<string> Opponents);
```

## Regras de EV

```csharp
static class Evs
{
    int[] Totals(IReadOnlyList<BattleEvent> events, ReferenceData data);   // HANDOFF §4.1
    int Multiplier(bool pokerus, bool macho);                              // 1, 2 ou 4
    int CountFor(IReadOnlyList<BattleEvent> events, string opponent, ReferenceData data); // §4.3
    bool IsUseful(Opponent o, int[] totals, int[] targets);                // §4.4
    IReadOnlyList<Opponent> Recent(IReadOnlyList<BattleEvent> events, ReferenceData data, int max = 6);
}

static class BattleLog
{
    RecordResult Record(TrainingPokemon p, string opponent, ReferenceData data); // §4.2
    void UndoLast(TrainingPokemon p);                                            // decrementa/remove
}
record RecordResult(int[] Gain);   // ganho real por stat; tudo 0 ⇒ UI usa a chave "Toast.NoEvGain"
```

## Regras de IV

```csharp
static class Stats
{
    int Value(int @base, int iv, int ev, int level, int natureMultiplier, bool isHp); // §2.2, inteiro
}

static class Ivs
{
    IvCell[] RangeFor(LevelEntry entry, TrainingPokemon p, ReferenceData data); // §4.7
    FinalCell[] Final(TrainingPokemon p, ReferenceData data);                   // §4.8
}

// C# 15: hierarquias fechadas ⇒ switch exaustivo sem default na UI
public closed record IvCell;
public sealed record IvSpan(int Min, int Max) : IvCell;
public sealed record IvInconsistent : IvCell;            // UI mostra "?"

public closed record FinalCell;
public sealed record FinalSpan(int Low, int High) : FinalCell;
public sealed record FinalSingle(int Value) : FinalCell;
public sealed record FinalConflict : FinalCell;          // UI mostra "conferir"
public sealed record FinalNoData : FinalCell;            // UI mostra "—"
```

## Busca

```csharp
static class TextFold { string Fold(string text); }               // research R7
static class RouteExpander { string Expand(string foldedText); }  // "rota 3, 4, 22", "rotas 12–15"

static class OpponentSearch
{
    IReadOnlyList<SearchHit> Search(string query, ReferenceData data);  // §4.5; ordem da lista
}
record SearchHit(Opponent Opponent, IReadOnlyList<string> TrainerLocations); // vazio se não veio de treinador
```

Casamento por substring com fronteira de dígito: se a consulta (já dobrada) termina em dígito, o
caractere seguinte no texto não pode ser dígito.

## Importação e backup

```csharp
static class SpreadsheetImport
{
    ImportPreview<BattleEvent> Battles(string pasted, ReferenceData data);   // §4.9
    ImportPreview<LevelEntry>  Levels(string pasted, TrainingPokemon p);     // §4.9
}
record ImportPreview<T>(IReadOnlyList<T> Items, int Recognized, int Ignored);

static class BackupFormat
{
    BackupReadResult Read(string json);                                      // backup-format.md
    string Write(IReadOnlyDictionary<string, TrainingPokemon> all, bool indented);
    BackupMergePreview Preview(IReadOnlyDictionary<string, TrainingPokemon> current,
                               IReadOnlyDictionary<string, TrainingPokemon> incoming);
}

// C# 15: union type — o chamador trata os dois casos em switch exaustivo
public record BackupDocuments(IReadOnlyDictionary<string, TrainingPokemon> Items);
public record BackupError(BackupErrorKind Kind);    // InvalidJson | WrongCollection | MissingDocuments
public union BackupReadResult(BackupDocuments, BackupError);

record BackupMergePreview(int New, int Replaced);
```

Nenhuma função do domínio devolve texto de interface: erros e resultados são códigos/dados e a
UI escolhe a chave de idioma (constituição, Princípio VII).

## Casos de aceite obrigatórios (testes)

Todos os casos do HANDOFF §6 (IV nível 5, interseção de 6 níveis, limites de EV, aliases, 4
buscas, filtro Úteis), mais:

- Os totais de EV e as faixas de IV calculados a partir de `user-data-export.json` coincidem com os
  do app atual (SC-003); os valores esperados são extraídos uma vez do `app-atual.html` e fixados
  no teste.
- `BackupFormat.Read(BackupFormat.Write(x))` é igual a `x` (SC-004).
