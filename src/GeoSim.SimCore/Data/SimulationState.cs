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

    /// <summary>All trade relations (bilateral pairs).</summary>
    public TradeRelation[] TradeRelations { get; set; } = [];

    // === Resource System ===

    /// <summary>All resource deposits across all regions.</summary>
    public ResourceDeposit[] Deposits { get; set; } = [];

    /// <summary>All extraction facilities.</summary>
    public ExtractionFacility[] ExtractionFacilities { get; set; } = [];

    /// <summary>All manufacturing facilities.</summary>
    public ManufacturingFacility[] ManufacturingFacilities { get; set; } = [];

    /// <summary>All population cohorts.</summary>
    public PopulationCohort[] PopulationCohorts { get; set; } = [];

    /// <summary>All military formations.</summary>
    public MilitaryFormation[] MilitaryFormations { get; set; } = [];

    /// <summary>Spoilage rates per commodity per tick [0, 1].</summary>
    public double[] SpoilageRates { get; set; } = new double[10];

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

    /// <summary>Get resource deposit by ID.</summary>
    public ResourceDeposit GetDeposit(int id) => Deposits[id];

    /// <summary>Get extraction facility by ID.</summary>
    public ExtractionFacility GetExtractionFacility(int id) => ExtractionFacilities[id];

    /// <summary>Get manufacturing facility by ID.</summary>
    public ManufacturingFacility GetManufacturingFacility(int id) => ManufacturingFacilities[id];

    /// <summary>Get all deposits in a region.</summary>
    public IEnumerable<ResourceDeposit> GetDepositsForRegion(int regionId)
    {
        foreach (var deposit in Deposits)
        {
            if (deposit.RegionId == regionId)
                yield return deposit;
        }
    }

    /// <summary>Get all extraction facilities in a region.</summary>
    public IEnumerable<ExtractionFacility> GetExtractionFacilitiesForRegion(int regionId)
    {
        foreach (var facility in ExtractionFacilities)
        {
            if (facility.RegionId == regionId)
                yield return facility;
        }
    }

    /// <summary>Get all manufacturing facilities in a region.</summary>
    public IEnumerable<ManufacturingFacility> GetManufacturingFacilitiesForRegion(int regionId)
    {
        foreach (var facility in ManufacturingFacilities)
        {
            if (facility.RegionId == regionId)
                yield return facility;
        }
    }

    /// <summary>Get trade relation between two countries (if exists).</summary>
    public TradeRelation? GetTradeRelation(int fromCountryId, int toCountryId)
    {
        foreach (var relation in TradeRelations)
        {
            if (relation.FromCountryId == fromCountryId && relation.ToCountryId == toCountryId)
                return relation;
        }
        return null;
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
