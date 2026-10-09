using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain;

/// <summary>An opponent shown in a list; <see cref="TrainerLocations"/> is filled only when a search matched by trainer.</summary>
public sealed record SearchHit(Opponent Opponent, IReadOnlyList<string> TrainerLocations)
{
    public SearchHit(Opponent opponent) : this(opponent, [])
    {
    }
}