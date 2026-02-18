namespace GeoSim.SimCore.Data;

/// <summary>
/// A production sector within a region. One of 50 ISIC industries.
/// Struct for cache-friendly storage when iterating thousands of sectors.
/// </summary>
public struct Sector
{
    /// <summary>ISIC sector index (0-49).</summary>
    public int Id;

    /// <summary>Which aggregated commodity this sector produces.</summary>
    public Commodity Commodity;

    /// <summary>Maximum output given installed capital (units/tick).</summary>
    public double Capacity;

    /// <summary>Current workers employed.</summary>
    public double LaborEmployed;

    /// <summary>Workers required per unit of output.</summary>
    public double LaborCoefficient;

    /// <summary>Output produced this tick (units).</summary>
    public double Output;

    /// <summary>Stockpiled inventory (units).</summary>
    public double Inventory;

    /// <summary>Current market price (currency units per unit).</summary>
    public double Price;

    /// <summary>Initial price for bound calculations.</summary>
    public double InitialPrice;

    /// <summary>Technology/productivity multiplier [0.5, 2.0].</summary>
    public double Efficiency;

    /// <summary>Value added this tick (output revenue minus input costs).</summary>
    public double ValueAdded;
}
