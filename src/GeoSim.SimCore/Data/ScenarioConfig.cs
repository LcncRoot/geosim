using System.Text.Json.Serialization;

namespace GeoSim.SimCore.Data;

/// <summary>
/// Top-level scenario configuration loaded from JSON.
/// Contains all initial conditions for a simulation.
/// </summary>
public sealed class ScenarioConfig
{
    /// <summary>Scenario name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Scenario description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    /// <summary>Author.</summary>
    [JsonPropertyName("author")]
    public string Author { get; init; } = "";

    /// <summary>Version string.</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";

    /// <summary>Starting year.</summary>
    [JsonPropertyName("startYear")]
    public int StartYear { get; init; } = 2024;

    /// <summary>Random seed for determinism.</summary>
    [JsonPropertyName("randomSeed")]
    public int RandomSeed { get; init; }

    // === Countries ===

    [JsonPropertyName("countries")]
    public CountryConfig[] Countries { get; init; } = [];

    // === Global Parameters ===

    /// <summary>Price sensitivity per commodity.</summary>
    [JsonPropertyName("priceSensitivities")]
    public double[] PriceSensitivities { get; init; } = [];

    /// <summary>Labor coefficients per commodity.</summary>
    [JsonPropertyName("laborCoefficients")]
    public double[] LaborCoefficients { get; init; } = [];

    /// <summary>Spoilage rates per commodity.</summary>
    [JsonPropertyName("spoilageRates")]
    public double[] SpoilageRates { get; init; } = [];

    /// <summary>Base interest rate.</summary>
    [JsonPropertyName("baseInterestRate")]
    public double BaseInterestRate { get; init; } = 0.02;
}

/// <summary>
/// Configuration for a single country in the scenario.
/// </summary>
public sealed class CountryConfig
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    // === Initial Economic State ===

    [JsonPropertyName("gdp")]
    public long Gdp { get; init; }

    [JsonPropertyName("debt")]
    public long Debt { get; init; }

    [JsonPropertyName("laborForce")]
    public long LaborForce { get; init; }

    [JsonPropertyName("population")]
    public long Population { get; init; }

    // === Tax Rates ===

    [JsonPropertyName("incomeTaxRate")]
    public double IncomeTaxRate { get; init; } = 0.15;

    [JsonPropertyName("corporateTaxRate")]
    public double CorporateTaxRate { get; init; } = 0.20;

    [JsonPropertyName("vatRate")]
    public double VatRate { get; init; } = 0.10;

    // === Trade ===

    [JsonPropertyName("importPropensity")]
    public double[] ImportPropensity { get; init; } = [];

    [JsonPropertyName("exportPropensity")]
    public double[] ExportPropensity { get; init; } = [];

    // === Initial Prices ===

    [JsonPropertyName("initialPrices")]
    public double[] InitialPrices { get; init; } = [];

    // === Consumption Weights (for CPI) ===

    [JsonPropertyName("consumptionWeights")]
    public double[] ConsumptionWeights { get; init; } = [];

    // === Technical Coefficients ===

    /// <summary>
    /// Flattened 10x10 technical coefficient matrix.
    /// Row-major: coefficients[i * 10 + j] = input i per output j.
    /// </summary>
    [JsonPropertyName("technicalCoefficients")]
    public double[] TechnicalCoefficients { get; init; } = [];

    // === Regions ===

    [JsonPropertyName("regions")]
    public RegionConfig[] Regions { get; init; } = [];

    // === Factions ===

    [JsonPropertyName("factions")]
    public FactionConfig[] Factions { get; init; } = [];
}

/// <summary>
/// Configuration for a region within a country.
/// </summary>
public sealed class RegionConfig
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("population")]
    public long Population { get; init; }

    [JsonPropertyName("laborForce")]
    public long LaborForce { get; init; }

    [JsonPropertyName("infrastructureFactor")]
    public double InfrastructureFactor { get; init; } = 1.0;

    /// <summary>Initial sector capacities by commodity.</summary>
    [JsonPropertyName("sectorCapacities")]
    public double[] SectorCapacities { get; init; } = [];

    /// <summary>Resource deposits in this region.</summary>
    [JsonPropertyName("deposits")]
    public DepositConfig[] Deposits { get; init; } = [];
}

/// <summary>
/// Configuration for a resource deposit.
/// </summary>
public sealed class DepositConfig
{
    [JsonPropertyName("subtype")]
    public required string Subtype { get; init; }

    [JsonPropertyName("resourceType")]
    public Commodity ResourceType { get; init; }

    [JsonPropertyName("totalReserves")]
    public long TotalReserves { get; init; }

    [JsonPropertyName("baseYield")]
    public double BaseYield { get; init; }

    [JsonPropertyName("difficulty")]
    public double Difficulty { get; init; } = 1.0;

    [JsonPropertyName("discoveryState")]
    public DiscoveryState DiscoveryState { get; init; } = DiscoveryState.Proven;
}

/// <summary>
/// Configuration for a political faction.
/// </summary>
public sealed class FactionConfig
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("basePower")]
    public double BasePower { get; init; }

    [JsonPropertyName("baseSatisfaction")]
    public double BaseSatisfaction { get; init; } = 50.0;

    [JsonPropertyName("redLineType")]
    public RedLineType RedLineType { get; init; }

    [JsonPropertyName("redLineThreshold")]
    public double RedLineThreshold { get; init; }

    // Preference weights (positive = wants higher, negative = wants lower)
    [JsonPropertyName("prefCorporateTax")]
    public double PrefCorporateTax { get; init; }

    [JsonPropertyName("prefWelfareSpending")]
    public double PrefWelfareSpending { get; init; }

    [JsonPropertyName("prefMilitarySpending")]
    public double PrefMilitarySpending { get; init; }

    [JsonPropertyName("prefGdpGrowth")]
    public double PrefGdpGrowth { get; init; }

    [JsonPropertyName("prefLowUnemployment")]
    public double PrefLowUnemployment { get; init; }
}
