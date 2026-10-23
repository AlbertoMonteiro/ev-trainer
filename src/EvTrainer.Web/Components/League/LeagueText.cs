using EvTrainer.Web.Domain.Reference;

using Microsoft.Extensions.Localization;

namespace EvTrainer.Web.Components.League;

/// <summary>Texts shared by the Elite Four planner components (spec 015).</summary>
public static class LeagueText
{
    /// <summary>"4×", "½×", "0×"… as the game's effectiveness reads.</summary>
    public static string Multiplier(double value) => value switch
    {
        0.5 => "½×",
        0.25 => "¼×",
        0.125 => "⅛×",
        _ => $"{value:0.##}×",
    };

    /// <summary>CSS class of a type badge ("type-water"); Curse's "???" type is "type-mystery".</summary>
    public static string TypeClass(string type) => type == "???" ? "type-mystery" : $"type-{type.ToLowerInvariant()}";

    public static string TrainerName(IStringLocalizer l, LeagueTrainer trainer) =>
        trainer.Id == "champion" ? l["League.Trainer.champion"] : trainer.Name;

    /// <summary>"Thunderbolt · Electric (especial) · 95 de poder · TM24" for the move picker.</summary>
    public static string MoveOption(IStringLocalizer l, LeagueMove move, IEnumerable<LearnEntry> sources)
    {
        var power = move.Damage switch
        {
            MoveDamage.Normal => l["League.Move.Power", move.Power].Value,
            MoveDamage.Fixed => l["League.Move.Fixed"].Value,
            MoveDamage.Variable => l["League.Move.Variable"].Value,
            _ => l["League.Move.NoPower"].Value,
        };
        var from = string.Join(", ", sources.Select(s => s.Source switch
        {
            LearnSource.Level => l["League.Source.Level", s.Level ?? 0].Value,
            LearnSource.Machine => s.Machine ?? "",
            _ => l["League.Source.Tutor"].Value,
        }).Distinct());
        var kind = !move.DealsDamage ? l["League.Move.StatusKind"].Value
            : Domain.TypeChart.IsPhysical(move.Type) ? l["League.Move.Physical"].Value
            : l["League.Move.Special"].Value;
        return l["League.Move.Option", move.Name, move.Type, kind, power, from];
    }
}