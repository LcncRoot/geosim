namespace GeoSim.SimCore.Data;

/// <summary>
/// Discovery state of a resource deposit.
/// </summary>
public enum DiscoveryState
{
    Unknown = 0,    // Must survey to find
    Surveyed = 1,   // Found, reserves estimated (±30%)
    Proven = 2      // Accurate reserve data
}

/// <summary>
/// A fixed geographic resource deposit that can be extracted.
/// </summary>
public sealed class ResourceDeposit
{
    /// <summary>Unique deposit ID.</summary>
    public int Id { get; init; }

    /// <summary>Hex ID where this deposit is located.</summary>
    public int HexId { get; init; }

    /// <summary>Region containing this deposit.</summary>
    public int RegionId { get; init; }

    /// <summary>What commodity this produces when extracted.</summary>
    public Commodity ResourceType { get; init; }

    /// <summary>Specific resource name (e.g., "anthracite", "tungsten", "rice_paddy").</summary>
    public required string Subtype { get; init; }

    /// <summary>Total extractable reserves at start.</summary>
    public long TotalReserves { get; init; }

    /// <summary>Remaining extractable reserves.</summary>
    public long Remaining { get; set; }

    /// <summary>Base yield: units extractable per facility level per tick.</summary>
    public double BaseYield { get; init; }

    /// <summary>Extraction difficulty [0.5, 2.0]. Higher = more costly.</summary>
    public double Difficulty { get; init; } = 1.0;

    /// <summary>Current discovery state.</summary>
    public DiscoveryState DiscoveryState { get; set; } = DiscoveryState.Unknown;

    /// <summary>Estimated reserves (what the player sees if Surveyed but not Proven).</summary>
    public long EstimatedReserves { get; set; }

    /// <summary>Whether this deposit is exhausted.</summary>
    public bool IsExhausted => Remaining <= 0;
}
