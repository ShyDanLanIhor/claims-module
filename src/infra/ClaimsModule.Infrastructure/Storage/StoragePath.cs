namespace ClaimsModule.Infrastructure.Storage;

/// <summary>Builds and sanitises blob paths (FRS §13, BR-D-01: strip path-traversal characters).</summary>
internal static class StoragePath
{
    public static string Sanitise(string fileName)
    {
        // Drop any directory components, then remove characters illegal in a file name.
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
            name = "file";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return cleaned.Replace("..", "_");
    }

    /// <summary>claim-documents path: {organisationId}/{claimId}/{sanitised-filename}.</summary>
    public static string Build(Guid organisationId, Guid claimId, string fileName) =>
        $"{organisationId}/{claimId}/{Sanitise(fileName)}";
}
