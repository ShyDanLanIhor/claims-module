namespace ClaimsModule.Domain.Enums;

/// <summary>Role a party plays on a claim (FRS §7.5, BR-P-02).</summary>
public enum PartyRole
{
    Claimant,
    Insured,
    ThirdParty,
    Witness,
    Attorney
}

/// <summary>Whether a party is an individual or an organisation.</summary>
public enum PartyType
{
    Person,
    Company
}

/// <summary>Type of asset affected by the loss (FRS §5.2, Step 2).</summary>
public enum AssetType
{
    Vehicle,
    Property,
    Person,
    Equipment,
    Other
}
