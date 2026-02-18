namespace GeoSim.SimCore.Systems;

using GeoSim.SimCore.Data;

/// <summary>
/// Stateless price system implementing supply/demand price adjustment.
/// </summary>
public static class PriceSystem
{
    /// <summary>Maximum price change per tick (±50%).</summary>
    public const double MaxPriceChange = 0.5;

    /// <summary>Smoothing factor for displayed prices (0.7 = 70% new, 30% old).</summary>
    public const double SmoothingFactor = 0.7;

    /// <summary>Price floor multiplier relative to initial price.</summary>
    public const double PriceFloorMultiplier = 0.1;

    /// <summary>Price ceiling multiplier relative to initial price.</summary>
    public const double PriceCeilingMultiplier = 10.0;

    /// <summary>Epsilon to prevent division by zero.</summary>
    private const double Epsilon = 0.0001;

    /// <summary>
    /// Update prices for all commodities in a country based on supply/demand.
    /// </summary>
    public static void UpdateCountry(
        SimulationState state,
        Country country,
        double[] totalDemand,
        double[] totalSupply)
    {
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            double newPrice = CalculateNewPrice(
                country.Prices[s],
                totalDemand[s],
                totalSupply[s],
                state.PriceSensitivities[s],
                country.InitialPrices[s]);

            country.Prices[s] = newPrice;

            // Update smoothed display price
            country.DisplayPrices[s] = CalculateSmoothedPrice(
                newPrice,
                country.DisplayPrices[s]);
        }

        // Update CPI
        country.Cpi = CalculateCpi(country.Prices, country.InitialPrices, country.ConsumptionWeights);
    }

    /// <summary>
    /// Calculate new price based on excess demand.
    /// P(t+1) = P(t) * (1 + σ * clamp((D-S)/S, -maxΔ, +maxΔ))
    /// </summary>
    public static double CalculateNewPrice(
        double currentPrice,
        double demand,
        double supply,
        double sensitivity,
        double initialPrice)
    {
        // Prevent division by zero
        double effectiveSupply = Math.Max(supply, Epsilon);

        // Calculate excess demand ratio
        double excessDemand = demand - supply;
        double excessDemandRatio = excessDemand / effectiveSupply;

        // Clamp to prevent extreme swings
        double clampedRatio = Math.Clamp(excessDemandRatio, -MaxPriceChange, MaxPriceChange);

        // Apply price adjustment
        double newPrice = currentPrice * (1.0 + sensitivity * clampedRatio);

        // Apply price bounds
        double floor = initialPrice * PriceFloorMultiplier;
        double ceiling = initialPrice * PriceCeilingMultiplier;

        return Math.Clamp(newPrice, floor, ceiling);
    }

    /// <summary>
    /// Calculate smoothed display price.
    /// Smoothed = α * new + (1-α) * old
    /// </summary>
    public static double CalculateSmoothedPrice(double newPrice, double previousSmoothed)
    {
        return SmoothingFactor * newPrice + (1.0 - SmoothingFactor) * previousSmoothed;
    }

    /// <summary>
    /// Calculate Consumer Price Index.
    /// CPI = Σ(weight * price / base_price) / Σ(weight)
    /// </summary>
    public static double CalculateCpi(
        double[] currentPrices,
        double[] basePrices,
        double[] consumptionWeights)
    {
        double weightedSum = 0;
        double totalWeight = 0;

        for (int i = 0; i < CommodityConstants.Count; i++)
        {
            double weight = consumptionWeights[i];
            if (weight <= 0 || basePrices[i] <= 0) continue;

            weightedSum += weight * (currentPrices[i] / basePrices[i]);
            totalWeight += weight;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 1.0;
    }

    /// <summary>
    /// Calculate annual inflation rate from CPI values.
    /// Assumes weekly ticks (52 per year).
    /// </summary>
    public static double CalculateInflationRate(double currentCpi, double yearAgoCpi)
    {
        if (yearAgoCpi <= 0) return 0;
        return (currentCpi / yearAgoCpi) - 1.0;
    }

    /// <summary>
    /// Aggregate supply and demand across all regions for a country.
    /// </summary>
    public static void AggregateSupplyDemand(
        SimulationState state,
        int countryId,
        Span<double> totalDemand,
        Span<double> totalSupply)
    {
        totalDemand.Clear();
        totalSupply.Clear();

        foreach (var region in state.GetRegionsForCountry(countryId))
        {
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                totalDemand[s] += region.Demand[s];
                totalSupply[s] += region.Supply[s];
            }
        }
    }

    /// <summary>
    /// Get default price sensitivities per commodity.
    /// From equations.md §3.1.
    /// </summary>
    public static double[] GetDefaultSensitivities()
    {
        return
        [
            0.15, // Agriculture - moderate (essential food)
            0.18, // RareEarths - volatile (strategic)
            0.20, // Petroleum - very volatile (energy)
            0.15, // Coal - moderate volatility
            0.12, // Ore - moderate (metals)
            0.18, // Uranium - volatile (strategic)
            0.15, // Electricity - moderate (derived)
            0.12, // ConsumerGoods - moderate
            0.10, // IndustrialGoods - sticky
            0.08, // MilitaryGoods - government buyer
            0.10, // Electronics - contract-based
            0.05  // Services - very sticky
        ];
    }

    /// <summary>
    /// Get default consumption weights for CPI (South Korea approx).
    /// From equations.md §3.3.
    /// </summary>
    public static double[] GetDefaultConsumptionWeights()
    {
        return
        [
            0.14, // Agriculture (food)
            0.00, // RareEarths (not consumer)
            0.07, // Petroleum (fuel/heating)
            0.00, // Coal (not consumer)
            0.00, // Ore (not consumer)
            0.00, // Uranium (not consumer)
            0.08, // Electricity (utilities)
            0.20, // ConsumerGoods
            0.00, // IndustrialGoods (not consumer)
            0.00, // MilitaryGoods (not consumer)
            0.12, // Electronics
            0.39  // Services
        ];
    }
}
