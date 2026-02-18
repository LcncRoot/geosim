using GeoSim.SimCore.Data;
using GeoSim.SimCore.Systems;

namespace GeoSim.SimCore.Tests.Systems;

public class LaborSystemTests
{
    [Fact]
    public void LaborDemand_CalculatedFromCoefficient()
    {
        // Given: sector with labor coefficient
        var sector = new Sector
        {
            Id = 0,
            Commodity = Commodity.Agriculture,
            Capacity = 100.0,
            LaborCoefficient = 0.5  // 0.5 workers per unit output
        };

        // When: calculate labor demand
        double demand = LaborSystem.CalculateLaborDemand(ref sector);

        // Then: demand = coefficient × capacity
        // 0.5 × 100 = 50 workers
        Assert.Equal(50.0, demand);
    }

    [Fact]
    public void LaborDemand_ZeroCoefficient_ZeroDemand()
    {
        // Given: sector with zero labor coefficient
        var sector = new Sector
        {
            Id = 0,
            Commodity = Commodity.Electricity,
            Capacity = 100.0,
            LaborCoefficient = 0.0
        };

        // When: calculate labor demand
        double demand = LaborSystem.CalculateLaborDemand(ref sector);

        // Then: no demand
        Assert.Equal(0.0, demand);
    }

    [Fact]
    public void UnemploymentRate_CalculatedCorrectly()
    {
        // Given: labor force and employment
        long laborForce = 1000;
        long employed = 950;

        // When: calculate unemployment
        double rate = LaborSystem.CalculateUnemploymentRate(employed, laborForce);

        // Then: 5% unemployment
        Assert.Equal(0.05, rate, 4);
    }

    [Fact]
    public void UnemploymentRate_FullEmployment()
    {
        // Given: everyone employed
        long laborForce = 1000;
        long employed = 1000;

        // When: calculate unemployment
        double rate = LaborSystem.CalculateUnemploymentRate(employed, laborForce);

        // Then: 0% unemployment
        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void UnemploymentRate_ZeroLaborForce()
    {
        // Given: no labor force
        long laborForce = 0;
        long employed = 0;

        // When: calculate unemployment
        double rate = LaborSystem.CalculateUnemploymentRate(employed, laborForce);

        // Then: no unemployment (edge case)
        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void WageAdjustment_TightMarket_WagesRise()
    {
        // Given: demand exceeds supply
        double currentWage = 1000.0;
        double laborDemand = 150.0;
        double laborSupply = 100.0;
        double adjustmentSpeed = 0.02;

        // When: calculate new wage
        double newWage = LaborSystem.CalculateNewWage(
            currentWage, laborDemand, laborSupply, adjustmentSpeed);

        // Then: wages rise
        // tightness = 1.5, adjustment = 0.02 × 0.5 = 0.01
        // newWage = 1000 × 1.01 = 1010
        Assert.True(newWage > currentWage, $"Wage {newWage} should be > {currentWage}");
        Assert.Equal(1010.0, newWage, 1);
    }

    [Fact]
    public void WageAdjustment_SlackMarket_WagesFall()
    {
        // Given: supply exceeds demand
        double currentWage = 1000.0;
        double laborDemand = 80.0;
        double laborSupply = 100.0;
        double adjustmentSpeed = 0.02;

        // When: calculate new wage
        double newWage = LaborSystem.CalculateNewWage(
            currentWage, laborDemand, laborSupply, adjustmentSpeed);

        // Then: wages fall
        // tightness = 0.8, adjustment = 0.02 × -0.2 = -0.004
        // newWage = 1000 × 0.996 = 996
        Assert.True(newWage < currentWage, $"Wage {newWage} should be < {currentWage}");
        Assert.Equal(996.0, newWage, 1);
    }

    [Fact]
    public void WageAdjustment_BalancedMarket_WagesStable()
    {
        // Given: demand equals supply
        double currentWage = 1000.0;
        double laborDemand = 100.0;
        double laborSupply = 100.0;
        double adjustmentSpeed = 0.02;

        // When: calculate new wage
        double newWage = LaborSystem.CalculateNewWage(
            currentWage, laborDemand, laborSupply, adjustmentSpeed);

        // Then: wages unchanged
        Assert.Equal(1000.0, newWage, 1);
    }

    [Fact]
    public void WageFloor_Enforced()
    {
        // Given: very low wage trying to fall further
        double currentWage = 50.0;  // Below floor
        double laborDemand = 50.0;
        double laborSupply = 100.0;  // Slack market
        double adjustmentSpeed = 0.05;

        // When: calculate new wage
        double newWage = LaborSystem.CalculateNewWage(
            currentWage, laborDemand, laborSupply, adjustmentSpeed);

        // Then: wage at floor
        Assert.True(newWage >= LaborSystem.MinWageFloor);
    }

    [Fact]
    public void LaborAllocation_SufficientSupply_MeetsDemand()
    {
        // Given: region with enough workers
        var region = CreateTestRegion();
        region.LaborForce = 1000;

        // Two sectors with demand
        region.Sectors[0] = new Sector
        {
            Id = 0,
            Commodity = Commodity.Agriculture,
            Capacity = 100.0,
            LaborCoefficient = 2.0  // Demand = 200
        };
        region.Sectors[1] = new Sector
        {
            Id = 1,
            Commodity = Commodity.ConsumerGoods,
            Capacity = 100.0,
            LaborCoefficient = 3.0  // Demand = 300
        };

        // When: allocate labor
        LaborSystem.AllocateLaborToSectors(region, 1000, 500);

        // Then: full demand met
        Assert.Equal(200.0, region.Sectors[0].LaborEmployed, 1);
        Assert.Equal(300.0, region.Sectors[1].LaborEmployed, 1);
    }

    [Fact]
    public void LaborAllocation_InsufficientSupply_ProportionalAllocation()
    {
        // Given: region with shortage
        var region = CreateTestRegion();
        region.LaborForce = 250;  // Only half of demand

        // Two sectors with demand
        region.Sectors[0] = new Sector
        {
            Id = 0,
            Commodity = Commodity.Agriculture,
            Capacity = 100.0,
            LaborCoefficient = 2.0  // Demand = 200
        };
        region.Sectors[1] = new Sector
        {
            Id = 1,
            Commodity = Commodity.ConsumerGoods,
            Capacity = 100.0,
            LaborCoefficient = 3.0  // Demand = 300
        };

        // When: allocate labor (500 demand, 250 supply)
        LaborSystem.AllocateLaborToSectors(region, 250, 500);

        // Then: proportional allocation (50% of each)
        Assert.Equal(100.0, region.Sectors[0].LaborEmployed, 1);
        Assert.Equal(150.0, region.Sectors[1].LaborEmployed, 1);
    }

    [Fact]
    public void LaborProductivity_Calculated()
    {
        // Given: sector with output and employment
        var sector = new Sector
        {
            Output = 100.0,
            LaborEmployed = 20.0
        };

        // When: calculate productivity
        double productivity = LaborSystem.CalculateLaborProductivity(ref sector);

        // Then: 5 units per worker
        Assert.Equal(5.0, productivity);
    }

    [Fact]
    public void LaborShare_Calculated()
    {
        // Given: wages and value added
        double wages = 60.0;
        double valueAdded = 100.0;

        // When: calculate labor share
        double share = LaborSystem.CalculateLaborShare(wages, valueAdded);

        // Then: 60% labor share
        Assert.Equal(0.6, share);
    }

    [Fact]
    public void AverageWage_Calculated()
    {
        // Given: region with varying wages
        var region = CreateTestRegion();
        region.Sectors[0].LaborEmployed = 100;
        region.SectorWages[0] = 1000;
        region.Sectors[1].LaborEmployed = 100;
        region.SectorWages[1] = 2000;

        // When: calculate average wage
        double avgWage = LaborSystem.CalculateAverageWage(region);

        // Then: weighted average = 1500
        Assert.Equal(1500.0, avgWage);
    }

    [Fact]
    public void RegionWages_TotalCalculated()
    {
        // Given: region with employment and wages
        var region = CreateTestRegion();
        region.Sectors[0].LaborEmployed = 100;
        region.SectorWages[0] = 1000;
        region.Sectors[1].LaborEmployed = 50;
        region.SectorWages[1] = 2000;

        // When: calculate total wages
        long totalWages = LaborSystem.CalculateRegionWages(region);

        // Then: 100*1000 + 50*2000 = 200000
        Assert.Equal(200000, totalWages);
    }

    private static Region CreateTestRegion()
    {
        var region = new Region { Name = "TestRegion" };
        region.InfrastructureFactor = 1.0;

        // Initialize all sectors
        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            region.Sectors[s] = new Sector
            {
                Id = s,
                Commodity = (Commodity)s,
                Capacity = 0,
                LaborCoefficient = 0,
                Efficiency = 1.0
            };
            region.SectorWages[s] = 1000;
        }

        return region;
    }
}
