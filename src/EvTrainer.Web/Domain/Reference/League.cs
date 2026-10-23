using System.Text.Json;

namespace EvTrainer.Web.Domain.Reference;

/// <summary>Which Pokémon League battle (spec 015): the first one or the rematch after the Sevii Islands.</summary>
public enum LeagueRound
{
    First,
    Rematch,
}

/// <summary>The player's starter; the Champion (the rival) takes the one that beats it.</summary>
public enum Starter
{
    Bulbasaur,
    Charmander,
    Squirtle,
}

/// <summary>
/// How a move deals damage: <see cref="Fixed"/> ignores weaknesses and resistances (Dragon Rage, Seismic Toss…),
/// <see cref="Variable"/> has no fixed power (Return, Low Kick…).
/// </summary>
public enum MoveDamage
{
    Status,
    Normal,
    Fixed,
    Variable,
}

public enum LearnSource
{
    Level,
    Machine,
    Tutor,
}

/// <param name="Accuracy">0 = never misses.</param>
public sealed record LeagueMove(string Name, string Type, int Power, int Accuracy, int Pp, MoveDamage Damage)
{
    public bool DealsDamage => Damage != MoveDamage.Status;
}

/// <param name="Machine">"TM24" or "HM03" when <paramref name="Source"/> is <see cref="LearnSource.Machine"/>.</param>
public sealed record LearnEntry(string Move, LearnSource Source, int? Level = null, string? Machine = null);

public sealed record LeagueSpecies(string Name, IReadOnlyList<string> Types, IReadOnlyList<string> Abilities, IReadOnlyList<LearnEntry> Learnset)
{
    public bool CanLearn(string move) => Learnset.Any(l => l.Move == move);
}

/// <param name="Ability">Always the species' first ability (trainer Pokémon, research R2).</param>
public sealed record LeagueMon(string Species, int Level, string? Item, string Ability, IReadOnlyList<string> Moves);

/// <param name="Teams">Key "first"/"rematch"; for the Champion "first.bulbasaur" etc. (player's starter).</param>
public sealed record LeagueTrainer(string Id, string Name, IReadOnlyDictionary<string, IReadOnlyList<LeagueMon>> Teams);

/// <summary>A Pokémon of the round, with its trainer and position in the trainer's team.</summary>
public sealed record LeagueOpponent(LeagueTrainer Trainer, int Index, LeagueMon Mon)
{
    public string Key => $"{Trainer.Id}.{Index}";
}

/// <summary>The embedded <c>league.json</c> (spec 015), generated from the FireRed decompilation by scripts/BuildLeague.cs.</summary>
public sealed class League
{
    private const string ResourceName = "EvTrainer.league.json";

    private readonly Dictionary<string, LeagueMove> _moves;
    private readonly Dictionary<string, LeagueSpecies> _species;

    private League(IReadOnlyList<LeagueTrainer> trainers, Dictionary<string, LeagueMove> moves, Dictionary<string, LeagueSpecies> species)
    {
        Trainers = trainers;
        _moves = moves;
        _species = species;
    }

    public IReadOnlyList<LeagueTrainer> Trainers { get; }

    public static League LoadEmbedded()
    {
        using var stream = typeof(League).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var moves = new Dictionary<string, LeagueMove>(StringComparer.Ordinal);
        foreach (var move in root.GetProperty("moves").EnumerateObject())
        {
            var value = move.Value;
            moves[move.Name] = new LeagueMove(
                move.Name,
                value.GetProperty("type").GetString()!,
                value.GetProperty("power").GetInt32(),
                value.GetProperty("accuracy").GetInt32(),
                value.GetProperty("pp").GetInt32(),
                value.GetProperty("damage").GetString() switch
                {
                    "status" => MoveDamage.Status,
                    "fixed" => MoveDamage.Fixed,
                    "variable" => MoveDamage.Variable,
                    _ => MoveDamage.Normal,
                });
        }

        var machines = root.GetProperty("tms").EnumerateObject().ToDictionary(t => t.Name, t => t.Value.GetString()!);
        var species = new Dictionary<string, LeagueSpecies>(StringComparer.Ordinal);
        foreach (var entry in root.GetProperty("species").EnumerateObject())
        {
            var value = entry.Value;
            List<LearnEntry> learnset =
            [
                .. value.GetProperty("level").EnumerateArray().Select(l => new LearnEntry(l[1].GetString()!, LearnSource.Level, Level: l[0].GetInt32())),
                .. value.GetProperty("tm").EnumerateArray().Select(t => new LearnEntry(machines[t.GetString()!], LearnSource.Machine, Machine: t.GetString())),
                .. value.GetProperty("tutor").EnumerateArray().Select(t => new LearnEntry(t.GetString()!, LearnSource.Tutor)),
            ];
            species[entry.Name] = new LeagueSpecies(
                entry.Name,
                [.. value.GetProperty("types").EnumerateArray().Select(t => t.GetString()!)],
                [.. value.GetProperty("abilities").EnumerateArray().Select(a => a.GetString()!)],
                learnset);
        }

        List<LeagueTrainer> trainers =
        [
            .. root.GetProperty("trainers").EnumerateArray().Select(t => new LeagueTrainer(
                t.GetProperty("id").GetString()!,
                t.GetProperty("name").GetString()!,
                t.GetProperty("teams").EnumerateObject().ToDictionary(
                    team => team.Name,
                    team => (IReadOnlyList<LeagueMon>)[.. team.Value.EnumerateArray().Select(m => new LeagueMon(
                        m.GetProperty("species").GetString()!,
                        m.GetProperty("level").GetInt32(),
                        m.GetProperty("item").GetString(),
                        m.GetProperty("ability").GetString()!,
                        [.. m.GetProperty("moves").EnumerateArray().Select(v => v.GetString()!)]))]))),
        ];

        return new League(trainers, moves, species);
    }

    public LeagueMove? Move(string name) => _moves.GetValueOrDefault(name);

    public LeagueSpecies? Species(string name) => _species.GetValueOrDefault(name);

    public IReadOnlyList<LeagueMon> Team(string trainerId, LeagueRound round, Starter starter)
    {
        var trainer = Trainers.First(t => t.Id == trainerId);
        return trainer.Teams.TryGetValue(RoundKey(round), out var team) ? team : trainer.Teams[$"{RoundKey(round)}.{StarterKey(starter)}"];
    }

    /// <summary>Every Pokémon of the round, trainer by trainer, in the order they are sent out.</summary>
    public IReadOnlyList<LeagueOpponent> Opponents(LeagueRound round, Starter starter) =>
    [
        .. Trainers.SelectMany(t => Team(t.Id, round, starter).Select((mon, i) => new LeagueOpponent(t, i, mon))),
    ];

    public static string RoundKey(LeagueRound round) => round == LeagueRound.Rematch ? "rematch" : "first";

    public static string StarterKey(Starter starter) => starter.ToString().ToLowerInvariant();
}