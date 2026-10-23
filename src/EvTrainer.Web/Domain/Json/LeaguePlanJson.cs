using System.Text.Json;
using System.Text.Json.Serialization;

using EvTrainer.Web.Domain.Model;
using EvTrainer.Web.Domain.Reference;

namespace EvTrainer.Web.Domain.Json;

/// <summary>
/// Reads and writes the planned team (contracts/plan-sync.md), stored on the device (<c>evtrainer:v1:plan</c>) and in
/// the synced document. Reading is tolerant and structural only; <see cref="LeaguePlans.Normalize"/> checks the game data.
/// </summary>
public static class LeaguePlanJson
{
    public static string Serialize(LeaguePlan plan) => JsonSerializer.Serialize(ToDto(plan), EvTrainerJsonContext.Default.LeaguePlanDto);

    /// <summary>The plan, or null when the JSON is missing, invalid or not an object.</summary>
    public static LeaguePlan? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, EvTrainerJsonContext.Default.LeaguePlanDto) is { } dto ? FromDto(dto) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static LeaguePlanDto ToDto(LeaguePlan plan) => new()
    {
        Round = League.RoundKey(plan.Round),
        Starter = League.StarterKey(plan.Starter),
        Members = [.. plan.Members.Select(m => (PlanMemberDto?)new PlanMemberDto
        {
            Species = m.Species,
            Ability = m.Ability,
            Moves = [.. m.Moves],
            Locked = [.. m.Locked],
        })],
        UpdatedAt = plan.UpdatedAt is { } at ? PokemonJson.ToElement(at) : null,
    };

    internal static LeaguePlan FromDto(LeaguePlanDto dto)
    {
        List<PlanMember> members = [];
        foreach (var m in dto.Members ?? [])
        {
            if (m?.Species is not { Length: > 0 } species)
            {
                continue;
            }

            List<string> moves = [.. (m.Moves ?? []).OfType<string>().Where(v => v.Length > 0).Distinct().Take(PlanMember.MaxMoves)];
            members.Add(new PlanMember(species, m.Ability ?? "", moves, [.. (m.Locked ?? []).OfType<string>().Where(moves.Contains).Distinct()]));
        }

        return new LeaguePlan(
            dto.Round == "rematch" ? LeagueRound.Rematch : LeagueRound.First,
            Enum.GetValues<Starter>().FirstOrDefault(s => League.StarterKey(s) == dto.Starter, Starter.Bulbasaur),
            [.. members.Take(LeaguePlan.MaxMembers)],
            PokemonJson.ParseTimestamp(dto.UpdatedAt));
    }
}

internal sealed class LeaguePlanDto
{
    [JsonPropertyName("round")]
    public string? Round { get; set; }

    [JsonPropertyName("starter")]
    public string? Starter { get; set; }

    [JsonPropertyName("members")]
    public List<PlanMemberDto?>? Members { get; set; }

    /// <summary>ISO date, read tolerantly (anything else becomes null).</summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? UpdatedAt { get; set; }
}

internal sealed class PlanMemberDto
{
    [JsonPropertyName("species")]
    public string? Species { get; set; }

    [JsonPropertyName("ability")]
    public string? Ability { get; set; }

    [JsonPropertyName("moves")]
    public List<string?>? Moves { get; set; }

    [JsonPropertyName("locked")]
    public List<string?>? Locked { get; set; }
}