namespace GeoSim.SimCore.Data;

/// <summary>
/// A geographic subdivision of a country with its own production and population.
/// </summary>
public sealed class Region
{
    /// <summary>Integer ID for array indexing.</summary>
    public int Id { get; init; }

    /// <summary>Parent country ID.</summary>
    public int CountryId { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    // === Production ===

    /// <summary>Sectors in this region. Array indexed by commodity.</summary>
    public Sector[] Sectors { get; } = new Sector[CommodityConstants.Count];

    /// <summary>
    /// Infrastructure factor [0.5, 1.2].
    /// Affects all production in this region.
    /// </summary>
    public double InfrastructureFactor { get; set; } = 1.0;

    // === Population ===

    /// <summary>Total population.</summary>
    public long Population { get; set; }

    /// <summary>Labor force (working-age population willing to work).</summary>
    public long LaborForce { get; set; }

    /// <summary>Currently employed workers.</summary>
    public long Employed { get; set; }

    /// <summary>Unemployment rate [0, 1].</summary>
    public double UnemploymentRate => LaborForce > 0 ? 1.0 - (double)Employed / LaborForce : 0.0;

    // === Wages ===

    /// <summary>Average wage in this region (cents per worker per tick).</summary>
    public long AverageWage { get; set; }

    /// <summary>Wages by sector.</summary>
    public long[] SectorWages { get; } = new long[CommodityConstants.Count];

    // === Political ===

    /// <summary>Unrest level [0, 100].</summary>
    public double Unrest { get; set; }

    /// <summary>Food insecurity index [0, 1].</summary>
    public double FoodInsecurity { get; set; }

    /// <summary>Inequality index (Gini proxy) [0, 1].</summary>
    public double Inequality { get; set; }

    // === Inventory ===

    /// <summary>Commodity stockpiles in this region.</summary>
    public double[] Inventory { get; } = new double[CommodityConstants.Count];

    // === Demand ===

    /// <summary>Total demand per commodity from this region.</summary>
    public double[] Demand { get; } = new double[CommodityConstants.Count];

    /// <summary>Total supply per commodity from this region.</summary>
    public double[] Supply { get; } = new double[CommodityConstants.Count];
}
