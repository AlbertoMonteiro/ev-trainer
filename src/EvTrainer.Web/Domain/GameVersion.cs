namespace EvTrainer.Web.Domain;

/// <summary>Game played (spec 014): trainers are the same in both, wild encounters differ.</summary>
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
}