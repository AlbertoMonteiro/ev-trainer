using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain.Model;

/// <summary>
/// A member of the planned team (spec 015): species, ability and up to 4 moves; <see cref="Locked"/> are the moves the
/// player fixed, which a suggestion never replaces.
/// </summary>
public sealed record PlanMember(string Species, string Ability, IReadOnlyList<string> Moves, IReadOnlyList<string> Locked)
{
    public const int MaxMoves = 4;
}

/// <summary>
/// The team planned against the Pokémon League (spec 015): which battle, the player's starter (it decides the
/// Champion's team) and up to 6 members. <see cref="UpdatedAt"/> is stamped on every local change, for the sync.
/// </summary>
public sealed record LeaguePlan(LeagueRound Round, Starter Starter, IReadOnlyList<PlanMember> Members, DateTimeOffset? UpdatedAt)
{
    public const int MaxMembers = 6;

    public static readonly LeaguePlan Empty = new(LeagueRound.First, Starter.Bulbasaur, [], null);
}

/// <summary>Plan rules that need the game data: only known species, their abilities and the moves they learn.</summary>
public static class LeaguePlans
{
    /// <summary>Drops unknown species and moves the species cannot learn; an invalid ability becomes the first one.</summary>
    public static LeaguePlan Normalize(LeaguePlan plan, League league) => plan with
    {
        Members = [.. plan.Members.Where(m => league.Species(m.Species) is not null).Take(LeaguePlan.MaxMembers).Select(m => Normalize(m, league))],
    };

    /// <summary>The member with another species: keeps the moves it also learns; the ability is kept only if it has it.</summary>
    public static PlanMember WithSpecies(PlanMember member, string species, League league) =>
        Normalize(member with { Species = species }, league);

    /// <summary>A new member of <paramref name="species"/> with its first ability and no moves.</summary>
    public static PlanMember NewMember(string species, League league) => Normalize(new PlanMember(species, "", [], []), league);

    /// <summary>Adds a member at the end, unless the team is full.</summary>
    public static LeaguePlan Add(LeaguePlan plan, PlanMember member) =>
        plan.Members.Count >= LeaguePlan.MaxMembers ? plan : plan with { Members = [.. plan.Members, member] };

    public static LeaguePlan Replace(LeaguePlan plan, int index, PlanMember member) =>
        plan with { Members = [.. plan.Members.Select((m, i) => i == index ? member : m)] };

    public static LeaguePlan Remove(LeaguePlan plan, int index) =>
        plan with { Members = [.. plan.Members.Where((_, i) => i != index)] };

    /// <summary>
    /// Puts <paramref name="move"/> in <paramref name="slot"/> (after the last move when the slot is empty), or clears the
    /// slot when null. A move already in another slot leaves it; a removed move is unlocked.
    /// </summary>
    public static PlanMember WithMove(PlanMember member, int slot, string? move)
    {
        List<string> moves = [.. member.Moves];
        if (slot < moves.Count)
        {
            moves.RemoveAt(slot);
        }

        if (move is not null)
        {
            moves.Remove(move);
            moves.Insert(Math.Min(slot, moves.Count), move);
        }

        return member with { Moves = moves, Locked = [.. member.Locked.Where(moves.Contains)] };
    }

    /// <summary>Locks or unlocks one of the member's moves.</summary>
    public static PlanMember ToggleLock(PlanMember member, string move) => member with
    {
        Locked = member.Locked.Contains(move) ? [.. member.Locked.Where(m => m != move)]
            : member.Moves.Contains(move) ? [.. member.Locked, move]
            : member.Locked,
    };

    private static PlanMember Normalize(PlanMember member, League league)
    {
        var species = league.Species(member.Species)!;
        List<string> moves = [.. member.Moves.Where(species.CanLearn).Distinct().Take(PlanMember.MaxMoves)];
        return member with
        {
            Ability = species.Abilities.Contains(member.Ability) ? member.Ability : species.Abilities[0],
            Moves = moves,
            Locked = [.. member.Locked.Where(moves.Contains).Distinct()],
        };
    }
}