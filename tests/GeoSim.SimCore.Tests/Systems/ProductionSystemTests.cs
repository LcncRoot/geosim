using GeoSim.SimCore.Data;
using GeoSim.SimCore.Systems;

namespace GeoSim.SimCore.Tests.Systems;

public class ProductionSystemTests
{
    private static TechnicalCoefficientMatrix CreateTestMatrix()
    {
        var matrix = TechnicalCoefficientMatrix.CreateEmpty();
        // Electronics requires Petroleum and Ore
        matrix[Commodity.Petroleum, Commodity.Electronics] = 0.10;
        matrix[Commodity.Ore, Commodity.Electronics] = 0.15;
        return matrix;
    }

    private static Region CreateTestRegion()
    {
        var region = new Region { Name = "TestRegion" };
        region.InfrastructureFactor = 1.0;

        // Set up Electronics sector
        region.Sectors[(int)Commodity.Electronics] = new Sector
        {
            Id = (int)Commodity.Electronics,
            Commodity = Commodity.Electronics,
            Capacity = 100.0,
            LaborEmployed = 50.0,
            LaborCoefficient = 0.5, // 0.5 workers per unit
            Efficiency = 1.0,
            Price = 10.0
        };

        return region;
    }

    [Fact]
    public void FullInputAvailability_OutputEqualsCapacity()
    {
        // Given: full inputs available
        var region = CreateTestRegion();
        region.Inventory[(int)Commodity.Petroleum] = 1000.0;
        region.Inventory[(int)Commodity.Ore] = 1000.0;

        var matrix = CreateTestMatrix();
        ref var sector = ref region.Sectors[(int)Commodity.Electronics];

        // Labor coefficient: need 50 workers for 100 output (0.5 per unit)
        double laborCoeff = 0.5;

        var country = new Country { Code = "TST", Name = "Test" };

        // When: calculate output
        double output = ProductionSystem.CalculateSectorOutput(
            ref sector, region, country, matrix, laborCoeff);

        // Then: output equals capacity (100)
        Assert.Equal(100.0, output, 1);
    }

    [Fact]
    public void OneInputAt50Percent_OutputReducedButNotZero()
    {
        // Given: Energy at 50%, Metals at 100%
        var region = CreateTestRegion();
        region.Inventory[(int)Commodity.Petroleum] = 5.0;   // Need 10 for 100 output
        region.Inventory[(int)Commodity.Ore] = 1000.0;

        var matrix = CreateTestMatrix();
        ref var sector = ref region.Sectors[(int)Commodity.Electronics];
        double laborCoeff = 0.5;

        var country = new Country { Code = "TST", Name = "Test" };

        // When: calculate output
        double output = ProductionSystem.CalculateSectorOutput(
            ref sector, region, country, matrix, laborCoeff);

        // Then: output is reduced but > 50% of capacity
        // Soft Leontief: α=0.6, bottleneck=0.5, avg=(0.5+1.0)/2=0.75
        // effective = 0.6*0.5 + 0.4*0.75 = 0.3 + 0.3 = 0.6
        // So output should be around 60
        Assert.True(output > 50.0, $"Output {output} should be > 50");
        Assert.True(output < 100.0, $"Output {output} should be < 100");
        Assert.Equal(60.0, output, 1);
    }

    [Fact]
    public void ZeroEnergy_OutputSeverelyReduced()
    {
        // Given: zero energy
        var region = CreateTestRegion();
        region.Inventory[(int)Commodity.Petroleum] = 0.0;
        region.Inventory[(int)Commodity.Ore] = 1000.0;

        var matrix = CreateTestMatrix();
        ref var sector = ref region.Sectors[(int)Commodity.Electronics];
        double laborCoeff = 0.5;

        var country = new Country { Code = "TST", Name = "Test" };

        // When: calculate output
        double output = ProductionSystem.CalculateSectorOutput(
            ref sector, region, country, matrix, laborCoeff);

        // Then: output is severely reduced (bottleneck = 0)
        // Soft Leontief: α=0.6, bottleneck=0, avg=(0+1)/2=0.5
        // effective = 0.6*0 + 0.4*0.5 = 0.2
        // So output should be around 20
        Assert.True(output < 30.0, $"Output {output} should be < 30");
        Assert.Equal(20.0, output, 1);
    }

    [Fact]
    public void NoLabor_OutputIsZero()
    {
        // Given: no workers
        var region = CreateTestRegion();
        region.Inventory[(int)Commodity.Petroleum] = 1000.0;
        region.Inventory[(int)Commodity.Ore] = 1000.0;

        var matrix = CreateTestMatrix();
        ref var sector = ref region.Sectors[(int)Commodity.Electronics];
        sector.LaborEmployed = 0;
        double laborCoeff = 0.5;

        var country = new Country { Code = "TST", Name = "Test" };

        // When: calculate output
        double output = ProductionSystem.CalculateSectorOutput(
            ref sector, region, country, matrix, laborCoeff);

        // Then: output is zero (labor constraint)
        Assert.Equal(0.0, output);
    }

    [Fact]
    public void InfrastructureModifier_ScalesOutput()
    {
        // Given: infrastructure at 0.5
        var region = CreateTestRegion();
        region.InfrastructureFactor = 0.5;
        region.Inventory[(int)Commodity.Petroleum] = 1000.0;
        region.Inventory[(int)Commodity.Ore] = 1000.0;

        var matrix = CreateTestMatrix();
        ref var sector = ref region.Sectors[(int)Commodity.Electronics];
        double laborCoeff = 0.5;

        var country = new Country { Code = "TST", Name = "Test" };

        // When: calculate output
        double output = ProductionSystem.CalculateSectorOutput(
            ref sector, region, country, matrix, laborCoeff);

        // Then: output is halved
        Assert.Equal(50.0, output, 1);
    }

    [Fact]
    public void ValueAdded_ComputedCorrectly()
    {
        // Given: output and prices
        double output = 100.0;
        double[] prices = new double[CommodityConstants.Count];
        prices[(int)Commodity.Electronics] = 10.0;  // Output price
        prices[(int)Commodity.Petroleum] = 5.0;        // Input price
        prices[(int)Commodity.Ore] = 8.0;        // Input price

        var matrix = CreateTestMatrix();

        // When: calculate value added
        double valueAdded = ProductionSystem.CalculateValueAdded(
            output, prices, matrix, (int)Commodity.Electronics);

        // Then: VA = revenue - input costs
        // Revenue = 100 * 10 = 1000
        // Energy cost = 0.10 * 100 * 5 = 50
        // Metals cost = 0.15 * 100 * 8 = 120
        // VA = 1000 - 50 - 120 = 830
        Assert.Equal(830.0, valueAdded, 1);
    }

    [Fact]
    public void SoftLeontief_NoInputsRequired_ReturnsTargetOutput()
    {
        // Given: sector with no input requirements
        var matrix = TechnicalCoefficientMatrix.CreateEmpty();
        double[] inventory = new double[CommodityConstants.Count];

        // When: calculate soft Leontief
        double result = ProductionSystem.CalculateSoftLeontief(
            (int)Commodity.Services, 100.0, inventory, matrix);

        // Then: returns target output (no constraint)
        Assert.Equal(100.0, result);
    }

    [Fact]
    public void ExtractionOutput_CalculatesCorrectly()
    {
        // Given: a facility on a deposit
        var deposit = new ResourceDeposit
        {
            Id = 0,
            Subtype = "coal",
            ResourceType = Commodity.Coal,
            TotalReserves = 10000,
            Remaining = 10000,
            BaseYield = 10.0,
            Difficulty = 1.0,
            DiscoveryState = DiscoveryState.Proven
        };

        var facility = new ExtractionFacility
        {
            Id = 0,
            FacilityType = "coal_mine",
            Level = 2,
            Condition = 1.0,
            Workers = 100,
            WorkersRequired = 100
        };

        var region = new Region { Name = "Mining Region" };
        region.InfrastructureFactor = 1.0;

        // When: calculate extraction
        double output = ProductionSystem.CalculateExtractionOutput(
            facility, deposit, region, 1.0);

        // Then: output = baseYield * level * workforce * condition * infra * tech
        // = 10 * 2 * 1.0 * 1.0 * 1.0 * 1.0 = 20
        Assert.Equal(20.0, output);
    }

    [Fact]
    public void ExtractionOutput_DegradedCondition_ReducesOutput()
    {
        // Given: facility at 25% condition
        var deposit = new ResourceDeposit
        {
            Id = 0,
            Subtype = "coal",
            ResourceType = Commodity.Coal,
            TotalReserves = 10000,
            Remaining = 10000,
            BaseYield = 10.0,
            Difficulty = 1.0,
            DiscoveryState = DiscoveryState.Proven
        };

        var facility = new ExtractionFacility
        {
            Id = 0,
            FacilityType = "coal_mine",
            Level = 2,
            Condition = 0.25,  // 25% condition
            Workers = 100,
            WorkersRequired = 100
        };

        var region = new Region { Name = "Mining Region" };
        region.InfrastructureFactor = 1.0;

        // When: calculate extraction
        double output = ProductionSystem.CalculateExtractionOutput(
            facility, deposit, region, 1.0);

        // Then: condition factor = sqrt(0.25) = 0.5
        // output = 10 * 2 * 1.0 * 0.5 * 1.0 * 1.0 = 10
        Assert.Equal(10.0, output);
    }

    [Fact]
    public void ExtractionOutput_ExhaustedDeposit_ReturnsZero()
    {
        // Given: exhausted deposit
        var deposit = new ResourceDeposit
        {
            Id = 0,
            Subtype = "coal",
            ResourceType = Commodity.Coal,
            TotalReserves = 10000,
            Remaining = 0,  // Exhausted
            BaseYield = 10.0,
            Difficulty = 1.0,
            DiscoveryState = DiscoveryState.Proven
        };

        var facility = new ExtractionFacility
        {
            Id = 0,
            FacilityType = "coal_mine",
            Level = 2,
            Condition = 1.0,
            Workers = 100,
            WorkersRequired = 100
        };

        var region = new Region { Name = "Mining Region" };
        region.InfrastructureFactor = 1.0;

        // When: calculate extraction
        double output = ProductionSystem.CalculateExtractionOutput(
            facility, deposit, region, 1.0);

        // Then: output is zero
        Assert.Equal(0.0, output);
    }
}
