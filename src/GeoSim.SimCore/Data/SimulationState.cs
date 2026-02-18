namespace GeoSim.SimCore.Data;

/// <summary>
/// The complete simulation state at a point in time.
/// This is the single source of truth — all mutable state lives here.
/// Systems are stateless functions that read and mutate this state.
/// </summary>
public sealed class SimulationState
{
    // === Time ===

    /// <summary>Current tick number (0 = start).</summary>
    public int Tick { get; set; }

    /// <summary>Ticks per year (52 for weekly).</summary>
    public int TicksPerYear { get; init; } = 52;

    /// <summary>Current year (for display).</summary>
    public int Year => StartYear + (Tick / TicksPerYear);

    /// <summary>Week within current year (1-52).</summary>
    public int Week => (Tick % TicksPerYear) + 1;

    /// <summary>Starting year of the scenario.</summary>
    public int StartYear { get; init; } = 2024;

    // === Determinism ===

    /// <summary>RNG seed for reproducibility.</summary>
    public int RandomSeed { get; init; }

    /// <summary>Current RNG state (for mid-simulation saves).</summary>
    public Random Rng { get; private set; } = null!;

    // === Entities ===

    /// <summary>All countries in the simulation.</summary>
    public Country[] Countries { get; set; } = [];

    /// <summary>All regions across all countries.</summary>
    public Region[] Regions { get; set; } = [];

    /// <summary>All factions across all countries.</summary>
    public Faction[] Factions { get; set; } = [];

    // === Static Data (per country) ===

    /// <summary>
    /// Technical coefficient matrices indexed by country ID.
    /// </summary>
    public TechnicalCoefficientMatrix[] CoefficientMatrices { get; set; } = [];

    /// <summary>
    /// Labor coefficients per commodity (workers per unit output).
    /// Same for all countries (could be per-country if needed).
    /// </summary>
    public double[] LaborCoefficients { get; set; } = new double[10];

    /// <summary>
    /// Price sensitivities per commodity for price adjustment.
    /// </summary>
    public double[] PriceSensitivities { get; set; } = new double[10];

    // === Lookup Helpers ===

    /// <summary>Get country by ID.</summary>
    public Country GetCountry(int id) => Countries[id];

    /// <summary>Get region by ID.</summary>
    public Region GetRegion(int id) => Regions[id];

    /// <summary>Get faction by ID.</summary>
    public Faction GetFaction(int id) => Factions[id];

    /// <summary>Get coefficient matrix for a country.</summary>
    public TechnicalCoefficientMatrix GetCoefficients(int countryId) => CoefficientMatrices[countryId];

    /// <summary>Get all regions for a country.</summary>
    public IEnumerable<Region> GetRegionsForCountry(int countryId)
    {
        foreach (var region in Regions)
        {
            if (region.CountryId == countryId)
                yield return region;
        }
    }

    /// <summary>Get all factions for a country.</summary>
    public IEnumerable<Faction> GetFactionsForCountry(int countryId)
    {
        foreach (var faction in Factions)
        {
            if (faction.CountryId == countryId)
                yield return faction;
        }
    }

    // === Initialization ===

    /// <summary>
    /// Initialize RNG from seed. Call after deserialization.
    /// </summary>
    public void InitializeRng()
    {
        Rng = new Random(RandomSeed + Tick);
    }

    /// <summary>
    /// Create an empty simulation state.
    /// </summary>
    public static SimulationState CreateEmpty(int seed = 0)
    {
        var state = new SimulationState { RandomSeed = seed };
        state.InitializeRng();
        return state;
    }
}
