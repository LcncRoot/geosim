namespace GeoSim.SimCore.Systems;

using GeoSim.SimCore.Data;

/// <summary>
/// Stateless labor system implementing employment and wage dynamics.
/// From equations.md §5.
/// </summary>
public static class LaborSystem
{
    /// <summary>Default wage adjustment speed ω.</summary>
    public const double DefaultWageAdjustmentSpeed = 0.02;

    /// <summary>Minimum wage adjustment speed.</summary>
    public const double MinWageAdjustmentSpeed = 0.01;

    /// <summary>Maximum wage adjustment speed.</summary>
    public const double MaxWageAdjustmentSpeed = 0.05;

    /// <summary>Friction factor for labor mobility between sectors.</summary>
    public const double LaborMobilityFriction = 0.1;

    /// <summary>Minimum wage floor (prevents zero wages).</summary>
    public const double MinWageFloor = 100; // cents per tick

    /// <summary>
    /// Update labor market for all regions in a country.
    /// </summary>
    public static void UpdateCountry(SimulationState state, int countryId)
    {
        var country = state.GetCountry(countryId);

        long totalEmployed = 0;
        long totalWages = 0;

        foreach (var region in state.GetRegionsForCountry(countryId))
        {
            UpdateRegion(state, region, country);
            totalEmployed += region.Employed;
            totalWages += CalculateRegionWages(region);
        }

        country.Employed = totalEmployed;
        country.TotalWages = totalWages;
    }

    /// <summary>
    /// Update labor market for a single region.
    /// </summary>
    public static void UpdateRegion(
        SimulationState state,
        Region region,
        Country country)
    {
        // Calculate total labor demand across all sectors
        double totalDemand = 0;
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            ref var sector = ref region.Sectors[s];
            double demand = CalculateLaborDemand(ref sector);
            totalDemand += demand;
        }

        // Allocate available labor to sectors
        double availableLabor = region.LaborForce;
        AllocateLaborToSectors(region, availableLabor, totalDemand);

        // Update region employment totals
        long employed = 0;
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            employed += (long)region.Sectors[s].LaborEmployed;
        }
        region.Employed = employed;

        // Update wages based on labor market tightness
        UpdateSectorWages(region, DefaultWageAdjustmentSpeed);
    }

    /// <summary>
    /// Calculate labor demand for a sector based on target output.
    /// L_demand = ℓ × Q_target
    /// From equations.md §5.1.
    /// </summary>
    public static double CalculateLaborDemand(ref Sector sector)
    {
        if (sector.LaborCoefficient <= 0) return 0;

        // Target output is capacity (could be modified by demand later)
        double targetOutput = sector.Capacity;
        return sector.LaborCoefficient * targetOutput;
    }

    /// <summary>
    /// Calculate labor demand for a given output target.
    /// </summary>
    public static double CalculateLaborDemand(double laborCoefficient, double targetOutput)
    {
        if (laborCoefficient <= 0) return 0;
        return laborCoefficient * targetOutput;
    }

    /// <summary>
    /// Allocate available labor to sectors based on demand.
    /// When demand exceeds supply, allocate proportionally.
    /// </summary>
    public static void AllocateLaborToSectors(
        Region region,
        double availableLabor,
        double totalDemand)
    {
        if (totalDemand <= 0 || availableLabor <= 0)
        {
            // No demand or no workers - zero employment
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                region.Sectors[s].LaborEmployed = 0;
            }
            return;
        }

        // Allocation factor: if demand > supply, proportionally reduce
        double allocationFactor = Math.Min(1.0, availableLabor / totalDemand);

        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            ref var sector = ref region.Sectors[s];
            double demand = CalculateLaborDemand(ref sector);
            sector.LaborEmployed = demand * allocationFactor;
        }
    }

    /// <summary>
    /// Calculate unemployment rate for a country.
    /// U = 1 - Employed / LaborForce
    /// From equations.md §5.3.
    /// </summary>
    public static double CalculateUnemploymentRate(long employed, long laborForce)
    {
        if (laborForce <= 0) return 0;
        return 1.0 - (double)employed / laborForce;
    }

    /// <summary>
    /// Calculate unemployment rate for a country.
    /// </summary>
    public static double CalculateUnemploymentRate(Country country)
    {
        return CalculateUnemploymentRate(country.Employed, country.LaborForce);
    }

    /// <summary>
    /// Calculate unemployment rate for a region.
    /// </summary>
    public static double CalculateUnemploymentRate(Region region)
    {
        return CalculateUnemploymentRate(region.Employed, region.LaborForce);
    }

    /// <summary>
    /// Update sector wages based on labor market tightness.
    /// W_t+1 = W_t × (1 + ω × (demand/supply - 1))
    /// From equations.md §5.4.
    /// </summary>
    public static void UpdateSectorWages(Region region, double adjustmentSpeed)
    {
        adjustmentSpeed = Math.Clamp(adjustmentSpeed, MinWageAdjustmentSpeed, MaxWageAdjustmentSpeed);

        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            ref var sector = ref region.Sectors[s];
            double demand = CalculateLaborDemand(ref sector);
            double supply = sector.LaborEmployed;

            double newWage = CalculateNewWage(
                region.SectorWages[s],
                demand,
                supply,
                adjustmentSpeed);

            region.SectorWages[s] = (long)newWage;
        }
    }

    /// <summary>
    /// Calculate new wage based on labor market tightness.
    /// W_t+1 = W_t × (1 + ω × (demand/supply - 1))
    /// </summary>
    public static double CalculateNewWage(
        double currentWage,
        double laborDemand,
        double laborSupply,
        double adjustmentSpeed)
    {
        if (currentWage <= 0) currentWage = MinWageFloor;

        // If no supply, wages should rise significantly
        if (laborSupply <= 0)
        {
            if (laborDemand > 0)
            {
                // High demand, no supply = max wage increase
                return currentWage * (1.0 + adjustmentSpeed * 0.5);
            }
            return currentWage; // No demand, no supply = stable
        }

        // Labor market tightness
        double tightness = laborDemand / laborSupply;

        // Wage adjustment: tight market (demand > supply) raises wages
        double adjustment = adjustmentSpeed * (tightness - 1.0);

        // Clamp adjustment to prevent extreme swings
        adjustment = Math.Clamp(adjustment, -0.1, 0.1);

        double newWage = currentWage * (1.0 + adjustment);

        // Apply floor
        return Math.Max(newWage, MinWageFloor);
    }

    /// <summary>
    /// Calculate total wages paid in a region.
    /// </summary>
    public static long CalculateRegionWages(Region region)
    {
        long totalWages = 0;
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            totalWages += (long)(region.Sectors[s].LaborEmployed * region.SectorWages[s]);
        }
        return totalWages;
    }

    /// <summary>
    /// Calculate average wage across all sectors in a region.
    /// </summary>
    public static double CalculateAverageWage(Region region)
    {
        double totalWages = 0;
        double totalEmployed = 0;

        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            totalWages += region.Sectors[s].LaborEmployed * region.SectorWages[s];
            totalEmployed += region.Sectors[s].LaborEmployed;
        }

        return totalEmployed > 0 ? totalWages / totalEmployed : 0;
    }

    /// <summary>
    /// Calculate labor productivity (output per worker) for a sector.
    /// </summary>
    public static double CalculateLaborProductivity(ref Sector sector)
    {
        if (sector.LaborEmployed <= 0) return 0;
        return sector.Output / sector.LaborEmployed;
    }

    /// <summary>
    /// Calculate labor share of value added.
    /// </summary>
    public static double CalculateLaborShare(double wages, double valueAdded)
    {
        if (valueAdded <= 0) return 0;
        return wages / valueAdded;
    }

    /// <summary>
    /// Simulate labor migration between sectors based on wage differentials.
    /// Higher-wage sectors attract workers from lower-wage sectors.
    /// </summary>
    public static void SimulateLaborMobility(Region region, double mobilityRate)
    {
        // Calculate average wage
        double avgWage = CalculateAverageWage(region);
        if (avgWage <= 0) return;

        mobilityRate = Math.Clamp(mobilityRate, 0, LaborMobilityFriction);

        // Workers move from low-wage to high-wage sectors
        double totalMigration = 0;
        Span<double> migrations = stackalloc double[CommodityConstants.Count];

        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            double sectorWage = region.SectorWages[s];
            double wageRatio = sectorWage / avgWage;

            // Sectors above average gain workers, below average lose
            double migration = region.Sectors[s].LaborEmployed * mobilityRate * (wageRatio - 1.0);
            migrations[s] = migration;
            totalMigration += migration;
        }

        // Net migration should be zero (workers move, not created/destroyed)
        // Normalize to ensure conservation
        if (Math.Abs(totalMigration) > 0.01)
        {
            double adjustment = -totalMigration / CommodityConstants.Count;
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                migrations[s] += adjustment;
            }
        }

        // Apply migrations
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            region.Sectors[s].LaborEmployed = Math.Max(0,
                region.Sectors[s].LaborEmployed + migrations[s]);
        }
    }

    /// <summary>
    /// Get labor market summary for a country.
    /// </summary>
    public static (double unemploymentRate, double avgWage, long totalEmployed)
        GetLaborMarketSummary(SimulationState state, int countryId)
    {
        var country = state.GetCountry(countryId);

        double totalWages = 0;
        long totalEmployed = 0;

        foreach (var region in state.GetRegionsForCountry(countryId))
        {
            totalWages += CalculateAverageWage(region) * region.Employed;
            totalEmployed += region.Employed;
        }

        double unemploymentRate = CalculateUnemploymentRate(country);
        double avgWage = totalEmployed > 0 ? totalWages / totalEmployed : 0;

        return (unemploymentRate, avgWage, totalEmployed);
    }
}
