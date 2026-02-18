namespace GeoSim.SimCore.Data;

/// <summary>
/// A political faction within a country.
/// Factions have interests, power, and can destabilize governments.
/// </summary>
public sealed class Faction
{
    /// <summary>Integer ID for array indexing.</summary>
    public int Id { get; init; }

    /// <summary>Parent country ID.</summary>
    public int CountryId { get; init; }

    /// <summary>Display name (e.g., "Chaebol/Industrialists").</summary>
    public required string Name { get; init; }

    // === Power ===

    /// <summary>
    /// Political power share [0, 1].
    /// All factions in a country should sum to 1.
    /// </summary>
    public double Power { get; set; }

    /// <summary>Base power before dynamic adjustments.</summary>
    public double BasePower { get; init; }

    // === Satisfaction ===

    /// <summary>Current satisfaction level [0, 100].</summary>
    public double Satisfaction { get; set; } = 50.0;

    /// <summary>Base satisfaction before policy effects.</summary>
    public double BaseSatisfaction { get; set; } = 50.0;

    // === Policy Preferences ===
    // Weights indicate how much the faction cares about each dimension.
    // Positive = wants higher, negative = wants lower.

    /// <summary>Preference weight on corporate tax rate (negative = wants lower).</summary>
    public double PrefCorporateTax { get; set; }

    /// <summary>Preference weight on income tax rate.</summary>
    public double PrefIncomeTax { get; set; }

    /// <summary>Preference weight on welfare spending share.</summary>
    public double PrefWelfareSpending { get; set; }

    /// <summary>Preference weight on military spending share.</summary>
    public double PrefMilitarySpending { get; set; }

    /// <summary>Preference weight on trade openness.</summary>
    public double PrefTradeOpenness { get; set; }

    /// <summary>Preference weight on GDP growth.</summary>
    public double PrefGdpGrowth { get; set; }

    /// <summary>Preference weight on low unemployment.</summary>
    public double PrefLowUnemployment { get; set; }

    /// <summary>Preference weight on wage growth.</summary>
    public double PrefWageGrowth { get; set; }

    /// <summary>Preference weight on low corruption.</summary>
    public double PrefLowCorruption { get; set; }

    // === Red Lines ===
    // Crossing these triggers severe satisfaction penalties.

    /// <summary>Red line type (what metric is checked).</summary>
    public RedLineType RedLine { get; set; }

    /// <summary>Threshold value for red line.</summary>
    public double RedLineThreshold { get; set; }

    /// <summary>Whether red line is currently violated.</summary>
    public bool RedLineViolated { get; set; }

    /// <summary>Satisfaction penalty when red line is crossed.</summary>
    public double RedLinePenalty { get; set; } = 30.0;
}

/// <summary>
/// Types of red lines factions can have.
/// </summary>
public enum RedLineType
{
    None = 0,
    CorporateTaxAbove,      // e.g., Chaebol: corporate tax > 30%
    UnemploymentAbove,      // e.g., Labor: unemployment > 8%
    DefenseSpendingBelow,   // e.g., Security: defense < 2% GDP
    CorruptionAbove,        // e.g., Progressive: corruption > threshold
    FoodImportsAbove,       // e.g., Rural: food imports > 80%
    DefenseBudgetCutAbove   // e.g., Military: budget cuts > 20%
}
