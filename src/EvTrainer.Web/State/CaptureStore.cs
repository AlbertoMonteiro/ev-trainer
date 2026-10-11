using EvTrainer.Web.Domain;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.State;

/// <summary>
/// Capture conveniences per species — how much each stat weighs and the best capture so far — saved right away
/// through <see cref="IPokemonRepository"/>, outside the backup (research R5–R7). Holds no UI text.
/// </summary>
public sealed class CaptureStore
{
    private readonly IPokemonRepository _repository;
    private readonly Pokedex _dex;
    private readonly ReferenceData _data;
    private CaptureNotes _notes;

    public CaptureStore(IPokemonRepository repository, Pokedex dex, ReferenceData data)
    {
        _repository = repository;
        _dex = dex;
        _data = data;
        _notes = repository.LoadCaptureNotes();
    }

    public event Action? Changed;

    /// <summary>Reloads the notes from the repository.</summary>
    public void Load()
    {
        _notes = _repository.LoadCaptureNotes();
        Changed?.Invoke();
    }

    /// <summary>The player's choice for the species, or the default rule (a copy).</summary>
    public StatImportance[] ImportanceFor(string species) =>
        _notes.Importance.TryGetValue(species, out var importance) ? [.. importance]
        : _dex.Find(species) is { } known ? Capture.DefaultImportance(known.Base)
        : [.. Enumerable.Repeat(StatImportance.Counts, Domain.Model.StatVector.Count)];

    public void SetImportance(string species, StatImportance[] importance)
    {
        _notes.Importance[species] = [.. importance];
        Commit();
    }

    public void ResetImportance(string species)
    {
        if (_notes.Importance.Remove(species))
        {
            Commit();
        }
    }

    /// <summary>Best capture so far of the species (a copy), or null.</summary>
    public CaptureInput? BestFor(string species) =>
        _notes.Best.TryGetValue(species, out var best) ? best with { Stats = [.. best.Stats] } : null;

    /// <summary>
    /// How a valid capture compares with the species' best so far, using the current importance for both;
    /// null without a best or when either side has no score.
    /// </summary>
    public CaptureComparison? CompareWithBest(CaptureInput input)
    {
        if (!input.IsValid(_dex, _data) || BestFor(input.Species) is not { } best)
        {
            return null;
        }

        var current = Evaluate(input);
        var kept = Evaluate(best);
        return current.Score is null || kept.Score is null ? null : Capture.Compare(current, kept);
    }

    /// <summary>
    /// Concludes an evaluation: a valid capture with a score becomes the best so far when there is none, when
    /// the kept one no longer has a score, or when it is better. Returns the undo, or null when nothing changed.
    /// </summary>
    public Action? Conclude(CaptureInput input)
    {
        if (!input.IsValid(_dex, _data) || Evaluate(input).Score is null)
        {
            return null;
        }

        var species = _dex.Find(input.Species)!.Name;
        var previous = BestFor(species);
        if (previous is not null && Evaluate(previous).Score is not null && CompareWithBest(input) != CaptureComparison.Better)
        {
            return null;
        }

        _notes.Best[species] = input with { Species = species, Stats = [.. input.Stats] };
        Commit();
        return () => Restore(species, previous);
    }

    /// <summary>Forgets the species' best so far; the undo puts it back.</summary>
    public Action? Forget(string species)
    {
        if (!_notes.Best.Remove(species, out var removed))
        {
            return null;
        }

        Commit();
        return () => Restore(species, removed);
    }

    private void Restore(string species, CaptureInput? best)
    {
        if (best is null)
        {
            _notes.Best.Remove(species);
        }
        else
        {
            _notes.Best[species] = best;
        }

        Commit();
    }

    /// <summary>Evaluates a capture with the species' current importance.</summary>
    public CaptureResult Evaluate(CaptureInput input) => Capture.Evaluate(input, ImportanceFor(input.Species), _dex, _data);

    private void Commit()
    {
        _repository.SaveCaptureNotes(_notes);
        Changed?.Invoke();
    }
}