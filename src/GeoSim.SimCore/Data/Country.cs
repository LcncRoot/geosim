namespace GeoSim.SimCore.Data;

/// <summary>
/// A sovereign nation in the simulation.
/// Contains aggregate economic, fiscal, political, and military state.
/// </summary>
public sealed class Country
{
    /// <summary>Integer ID for array indexing.</summary>
    public int Id { get; init; }

    /// <summary>ISO 3166-1 alpha-3 code (e.g., "KOR", "USA").</summary>
    public required string Code { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    // === Economic Aggregates ===

    /// <summary>Nominal GDP in cents (smallest currency unit).</summary>
    public long Gdp { get; set; }

    /// <summary>GDP from previous tick for growth calculation.</summary>
    public long GdpPrevious { get; set; }

    /// <summary>Consumer Price Index (base period = 1.0).</summary>
    public double Cpi { get; set; } = 1.0;

    /// <summary>CPI from 52 ticks ago for annual inflation.</summary>
    public double CpiYearAgo { get; set; } = 1.0;

    /// <summary>Annual inflation rate (computed).</summary>
    public double InflationRate => CpiYearAgo > 0 ? (Cpi / CpiYearAgo) - 1.0 : 0.0;

    // === Labor ===

    /// <summary>Total labor force (workers).</summary>
    public long LaborForce { get; set; }

    /// <summary>Total employed workers.</summary>
    public long Employed { get; set; }

    /// <summary>Unemployment rate [0, 1].</summary>
    public double UnemploymentRate => LaborForce > 0 ? 1.0 - (double)Employed / LaborForce : 0.0;

    /// <summary>Total wages paid this tick (cents).</summary>
    public long TotalWages { get; set; }

    // === Fiscal ===

    /// <summary>National debt (cents). Positive = government owes.</summary>
    public long Debt { get; set; }

    /// <summary>Debt-to-GDP ratio.</summary>
    public double DebtToGdp => Gdp > 0 ? (double)Debt / Gdp : 0.0;

    /// <summary>Current interest rate on debt [0, 1].</summary>
    public double InterestRate { get; set; }

    /// <summary>Base interest rate before risk premium.</summary>
    public double BaseInterestRate { get; set; } = 0.02;

    /// <summary>Foreign exchange reserves (cents in USD equivalent).</summary>
    public long FxReserves { get; set; }

    // === Tax Rates ===

    /// <summary>Effective income tax rate [0, 1].</summary>
    public double IncomeTaxRate { get; set; } = 0.15;

    /// <summary>Corporate tax rate [0, 1].</summary>
    public double CorporateTaxRate { get; set; } = 0.20;

    /// <summary>Value-added tax rate [0, 1].</summary>
    public double VatRate { get; set; } = 0.10;

    // === Budget ===

    /// <summary>Tax revenue this tick (cents).</summary>
    public long TaxRevenue { get; set; }

    /// <summary>Government spending this tick (cents).</summary>
    public long GovSpending { get; set; }

    /// <summary>Budget balance (revenue - spending).</summary>
    public long BudgetBalance => TaxRevenue - GovSpending;

    // === Spending Allocations (fractions of total spending) ===

    public double WelfareSpendingShare { get; set; } = 0.35;
    public double EducationSpendingShare { get; set; } = 0.15;
    public double DefenseSpendingShare { get; set; } = 0.13;
    public double InfrastructureSpendingShare { get; set; } = 0.10;
    public double HealthcareSpendingShare { get; set; } = 0.08;

    // === Trade ===

    /// <summary>Trade balance this tick (exports - imports, in cents).</summary>
    public long TradeBalance { get; set; }

    /// <summary>Import propensity per commodity [0, 1].</summary>
    public double[] ImportPropensity { get; } = new double[10];

    /// <summary>Export propensity per commodity [0, 1].</summary>
    public double[] ExportPropensity { get; } = new double[10];

    // === Political ===

    /// <summary>Government legitimacy [0, 100].</summary>
    public double Legitimacy { get; set; } = 50.0;

    /// <summary>Corruption index [0, 1] where 0 = clean.</summary>
    public double Corruption { get; set; }

    /// <summary>Average unrest across regions [0, 100].</summary>
    public double AverageUnrest { get; set; }

    /// <summary>War weariness [0, 1].</summary>
    public double WarWeariness { get; set; }

    /// <summary>Whether currently at war.</summary>
    public bool AtWar { get; set; }

    // === Military ===

    /// <summary>Total military power (abstract units).</summary>
    public double MilitaryPower { get; set; }

    /// <summary>Military goods required per tick.</summary>
    public double MilitaryGoodsRequired { get; set; }

    /// <summary>Military procurement satisfaction [0, 1].</summary>
    public double ProcurementSatisfaction { get; set; } = 1.0;

    // === Regions ===

    /// <summary>Regions within this country (by region ID).</summary>
    public int[] RegionIds { get; set; } = [];

    // === Factions ===

    /// <summary>Faction IDs active in this country.</summary>
    public int[] FactionIds { get; set; } = [];

    // === Market Prices ===

    /// <summary>Market prices per commodity in this country.</summary>
    public double[] Prices { get; } = new double[10];

    /// <summary>Smoothed display prices per commodity.</summary>
    public double[] DisplayPrices { get; } = new double[10];

    /// <summary>Initial prices for bound calculations.</summary>
    public double[] InitialPrices { get; } = new double[10];

    // === Consumption basket weights for CPI ===

    /// <summary>Consumer basket weights per commodity (should sum to 1).</summary>
    public double[] ConsumptionWeights { get; } = new double[10];
}
