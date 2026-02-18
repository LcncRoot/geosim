namespace GeoSim.SimCore.Data;

/// <summary>
/// Commodity bundle representing a collection of commodities with quantities.
/// Used for build costs, maintenance costs, etc.
/// </summary>
public struct CommodityBundle
{
    /// <summary>Quantities per commodity.</summary>
    public double[] Quantities { get; }

    /// <summary>Money cost (in cents).</summary>
    public long MoneyCost { get; set; }

    public CommodityBundle()
    {
        Quantities = new double[10];
    }

    public double this[Commodity c]
    {
        get => Quantities[(int)c];
        set => Quantities[(int)c] = value;
    }
}

/// <summary>
/// A facility that extracts resources from a deposit.
/// </summary>
public sealed class ExtractionFacility
{
    /// <summary>Unique facility ID.</summary>
    public int Id { get; init; }

    /// <summary>Hex ID where this facility is located.</summary>
    public int HexId { get; init; }

    /// <summary>Region containing this facility.</summary>
    public int RegionId { get; init; }

    /// <summary>Deposit this facility extracts from.</summary>
    public int DepositId { get; init; }

    /// <summary>Facility type name (e.g., "coal_mine", "rice_farm").</summary>
    public required string FacilityType { get; init; }

    /// <summary>Upgrade level [0-5]. 0 = not built.</summary>
    public int Level { get; set; }

    /// <summary>Condition [0, 1]. Degrades without maintenance.</summary>
    public double Condition { get; set; } = 1.0;

    /// <summary>Workers currently assigned.</summary>
    public int Workers { get; set; }

    /// <summary>Workers required for full operation at current level.</summary>
    public int WorkersRequired { get; set; }

    /// <summary>Whether facility is under construction.</summary>
    public bool UnderConstruction { get; set; }

    /// <summary>Construction progress [0, 1].</summary>
    public double ConstructionProgress { get; set; }

    /// <summary>Ticks remaining for current construction.</summary>
    public int ConstructionTicksRemaining { get; set; }

    /// <summary>Output this tick (computed by production system).</summary>
    public double Output { get; set; }

    // === Cost Configuration (set from scenario data) ===

    /// <summary>Build cost per level.</summary>
    public CommodityBundle BuildCost { get; set; }

    /// <summary>Maintenance cost per tick.</summary>
    public CommodityBundle MaintenanceCost { get; set; }

    /// <summary>Base build time in ticks.</summary>
    public int BaseBuildTime { get; set; }

    /// <summary>Degradation rate per tick without maintenance.</summary>
    public double DegradationRate { get; set; } = 0.01;
}

/// <summary>
/// A manufacturing facility that transforms inputs into outputs.
/// Not tied to a resource deposit — capacity comes from capital investment.
/// </summary>
public sealed class ManufacturingFacility
{
    /// <summary>Unique facility ID.</summary>
    public int Id { get; init; }

    /// <summary>Hex ID where this facility is located.</summary>
    public int HexId { get; init; }

    /// <summary>Region containing this facility.</summary>
    public int RegionId { get; init; }

    /// <summary>Facility type name (e.g., "semiconductor_fab", "shipyard").</summary>
    public required string FacilityType { get; init; }

    /// <summary>What commodity this facility produces.</summary>
    public Commodity OutputCommodity { get; init; }

    /// <summary>Upgrade level [0-5]. 0 = not built.</summary>
    public int Level { get; set; }

    /// <summary>Condition [0, 1]. Degrades without maintenance.</summary>
    public double Condition { get; set; } = 1.0;

    /// <summary>Workers currently assigned.</summary>
    public int Workers { get; set; }

    /// <summary>Workers required for full operation at current level.</summary>
    public int WorkersRequired { get; set; }

    /// <summary>Whether facility is under construction.</summary>
    public bool UnderConstruction { get; set; }

    /// <summary>Construction progress [0, 1].</summary>
    public double ConstructionProgress { get; set; }

    /// <summary>Ticks remaining for current construction.</summary>
    public int ConstructionTicksRemaining { get; set; }

    /// <summary>Output capacity per level (modified by condition, workers, etc.).</summary>
    public double BaseCapacity { get; set; }

    /// <summary>Output this tick (computed by production system).</summary>
    public double Output { get; set; }

    // === Cost Configuration ===

    /// <summary>Build cost per level.</summary>
    public CommodityBundle BuildCost { get; set; }

    /// <summary>Maintenance cost per tick.</summary>
    public CommodityBundle MaintenanceCost { get; set; }

    /// <summary>Base build time in ticks.</summary>
    public int BaseBuildTime { get; set; }

    /// <summary>Degradation rate per tick without maintenance.</summary>
    public double DegradationRate { get; set; } = 0.01;

    /// <summary>Whether this facility requires high security (arms factory, etc.).</summary>
    public bool RequiresSecurity { get; set; }
}
