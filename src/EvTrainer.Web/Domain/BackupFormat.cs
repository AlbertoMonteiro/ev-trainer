using EvTrainer.Web.Domain.Json;
using EvTrainer.Web.Domain.Model;

namespace EvTrainer.Web.Domain;

public sealed record BackupDocuments(IReadOnlyDictionary<string, TrainingPokemon> Items);

public enum BackupErrorKind
{
    InvalidJson,
    WrongCollection,
    MissingDocuments,
}

public sealed record BackupError(BackupErrorKind Kind);

/// <summary>Result of reading a backup: either the documents or why the file was rejected.</summary>
public union BackupReadResult(BackupDocuments, BackupError);

public sealed record BackupMergePreview(int New, int Replaced);

/// <summary>Backup file format (contracts/backup-format.md), identical to user-data-export.json.</summary>
public static class BackupFormat
{
    public static BackupReadResult Read(string json)
    {
        if (!PokemonJson.TryParse(json, out var dto))
        {
            return new BackupError(BackupErrorKind.InvalidJson);
        }

        if (dto.Collection != PokemonJson.Collection)
        {
            return new BackupError(BackupErrorKind.WrongCollection);
        }

        return dto.Documents is { } documents
            ? new BackupDocuments(PokemonJson.FromDocuments(documents))
            : new BackupError(BackupErrorKind.MissingDocuments);
    }

    public static string Write(IReadOnlyDictionary<string, TrainingPokemon> all, bool indented) =>
        PokemonJson.Serialize(all, indented);

    /// <summary>Merge preview: ids not present locally are new, the others replace local ones.</summary>
    public static BackupMergePreview Preview(
        IReadOnlyDictionary<string, TrainingPokemon> current,
        IReadOnlyDictionary<string, TrainingPokemon> incoming)
    {
        var replaced = incoming.Keys.Count(current.ContainsKey);
        return new BackupMergePreview(incoming.Count - replaced, replaced);
    }
}