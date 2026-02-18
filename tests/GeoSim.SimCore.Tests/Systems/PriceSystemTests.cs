using GeoSim.SimCore.Data;
using GeoSim.SimCore.Systems;

namespace GeoSim.SimCore.Tests.Systems;

public class PriceSystemTests
{
    [Fact]
    public void ExcessDemand_PriceRises()
    {
        // Given: demand > supply
        double currentPrice = 100.0;
        double demand = 150.0;
        double supply = 100.0;
        double sensitivity = 0.15;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: price rises
        // excessRatio = (150-100)/100 = 0.5
        // newPrice = 100 * (1 + 0.15 * 0.5) = 100 * 1.075 = 107.5
        Assert.True(newPrice > currentPrice, $"Price {newPrice} should be > {currentPrice}");
        Assert.Equal(107.5, newPrice, 1);
    }

    [Fact]
    public void ExcessSupply_PriceFalls()
    {
        // Given: supply > demand
        double currentPrice = 100.0;
        double demand = 80.0;
        double supply = 100.0;
        double sensitivity = 0.15;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: price falls
        // excessRatio = (80-100)/100 = -0.2
        // newPrice = 100 * (1 + 0.15 * -0.2) = 100 * 0.97 = 97
        Assert.True(newPrice < currentPrice, $"Price {newPrice} should be < {currentPrice}");
        Assert.Equal(97.0, newPrice, 1);
    }

    [Fact]
    public void BalancedSupplyDemand_PriceStable()
    {
        // Given: supply == demand
        double currentPrice = 100.0;
        double demand = 100.0;
        double supply = 100.0;
        double sensitivity = 0.15;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: price unchanged
        Assert.Equal(100.0, newPrice, 1);
    }

    [Fact]
    public void Clamping_PreventsExplosion()
    {
        // Given: extreme excess demand (10x supply)
        double currentPrice = 100.0;
        double demand = 1000.0;
        double supply = 100.0;
        double sensitivity = 0.20;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: clamped to max 50% excess ratio
        // excessRatio clamped to 0.5
        // newPrice = 100 * (1 + 0.20 * 0.5) = 100 * 1.10 = 110
        Assert.Equal(110.0, newPrice, 1);
    }

    [Fact]
    public void PriceBounds_PreventExtremes()
    {
        // Given: price trying to go below floor
        double currentPrice = 15.0;
        double demand = 10.0;
        double supply = 100.0;
        double sensitivity = 0.20;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: bounded to floor (0.1 * initial = 10)
        Assert.True(newPrice >= 10.0, $"Price {newPrice} should be >= floor 10");
    }

    [Fact]
    public void PriceCeiling_PreventsRunaway()
    {
        // Given: price at ceiling
        double currentPrice = 1000.0;
        double demand = 200.0;
        double supply = 100.0;
        double sensitivity = 0.20;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: bounded to ceiling (10 * initial = 1000)
        Assert.True(newPrice <= 1000.0, $"Price {newPrice} should be <= ceiling 1000");
        Assert.Equal(1000.0, newPrice, 1);
    }

    [Fact]
    public void Smoothing_DampensOscillation()
    {
        // Given: big price jump
        double newRawPrice = 150.0;
        double previousSmoothed = 100.0;

        // When: calculate smoothed price
        double smoothed = PriceSystem.CalculateSmoothedPrice(newRawPrice, previousSmoothed);

        // Then: smoothed is between old and new
        // smoothed = 0.7 * 150 + 0.3 * 100 = 105 + 30 = 135
        Assert.True(smoothed > previousSmoothed);
        Assert.True(smoothed < newRawPrice);
        Assert.Equal(135.0, smoothed, 1);
    }

    [Fact]
    public void CPI_ComputedCorrectly()
    {
        // Given: prices and weights (12 commodities)
        // Indices: Agriculture(0), RareEarths(1), Petroleum(2), Coal(3), Ore(4), Uranium(5),
        //          Electricity(6), ConsumerGoods(7), IndustrialGoods(8), MilitaryGoods(9),
        //          Electronics(10), Services(11)
        double[] currentPrices = [110, 100, 120, 100, 100, 100, 105, 115, 100, 100, 108, 112];
        double[] basePrices = [100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100];
        double[] weights = PriceSystem.GetDefaultConsumptionWeights();

        // When: calculate CPI
        double cpi = PriceSystem.CalculateCpi(currentPrices, basePrices, weights);

        // Then: weighted average of price ratios
        // Agriculture: 0.14 * 1.10 = 0.154
        // Petroleum: 0.07 * 1.20 = 0.084
        // Electricity: 0.08 * 1.05 = 0.084
        // ConsumerGoods: 0.20 * 1.15 = 0.23
        // Electronics: 0.12 * 1.08 = 0.1296
        // Services: 0.39 * 1.12 = 0.4368
        // Total weight = 1.0, sum = 1.1184
        Assert.True(cpi > 1.0, $"CPI {cpi} should be > 1.0 (inflation)");
        Assert.Equal(1.118, cpi, 2);
    }

    [Fact]
    public void InflationRate_CalculatedCorrectly()
    {
        // Given: CPI now vs year ago
        double currentCpi = 1.05;
        double yearAgoCpi = 1.00;

        // When: calculate inflation
        double inflation = PriceSystem.CalculateInflationRate(currentCpi, yearAgoCpi);

        // Then: 5% inflation
        Assert.Equal(0.05, inflation, 4);
    }

    [Fact]
    public void ZeroSupply_HandledGracefully()
    {
        // Given: zero supply
        double currentPrice = 100.0;
        double demand = 50.0;
        double supply = 0.0;
        double sensitivity = 0.15;
        double initialPrice = 100.0;

        // When: calculate new price
        double newPrice = PriceSystem.CalculateNewPrice(
            currentPrice, demand, supply, sensitivity, initialPrice);

        // Then: doesn't crash, price increases (capped)
        Assert.True(newPrice > currentPrice);
        Assert.False(double.IsNaN(newPrice));
        Assert.False(double.IsInfinity(newPrice));
    }

    [Fact]
    public void DefaultSensitivities_Reasonable()
    {
        var sensitivities = PriceSystem.GetDefaultSensitivities();

        Assert.Equal(12, sensitivities.Length);
        Assert.All(sensitivities, s => Assert.InRange(s, 0.01, 0.5));

        // Petroleum should be most volatile
        Assert.Equal(0.20, sensitivities[(int)Commodity.Petroleum]);

        // Services should be least volatile
        Assert.Equal(0.05, sensitivities[(int)Commodity.Services]);
    }
}
