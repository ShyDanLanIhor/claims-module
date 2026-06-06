namespace ClaimsModule.Domain.Documents;

/// <summary>
/// Canonical set of document categories offered when uploading a claim document. This is the single
/// source of truth: the upload validator and the <c>GET /api/reference/document-types</c> endpoint both
/// read it, and the Angular picker fetches that endpoint — so the list is defined in exactly one place
/// (no FE/BE duplication). <see cref="ClaimDocument.DocumentType"/> remains a free-form string column;
/// this set only governs which values an upload accepts.
/// </summary>
public static class DocumentTypes
{
    /// <summary>Fallback category used when an upload does not specify a type.</summary>
    public const string Default = "Other";

    /// <summary>All accepted document categories, in display order.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        "ClaimForm",
        "PoliceReport",
        "MedicalReport",
        "RepairEstimate",
        "Invoice",
        "Photo",
        "Correspondence",
        Default,
    ];

    private static readonly HashSet<string> Lookup = new(All, StringComparer.OrdinalIgnoreCase);

    /// <summary>True if <paramref name="documentType"/> is an accepted category (case-insensitive).</summary>
    public static bool IsAccepted(string documentType) => Lookup.Contains(documentType);
}
