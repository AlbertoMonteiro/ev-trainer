# Contract: Avaliar captura (domínio, store, armazenamento e UI)

## Domínio (`EvTrainer.Web.Domain`)

```csharp
public sealed record IvUnknowable : IvCell;                      // HP de Shedinja

public static partial class Ivs
{
    public static IvCell[] RangeFor(SpeciesEntry? species, Nature? nature, int level, int[] stats, int[] evs);
}

public enum IvGrade { Poor, Fair, Good, Great }
public sealed record GradeSpan(IvGrade Low, IvGrade High);
public enum VerdictGrade { Weak, Fair, Good, Excellent }
public enum VerdictProblem { NothingCounted, Inconsistent }
public closed record Verdict;
public sealed record VerdictUnavailable(VerdictProblem Reason) : Verdict;
public sealed record VerdictKnown(VerdictGrade Grade) : Verdict;
public sealed record VerdictUncertain(VerdictGrade Low, VerdictGrade High) : Verdict;
public enum NatureEffect { Harms, Neutral, Helps }
public enum StatImportance { Ignored = 0, Counts = 1, Main = 3 }   // valor = peso (emenda R10)
public enum CaptureComparison { Better, Equivalent, Worse }

public sealed record CaptureInput(string Species, int Level, string Nature, int[] Stats);

public sealed record CaptureResult(IvCell[] Cells, GradeSpan?[] Grades, Verdict Verdict, NatureEffect NatureEffect, int? Score);

public static class Capture
{
    public static IvGrade Grade(int iv);
    public static StatImportance[] DefaultImportance(int[] baseStats);
    public static StatImportance Next(StatImportance importance);   // Ignored → Counts → Main → Ignored
    public static NatureEffect EffectOf(Nature? nature, StatImportance[] importance);
    public static CaptureResult Evaluate(CaptureInput input, StatImportance[] importance, Pokedex dex, ReferenceData data);
    public static CaptureComparison Compare(CaptureResult current, CaptureResult best);   // ambos com Score != null
}
```

Exemplos obrigatórios (testes):

| Entrada | Esperado |
|---|---|
| Squirtle Nv 5 Timid `20/9/12/11/12/11` | faixas `12–31, 4–31, 10–29, 20–31, 12–31, 14–31` (iguais à tabela de IVs); contam HP, DEF, SPA, SPD, SPE (ATK 48 < SPA 50); veredito Incerto |
| `Grade(10)`, `Grade(11)`, `Grade(19)`, `Grade(20)`, `Grade(25)`, `Grade(26)` | Poor, Fair, Fair, Good, Good, Great |
| `DefaultImportance` Abra `[25,20,15,105,55,90]` | `[Counts, Ignored, Counts, Main, Counts, Counts]` |
| `DefaultImportance` Machop `[70,80,50,35,35,35]` | `[Counts, Main, Counts, Ignored, Counts, Counts]` |
| `DefaultImportance` com Ataque = SpAtk | todos `Counts` |
| Machop Docile Nv 16 `50/31/25/21/19/20` × Hasty Nv 17 `52/37/21/19/16/22` | scores 209 × 272 ⇒ Hasty `Better` (emenda) |
| Shedinja HP 1 | `Cells[0]` é `IvUnknowable`; não conta |
| Timid (+SPE −ATK) com ATK não contando | `Helps` |
| Modest (+SPA −ATK) num atacante físico | `Harms` |
| Hardy | `Neutral` |
| scores 100 e 104, mesma nature | `Equivalent`; 100 e 105 ⇒ `Worse`/`Better` |
| scores empatados, Helps × Neutral | `Better` para quem tem Helps |

## Armazenamento

```csharp
public sealed class CaptureNotes
{
    public Dictionary<string, StatImportance[]> Importance { get; }
    public Dictionary<string, CaptureInput> Best { get; }
}

public interface IPokemonRepository
{
    // … existentes …
    CaptureNotes LoadCaptureNotes();            // chave evtrainer:v1:capture; ausente/inválido ⇒ vazio
    void SaveCaptureNotes(CaptureNotes notes);
}
```

Formato JSON em [data-model.md](../data-model.md#capturenotes--chave-evtrainerv1capture).

## Store (`EvTrainer.Web.State`)

```csharp
public sealed class CaptureStore(IPokemonRepository repository, Pokedex dex, ReferenceData data)
{
    public event Action? Changed;
    public StatImportance[] ImportanceFor(string species);     // escolha gravada ou padrão
    public void SetImportance(string species, StatImportance[] importance);
    public void ResetImportance(string species);
    public CaptureInput? BestFor(string species);
    public Action? Conclude(CaptureInput input);               // vira o melhor se válida e melhor/primeira; undo
    public Action? Forget(string species);                     // undo
}

public sealed partial class TrainerStore
{
    public (string Id, Action Undo) AddFromCapture(CaptureInput input, Pokedex dex);
}
```

## UI (pt-BR via chaves `Capture.*`)

| Onde | Elemento | Comportamento |
|---|---|---|
| Menu | **Avaliar captura** (`Menu.Capture`) | abre a tela de avaliação |
| Tela vazia | botão **Avaliar captura** | idem |
| Tela de avaliação | título, **Fechar** (conclui) | volta para EVs (ou tela vazia) |
| | espécie: busca do Pokédex → chip com sprite e **Trocar** | |
| | nível, nature (lista com +/−), 6 stats `inputmode="numeric"` | inválidos com `aria-invalid` |
| | tabela por stat: faixa, classificação, botão de importância (principal / conta / ignora) | ignorados esmaecidos, principal destacado; "?" com explicação; "não dá para saber" no HP de Shedinja |
| | veredito, efeito da nature, dica de nível baixo quando Incerto | |
| | cartão **Melhor até agora**: resumo, Melhor/Pior/Equivalente, **Esquecer**, **Adicionar à minha lista** | "será guardado ao concluir" enquanto não concluída |
| | **Restaurar importância** | |
| | **Nova avaliação** (conclui, limpa só os stats), **Adicionar à minha lista** (conclui, cria e abre a aba IVs) | toasts com Desfazer |
