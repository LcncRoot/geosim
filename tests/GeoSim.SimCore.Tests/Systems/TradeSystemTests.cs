using GeoSim.SimCore.Data;
using GeoSim.SimCore.Systems;

namespace GeoSim.SimCore.Tests.Systems;

public class TradeSystemTests
{
    [Fact]
    public void EqualPrices_TradeEqualsBase()
    {
        // Given: equal prices, no tariff
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 10.0;
        double tariffRate = 0.0;
        double sanctionSeverity = 0.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: trade equals base volume
        // priceRatio = 10 / 10 = 1.0
        // multiplier = 1.0^2 = 1.0
        Assert.Equal(100.0, flow, 1);
    }

    [Fact]
    public void HigherImporterPrice_TradeIncreases()
    {
        // Given: importer price higher than exporter
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 15.0;  // 50% higher
        double tariffRate = 0.0;
        double sanctionSeverity = 0.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: trade increases
        // priceRatio = 15 / 10 = 1.5
        // multiplier = 1.5^2 = 2.25
        // flow = 100 * 2.25 = 225
        Assert.True(flow > baseVolume, $"Flow {flow} should be > base {baseVolume}");
        Assert.Equal(225.0, flow, 1);
    }

    [Fact]
    public void LowerImporterPrice_TradeDecreases()
    {
        // Given: importer price lower than exporter
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 5.0;  // 50% lower
        double tariffRate = 0.0;
        double sanctionSeverity = 0.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: trade decreases
        // priceRatio = 5 / 10 = 0.5
        // multiplier = 0.5^2 = 0.25
        // flow = 100 * 0.25 = 25
        Assert.True(flow < baseVolume, $"Flow {flow} should be < base {baseVolume}");
        Assert.Equal(25.0, flow, 1);
    }

    [Fact]
    public void Tariff_ReducesTrade()
    {
        // Given: 20% tariff
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 10.0;
        double tariffRate = 0.20;
        double sanctionSeverity = 0.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: trade reduced
        // effectiveExporterPrice = 10 * 1.2 = 12
        // priceRatio = 10 / 12 = 0.833
        // multiplier = 0.833^2 = 0.694
        // flow = 100 * 0.694 = 69.4
        Assert.True(flow < baseVolume, $"Flow {flow} should be < base {baseVolume}");
        Assert.Equal(69.4, flow, 1);
    }

    [Fact]
    public void FullSanctions_ZeroTrade()
    {
        // Given: full sanctions (severity = 1.0)
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 10.0;
        double tariffRate = 0.0;
        double sanctionSeverity = 1.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: no trade
        Assert.Equal(0.0, flow);
    }

    [Fact]
    public void PartialSanctions_ReduceTrade()
    {
        // Given: 50% sanctions
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 10.0;
        double tariffRate = 0.0;
        double sanctionSeverity = 0.5;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: trade halved
        Assert.Equal(50.0, flow, 1);
    }

    [Fact]
    public void ZeroBaseVolume_ZeroTrade()
    {
        // Given: no baseline trade
        double baseVolume = 0.0;
        double exporterPrice = 10.0;
        double importerPrice = 20.0;
        double tariffRate = 0.0;
        double sanctionSeverity = 0.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: no trade
        Assert.Equal(0.0, flow);
    }

    [Fact]
    public void TradeElasticity_AffectsSensitivity()
    {
        // Given: same price difference, different elasticities
        double baseVolume = 100.0;
        double exporterPrice = 10.0;
        double importerPrice = 15.0;
        double tariffRate = 0.0;
        double sanctionSeverity = 0.0;

        // When: calculate with low elasticity
        double flowLow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, 1.0);

        // When: calculate with high elasticity
        double flowHigh = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, 3.0);

        // Then: higher elasticity = bigger response
        // Low: 1.5^1 = 1.5, flow = 150
        // High: 1.5^3 = 3.375, flow = 337.5
        Assert.Equal(150.0, flowLow, 1);
        Assert.Equal(337.5, flowHigh, 1);
    }

    [Fact]
    public void ImportPrice_IncludesTariff()
    {
        // Given: base price and tariff
        double basePrice = 100.0;
        double tariffRate = 0.25;

        // When: calculate import price
        double importPrice = TradeSystem.CalculateImportPrice(basePrice, tariffRate);

        // Then: price includes tariff
        Assert.Equal(125.0, importPrice);
    }

    [Fact]
    public void TariffRevenue_Calculated()
    {
        // Given: trade with tariff
        double tariffRate = 0.10;
        double importPrice = 100.0;
        double importVolume = 50.0;

        // When: calculate tariff revenue
        double revenue = TradeSystem.CalculateTariffRevenue(
            tariffRate, importPrice, importVolume);

        // Then: revenue = rate * price * volume
        Assert.Equal(500.0, revenue);
    }

    [Fact]
    public void ExtremeMultiplier_Clamped()
    {
        // Given: extreme price differential
        double baseVolume = 100.0;
        double exporterPrice = 1.0;
        double importerPrice = 100.0;  // 100x difference
        double tariffRate = 0.0;
        double sanctionSeverity = 0.0;
        double elasticity = 2.0;

        // When: calculate trade flow
        double flow = TradeSystem.CalculateTradeFlow(
            baseVolume, exporterPrice, importerPrice,
            tariffRate, sanctionSeverity, elasticity);

        // Then: multiplier clamped to max (10.0)
        Assert.Equal(1000.0, flow, 1);  // base * MaxMultiplier
    }

    [Fact]
    public void UpdateTradeRelation_UpdatesBalances()
    {
        // Given: two countries with trade relation
        var state = CreateTestState();

        var relation = state.TradeRelations[0];
        relation.BaseTradeVolume[(int)Commodity.Agriculture] = 100.0;
        state.Countries[0].Prices[(int)Commodity.Agriculture] = 10.0;
        state.Countries[1].Prices[(int)Commodity.Agriculture] = 10.0;

        // When: update trade relation
        TradeSystem.UpdateTradeRelation(state, relation);

        // Then: exporter gains, importer pays
        Assert.True(state.Countries[0].TradeBalance > 0);
        Assert.True(state.Countries[1].TradeBalance < 0);
    }

    [Fact]
    public void UpdateTradeRelation_CollectsTariffRevenue()
    {
        // Given: trade with tariff
        var state = CreateTestState();

        var relation = state.TradeRelations[0];
        relation.BaseTradeVolume[(int)Commodity.Agriculture] = 100.0;
        relation.TariffRates[(int)Commodity.Agriculture] = 0.10;
        state.Countries[0].Prices[(int)Commodity.Agriculture] = 10.0;
        state.Countries[1].Prices[(int)Commodity.Agriculture] = 10.0;

        long initialTax = state.Countries[1].TaxRevenue;

        // When: update trade
        TradeSystem.UpdateTradeRelation(state, relation);

        // Then: importer collects tariff revenue
        Assert.True(state.Countries[1].TaxRevenue > initialTax);
    }

    private static SimulationState CreateTestState()
    {
        var state = SimulationState.CreateEmpty();

        state.Countries =
        [
            new Country { Id = 0, Code = "EXP", Name = "Exporter" },
            new Country { Id = 1, Code = "IMP", Name = "Importer" }
        ];

        // Initialize prices
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            state.Countries[0].Prices[s] = 10.0;
            state.Countries[0].InitialPrices[s] = 10.0;
            state.Countries[1].Prices[s] = 10.0;
            state.Countries[1].InitialPrices[s] = 10.0;
        }

        state.TradeRelations =
        [
            new TradeRelation
            {
                FromCountryId = 0,
                ToCountryId = 1
            }
        ];

        return state;
    }
}
