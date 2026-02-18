namespace GeoSim.SimCore.Systems;

using GeoSim.SimCore.Data;

/// <summary>
/// Stateless production system implementing Leontief input-output economics.
/// This is the hot loop — runs for every region-sector every tick.
/// </summary>
public static class ProductionSystem
{
    /// <summary>
    /// Bottleneck dominance factor for soft Leontief.
    /// 0.6 = bottleneck-dominant but not binary.
    /// </summary>
    public const double Alpha = 0.6;

    /// <summary>
    /// Run production for all regions in a country.
    /// </summary>
    public static void UpdateCountry(SimulationState state, int countryId)
    {
        var country = state.GetCountry(countryId);
        var coefficients = state.GetCoefficients(countryId);

        foreach (var region in state.GetRegionsForCountry(countryId))
        {
            UpdateRegion(state, region, country, coefficients);
        }
    }

    /// <summary>
    /// Run production for all sectors in a region.
    /// </summary>
    public static void UpdateRegion(
        SimulationState state,
        Region region,
        Country country,
        TechnicalCoefficientMatrix coefficients)
    {
        // First pass: calculate output for each sector
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            ref var sector = ref region.Sectors[s];

            double output = CalculateSectorOutput(
                ref sector,
                region,
                country,
                coefficients,
                state.LaborCoefficients[s]);

            sector.Output = output;

            // Update supply
            region.Supply[s] = output + region.Inventory[s];
        }

        // Second pass: consume inputs and calculate value added
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            ref var sector = ref region.Sectors[s];

            ConsumeInputs(ref sector, region, coefficients, s);

            sector.ValueAdded = CalculateValueAdded(
                sector.Output,
                country.Prices,
                coefficients,
                s);
        }
    }

    /// <summary>
    /// Calculate output for a single sector using soft Leontief production function.
    /// Q = min(capacity, labor/coeff, φ(inputs)) × infrastructure × efficiency
    /// </summary>
    public static double CalculateSectorOutput(
        ref Sector sector,
        Region region,
        Country country,
        TechnicalCoefficientMatrix coefficients,
        double laborCoefficient)
    {
        // Capacity constraint: K × utilization (assume full utilization for now)
        double capacityConstraint = sector.Capacity;

        // Labor constraint: L / labor_coeff
        double laborConstraint = laborCoefficient > 0
            ? sector.LaborEmployed / laborCoefficient
            : double.MaxValue;

        // Input constraint: soft Leontief
        double inputConstraint = CalculateSoftLeontief(
            sector.Id,
            capacityConstraint, // target output
            region.Inventory,
            coefficients);

        // Take minimum of all constraints
        double rawOutput = Math.Min(capacityConstraint, Math.Min(laborConstraint, inputConstraint));

        // Apply modifiers
        double output = rawOutput * region.InfrastructureFactor * sector.Efficiency;

        // Can't produce negative
        return Math.Max(0, output);
    }

    /// <summary>
    /// Soft Leontief input availability function.
    /// φ(I) = α × bottleneck + (1-α) × average_satisfaction
    /// </summary>
    public static double CalculateSoftLeontief(
        int outputSector,
        double targetOutput,
        double[] inventory,
        TechnicalCoefficientMatrix coefficients)
    {
        if (targetOutput <= 0) return 0;

        double bottleneck = double.MaxValue;
        double totalSatisfaction = 0;
        int inputCount = 0;

        for (int i = 0; i < CommodityConstants.Count; i++)
        {
            double coeff = coefficients[i, outputSector];
            if (coeff <= 0) continue; // This input not required

            double required = coeff * targetOutput;
            double available = inventory[i];
            double satisfaction = Math.Min(1.0, available / required);

            // Track bottleneck (minimum)
            if (satisfaction < bottleneck)
                bottleneck = satisfaction;

            totalSatisfaction += satisfaction;
            inputCount++;
        }

        // If no inputs required, no constraint
        if (inputCount == 0) return targetOutput;

        // If bottleneck is still MaxValue, all inputs fully satisfied
        if (bottleneck == double.MaxValue) bottleneck = 1.0;

        double averageSatisfaction = totalSatisfaction / inputCount;

        // Soft Leontief: weighted combination
        double effectiveFactor = Alpha * bottleneck + (1 - Alpha) * averageSatisfaction;

        return targetOutput * effectiveFactor;
    }

    /// <summary>
    /// Consume inputs from inventory based on actual production.
    /// </summary>
    public static void ConsumeInputs(
        ref Sector sector,
        Region region,
        TechnicalCoefficientMatrix coefficients,
        int sectorIndex)
    {
        double output = sector.Output;
        if (output <= 0) return;

        for (int i = 0; i < CommodityConstants.Count; i++)
        {
            double coeff = coefficients[i, sectorIndex];
            if (coeff <= 0) continue;

            double required = coeff * output;
            double available = region.Inventory[i];

            // Consume what's available, up to required amount
            double consumed = Math.Min(required, available);
            region.Inventory[i] -= consumed;
        }
    }

    /// <summary>
    /// Calculate value added: output revenue minus input costs.
    /// VA = Q × P_output - Σ(a_is × Q × P_i)
    /// </summary>
    public static double CalculateValueAdded(
        double output,
        double[] prices,
        TechnicalCoefficientMatrix coefficients,
        int sectorIndex)
    {
        if (output <= 0) return 0;

        double revenue = output * prices[sectorIndex];
        double inputCost = coefficients.CalculateInputCost(
            (Commodity)sectorIndex,
            output,
            prices);

        return revenue - inputCost;
    }

    /// <summary>
    /// Calculate extraction output from a facility.
    /// From resources.md §3.
    /// </summary>
    public static double CalculateExtractionOutput(
        ExtractionFacility facility,
        ResourceDeposit deposit,
        Region region,
        double technologyModifier = 1.0)
    {
        if (facility.Level <= 0 || deposit.IsExhausted)
            return 0;

        // workforce_factor = min(1.0, workers / workers_required)
        double workforceFactor = facility.WorkersRequired > 0
            ? Math.Min(1.0, (double)facility.Workers / facility.WorkersRequired)
            : 1.0;

        // condition_factor = condition ^ 0.5 (degrades gracefully)
        double conditionFactor = Math.Sqrt(Math.Max(0, facility.Condition));

        // extraction = base_yield × level × workforce × condition × infrastructure × tech
        double output = deposit.BaseYield
            * facility.Level
            * workforceFactor
            * conditionFactor
            * region.InfrastructureFactor
            * technologyModifier;

        // Can't extract more than remaining reserves
        output = Math.Min(output, deposit.Remaining);

        return Math.Max(0, output);
    }

    /// <summary>
    /// Calculate manufacturing output from a facility.
    /// </summary>
    public static double CalculateManufacturingOutput(
        ManufacturingFacility facility,
        Region region,
        double[] inputSatisfaction)
    {
        if (facility.Level <= 0)
            return 0;

        double workforceFactor = facility.WorkersRequired > 0
            ? Math.Min(1.0, (double)facility.Workers / facility.WorkersRequired)
            : 1.0;

        double conditionFactor = Math.Sqrt(Math.Max(0, facility.Condition));

        // Input satisfaction affects output (soft constraint)
        int commodityIndex = (int)facility.OutputCommodity;
        double inputFactor = commodityIndex < inputSatisfaction.Length
            ? inputSatisfaction[commodityIndex]
            : 1.0;

        double output = facility.BaseCapacity
            * facility.Level
            * workforceFactor
            * conditionFactor
            * region.InfrastructureFactor
            * inputFactor;

        return Math.Max(0, output);
    }
}
