using EvTrainer.Web.Domain;
using EvTrainer.Web.State;

using Microsoft.Extensions.Localization;

namespace EvTrainer.Web.Components.Evs;

/// <summary>
/// Text of the toast after recording a battle (spec 012) or a whole trainer battle (spec 016): the selected Pokémon's
/// gain and, with the Exp. Share, the holder's gain on the same line.
/// </summary>
public static class BattleToast
{
    public static string Message(IStringLocalizer l, TrainerStore store, string title, IReadOnlyList<SharedRecord> records)
    {
        var main = Sum(records.Select(r => r.Main.Gain));
        var message = main.Any(g => g > 0)
            ? l["Toast.EvGain", title, Gains(l, main)].Value
            : l["Toast.NoEvGain", title].Value;

        if (records.FirstOrDefault(r => r.HolderId is not null)?.HolderId is { } holderId && store.All.TryGetValue(holderId, out var holder))
        {
            var name = string.IsNullOrWhiteSpace(holder.CurrentSpecies) ? l["Pokemon.NoSpecies"].Value : holder.CurrentSpecies;
            var shared = Sum(records.Where(r => r.Holder is not null).Select(r => r.Holder!.Gain));
            message += " · " + (shared.Any(g => g > 0)
                ? l["Toast.ExpShareGain", name, Gains(l, shared)].Value
                : l["Toast.ExpShareNoGain", name].Value);
        }

        return message;
    }

    /// <summary>"+6 SpAtk, +1 Speed" for the stats with a gain.</summary>
    public static string Gains(IStringLocalizer l, IReadOnlyList<int> gain) => string.Join(", ", gain
        .Select((value, i) => (value, i))
        .Where(x => x.value > 0)
        .Select(x => l["Evs.Gain.Part", x.value, l[$"Stat.Long.{x.i}"].Value].Value));

    private static int[] Sum(IEnumerable<int[]> gains) =>
        gains.Aggregate(new int[Domain.Model.StatVector.Count], (sum, gain) => [.. sum.Zip(gain, (a, b) => a + b)]);
}