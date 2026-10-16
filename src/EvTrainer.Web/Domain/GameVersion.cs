namespace EvTrainer.Web.Domain;

/// <summary>Game played (spec 014): trainers are the same in both, wild encounters and Deoxys's forme differ.</summary>
public enum GameVersion
{
    FireRed,
    LeafGreen,
}

/// <summary>Keys of <see cref="GameVersion"/> as saved in the device preferences.</summary>
public static class GameVersions
{
    public const string FireRedKey = "fireRed";
    public const string LeafGreenKey = "leafGreen";

    /// <summary>"leafGreen" is LeafGreen; anything else (missing, unknown) is FireRed.</summary>
    public static GameVersion Parse(string? key) => key == LeafGreenKey ? GameVersion.LeafGreen : GameVersion.FireRed;

    public static string Key(GameVersion version) => version == GameVersion.LeafGreen ? LeafGreenKey : FireRedKey;

    /// <summary>
    /// Base stats the game uses for the species: Deoxys is the Attack forme in FireRed and the Defense forme in
    /// LeafGreen (pret/pokefirered src/pokemon.c, sDeoxysBaseStats); every other species keeps its own.
    /// </summary>
    public static int[] BaseStats(string species, int[] baseStats, GameVersion version) => species switch
    {
        "Deoxys" when version == GameVersion.LeafGreen => [50, 70, 160, 70, 160, 90],
        "Deoxys" => [50, 180, 20, 180, 20, 150],
        _ => baseStats,
    };
}