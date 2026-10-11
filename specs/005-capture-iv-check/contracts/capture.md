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
public enum CaptureComparison { Better, Equivalent, Worse }

public sealed record CaptureInput(string Species, int Level, string Nature, int[] Stats);

public sealed record CaptureResult(IvCell[] Cells, GradeSpan?[] Grades, Verdict Verdict, NatureEffect NatureEffect, int? Score);

public static class Capture
{
    public static IvGrade Grade(int iv);
    public static bool[] DefaultCounted(int[] baseStats);
    public static CaptureResult Evaluate(CaptureInput input, bool[] counted, Pokedex dex, ReferenceData data);
    public static CaptureComparison Compare(CaptureResult current, CaptureResult best);   // ambos com Score != null
}
```

Exemplos obrigatórios (testes):

| Entrada | Esperado |
|---|---|
| Squirtle Nv 5 Timid `20/9/12/11/12/11` | faixas `12–31, 4–31, 10–29, 20–31, 12–31, 14–31` (iguais à tabela de IVs); contam HP, DEF, SPA, SPD, SPE (ATK 48 < SPA 50); veredito Incerto |
| `Grade(10)`, `Grade(11)`, `Grade(19)`, `Grade(20)`, `Grade(25)`, `Grade(26)` | Poor, Fair, Fair, Good, Good, Great |
| `DefaultCounted` Abra `[25,20,15,105,55,90]` | `[T,F,T,T,T,T]` |
| `DefaultCounted` Machop `[70,80,50,35,35,35]` | `[T,T,T,F,T,T]` |
| `DefaultCounted` com Ataque = SpAtk | todos `true` |
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
    public Dictionary<string, bool[]> Counted { get; }
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
    public bool[] CountedFor(string species);                  // marcação gravada ou padrão
    public void SetCounted(string species, bool[] counted);
    public void ResetCounted(string species);
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
| | tabela por stat: faixa, classificação, botão "conta" (`aria-pressed`) | stats que não contam esmaecidos; "?" com explicação; "não dá para saber" no HP de Shedinja |
| | veredito, efeito da nature, dica de nível baixo quando Incerto | |
| | cartão **Melhor até agora**: resumo, Melhor/Pior/Equivalente, **Esquecer**, **Adicionar à minha lista** | "será guardado ao concluir" enquanto não concluída |
| | **Restaurar padrão** (stats que contam) | |
| | **Nova avaliação** (conclui, limpa só os stats), **Adicionar à minha lista** (conclui, cria e abre a aba IVs) | toasts com Desfazer |
