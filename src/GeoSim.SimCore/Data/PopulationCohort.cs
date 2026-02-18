namespace GeoSim.SimCore.Data;

/// <summary>
/// Wealth level categories for population cohorts.
/// </summary>
public enum WealthLevel
{
    Subsistence = 0,
    Poor = 1,
    Middle = 2,
    Wealthy = 3,
    Rich = 4
}

/// <summary>
/// A demographic group within a region with shared consumption patterns.
/// </summary>
public sealed class PopulationCohort
{
    /// <summary>Unique cohort ID.</summary>
    public int Id { get; init; }

    /// <summary>Region this cohort belongs to.</summary>
    public int RegionId { get; init; }

    /// <summary>Sector this cohort works in (for wage tracking).</summary>
    public Commodity PrimarySector { get; init; }

    /// <summary>Current wealth level category.</summary>
    public WealthLevel WealthLevel { get; set; } = WealthLevel.Middle;

    /// <summary>Population count in this cohort.</summary>
    public long Population { get; set; }

    /// <summary>Accumulated wealth (savings, in cents).</summary>
    public long Wealth { get; set; }

    /// <summary>Income this tick (wages + transfers - taxes).</summary>
    public long Income { get; set; }

    /// <summary>Cost of living this tick.</summary>
    public long CostOfLiving { get; set; }

    /// <summary>Savings rate [0, 1].</summary>
    public double SavingsRate { get; set; } = 0.15;

    // === Consumption Demand ===
    // Units consumed per capita per tick, varies by wealth level.
    // These are multipliers on base consumption.

    /// <summary>Consumption multipliers by commodity for this cohort.</summary>
    public double[] ConsumptionMultipliers { get; } = new double[10];

    /// <summary>
    /// Get base consumption per capita for a commodity at a wealth level.
    /// From equations.md §7.1 consumption curves.
    /// </summary>
    public static double GetBaseConsumption(WealthLevel wealth, Commodity commodity)
    {
        // Default consumption curves from spec
        return (wealth, commodity) switch
        {
            (WealthLevel.Subsistence, Commodity.Food) => 1.0,
            (WealthLevel.Subsistence, Commodity.Energy) => 0.3,
            (WealthLevel.Subsistence, Commodity.ConsumerGoods) => 0.1,
            (WealthLevel.Subsistence, Commodity.Electronics) => 0.0,
            (WealthLevel.Subsistence, Commodity.Services) => 0.1,

            (WealthLevel.Poor, Commodity.Food) => 1.0,
            (WealthLevel.Poor, Commodity.Energy) => 0.6,
            (WealthLevel.Poor, Commodity.ConsumerGoods) => 0.3,
            (WealthLevel.Poor, Commodity.Electronics) => 0.1,
            (WealthLevel.Poor, Commodity.Services) => 0.3,

            (WealthLevel.Middle, Commodity.Food) => 1.0,
            (WealthLevel.Middle, Commodity.Energy) => 1.0,
            (WealthLevel.Middle, Commodity.ConsumerGoods) => 0.8,
            (WealthLevel.Middle, Commodity.Electronics) => 0.5,
            (WealthLevel.Middle, Commodity.Services) => 1.0,

            (WealthLevel.Wealthy, Commodity.Food) => 1.0,
            (WealthLevel.Wealthy, Commodity.Energy) => 1.3,
            (WealthLevel.Wealthy, Commodity.ConsumerGoods) => 1.5,
            (WealthLevel.Wealthy, Commodity.Electronics) => 1.2,
            (WealthLevel.Wealthy, Commodity.Services) => 2.0,

            (WealthLevel.Rich, Commodity.Food) => 1.0,
            (WealthLevel.Rich, Commodity.Energy) => 1.5,
            (WealthLevel.Rich, Commodity.ConsumerGoods) => 2.0,
            (WealthLevel.Rich, Commodity.Electronics) => 2.0,
            (WealthLevel.Rich, Commodity.Services) => 3.5,

            // Other commodities have minimal direct consumer demand
            _ => 0.0
        };
    }

    /// <summary>
    /// Calculate total consumption demand from this cohort for a commodity.
    /// </summary>
    public double GetDemand(Commodity commodity)
    {
        double baseDemand = GetBaseConsumption(WealthLevel, commodity);
        double multiplier = ConsumptionMultipliers[(int)commodity];
        if (multiplier <= 0) multiplier = 1.0;
        return Population * baseDemand * multiplier;
    }
}
