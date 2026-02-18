using GeoSim.SimCore.Data;

namespace GeoSim.SimCore.Tests.Data;

public class DataModelTests
{
    [Fact]
    public void Commodity_HasCorrectValues()
    {
        Assert.Equal(0, (int)Commodity.Food);
        Assert.Equal(5, (int)Commodity.Electronics);
        Assert.Equal(9, (int)Commodity.Construction);
    }

    [Fact]
    public void TechnicalCoefficientMatrix_IndexerWorks()
    {
        var matrix = TechnicalCoefficientMatrix.CreateEmpty();

        matrix[Commodity.Energy, Commodity.Electronics] = 0.06;

        Assert.Equal(0.06, matrix[Commodity.Energy, Commodity.Electronics]);
        Assert.Equal(0.06, matrix[1, 5]);
    }

    [Fact]
    public void TechnicalCoefficientMatrix_CalculateInputCost()
    {
        var matrix = TechnicalCoefficientMatrix.CreateEmpty();
        matrix[Commodity.Energy, Commodity.Electronics] = 0.10;
        matrix[Commodity.Metals, Commodity.Electronics] = 0.15;

        Span<double> prices = stackalloc double[10];
        prices[(int)Commodity.Energy] = 100.0;
        prices[(int)Commodity.Metals] = 200.0;

        // Producing 10 units of Electronics:
        // Energy: 0.10 * 10 * 100 = 100
        // Metals: 0.15 * 10 * 200 = 300
        // Total: 400
        double cost = matrix.CalculateInputCost(Commodity.Electronics, 10.0, prices);

        Assert.Equal(400.0, cost);
    }

    [Fact]
    public void Country_UnemploymentRateCalculation()
    {
        var country = new Country { Code = "KOR", Name = "South Korea" };
        country.LaborForce = 1000;
        country.Employed = 950;

        Assert.Equal(0.05, country.UnemploymentRate, 4);
    }

    [Fact]
    public void Country_DebtToGdpCalculation()
    {
        var country = new Country { Code = "KOR", Name = "South Korea" };
        country.Gdp = 100_000_000; // $1M in cents
        country.Debt = 55_000_000;  // $550K in cents

        Assert.Equal(0.55, country.DebtToGdp, 4);
    }

    [Fact]
    public void Region_UnemploymentRateCalculation()
    {
        var region = new Region { Name = "Seoul" };
        region.LaborForce = 500;
        region.Employed = 475;

        Assert.Equal(0.05, region.UnemploymentRate, 4);
    }

    [Fact]
    public void SimulationState_YearAndWeekCalculation()
    {
        var state = SimulationState.CreateEmpty();
        state.Tick = 0;

        Assert.Equal(2024, state.Year);
        Assert.Equal(1, state.Week);

        state.Tick = 51;
        Assert.Equal(2024, state.Year);
        Assert.Equal(52, state.Week);

        state.Tick = 52;
        Assert.Equal(2025, state.Year);
        Assert.Equal(1, state.Week);
    }

    [Fact]
    public void Faction_RedLineTypes_Exist()
    {
        var faction = new Faction
        {
            Name = "Chaebol",
            RedLine = RedLineType.CorporateTaxAbove,
            RedLineThreshold = 0.30
        };

        Assert.Equal(RedLineType.CorporateTaxAbove, faction.RedLine);
        Assert.Equal(0.30, faction.RedLineThreshold);
    }
}
