using GeoSim.SimCore.Data;
using GeoSim.SimCore.Systems;

namespace GeoSim.SimCore.Tests.Systems;

public class PoliticalSystemTests
{
    [Fact]
    public void FactionSatisfaction_LowTaxesIncreaseBusinessSatisfaction()
    {
        // Given: business faction that wants low taxes
        // Positive PrefCorporateTax means they like when EvaluateTaxPolicy returns positive
        // EvaluateTaxPolicy returns positive when tax < 20%
        var faction = new Faction
        {
            Name = "Business",
            BaseSatisfaction = 50,
            PrefCorporateTax = 0.5  // Positive = likes low taxes (positive utility)
        };

        // When: low corporate tax
        double satisfaction = PoliticalSystem.CalculateFactionSatisfaction(
            faction,
            corporateTaxRate: 0.15,  // Low tax (below 20% neutral)
            incomeTaxRate: 0.20,
            welfareSpending: 0.10,
            defenseSpending: 0.10,
            unemploymentRate: 0.05,
            corruption: 0.10);

        // Then: satisfaction above base
        Assert.True(satisfaction > 50, $"Satisfaction {satisfaction} should be > 50");
    }

    [Fact]
    public void FactionSatisfaction_HighTaxesDecreasesBusinessSatisfaction()
    {
        // Given: business faction that wants low taxes
        var faction = new Faction
        {
            Name = "Business",
            BaseSatisfaction = 50,
            PrefCorporateTax = 0.5  // Likes low taxes
        };

        // When: high corporate tax
        double satisfaction = PoliticalSystem.CalculateFactionSatisfaction(
            faction,
            corporateTaxRate: 0.35,  // High tax (above 20% neutral)
            incomeTaxRate: 0.20,
            welfareSpending: 0.10,
            defenseSpending: 0.10,
            unemploymentRate: 0.05,
            corruption: 0.10);

        // Then: satisfaction below base (high tax = negative utility × positive preference)
        Assert.True(satisfaction < 50, $"Satisfaction {satisfaction} should be < 50");
    }

    [Fact]
    public void FactionSatisfaction_HighWelfareIncreasesLaborSatisfaction()
    {
        // Given: labor faction that wants welfare
        var faction = new Faction
        {
            Name = "Labor",
            BaseSatisfaction = 50,
            PrefWelfareSpending = 0.5  // Wants more welfare
        };

        // When: high welfare spending
        double satisfaction = PoliticalSystem.CalculateFactionSatisfaction(
            faction,
            corporateTaxRate: 0.20,
            incomeTaxRate: 0.20,
            welfareSpending: 0.40,  // High welfare
            defenseSpending: 0.10,
            unemploymentRate: 0.05,
            corruption: 0.10);

        // Then: satisfaction above base
        Assert.True(satisfaction > 50, $"Satisfaction {satisfaction} should be > 50");
    }

    [Fact]
    public void FactionSatisfaction_HighUnemploymentDecreasesLaborSatisfaction()
    {
        // Given: labor faction that cares about unemployment
        var faction = new Faction
        {
            Name = "Labor",
            BaseSatisfaction = 50,
            PrefLowUnemployment = 0.5
        };

        // When: high unemployment
        double satisfaction = PoliticalSystem.CalculateFactionSatisfaction(
            faction,
            corporateTaxRate: 0.20,
            incomeTaxRate: 0.20,
            welfareSpending: 0.20,
            defenseSpending: 0.10,
            unemploymentRate: 0.12,  // High unemployment
            corruption: 0.10);

        // Then: satisfaction below base
        Assert.True(satisfaction < 50, $"Satisfaction {satisfaction} should be < 50");
    }

    [Fact]
    public void Legitimacy_ConvergesToSatisfaction()
    {
        // Given: legitimacy below satisfaction
        double currentLegitimacy = 40.0;
        double avgSatisfaction = 70.0;
        double speed = 0.1;

        // When: update legitimacy
        double newLegitimacy = PoliticalSystem.CalculateNewLegitimacy(
            currentLegitimacy, avgSatisfaction, speed);

        // Then: legitimacy moves toward satisfaction
        // L' = 40 + 0.1 × (70 - 40) = 40 + 3 = 43
        Assert.True(newLegitimacy > currentLegitimacy);
        Assert.Equal(43.0, newLegitimacy, 1);
    }

    [Fact]
    public void Legitimacy_DecreasesWhenSatisfactionLow()
    {
        // Given: legitimacy above satisfaction
        double currentLegitimacy = 70.0;
        double avgSatisfaction = 40.0;
        double speed = 0.1;

        // When: update legitimacy
        double newLegitimacy = PoliticalSystem.CalculateNewLegitimacy(
            currentLegitimacy, avgSatisfaction, speed);

        // Then: legitimacy decreases
        // L' = 70 + 0.1 × (40 - 70) = 70 - 3 = 67
        Assert.True(newLegitimacy < currentLegitimacy);
        Assert.Equal(67.0, newLegitimacy, 1);
    }

    [Fact]
    public void Legitimacy_ClampedToRange()
    {
        // Given: extreme values
        double currentLegitimacy = 5.0;
        double avgSatisfaction = -50.0;  // Invalid but test clamping
        double speed = 0.2;

        // When: update legitimacy
        double newLegitimacy = PoliticalSystem.CalculateNewLegitimacy(
            currentLegitimacy, avgSatisfaction, speed);

        // Then: clamped to [0, 100]
        Assert.True(newLegitimacy >= 0);
        Assert.True(newLegitimacy <= 100);
    }

    [Fact]
    public void RedLine_CorporateTaxViolation()
    {
        // Given: faction with corporate tax red line
        var faction = new Faction
        {
            Name = "Chaebol",
            RedLine = RedLineType.CorporateTaxAbove,
            RedLineThreshold = 0.25
        };

        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            CorporateTaxRate = 0.30  // Above threshold
        };

        var state = SimulationState.CreateEmpty();

        // When: check violation
        bool violated = PoliticalSystem.IsRedLineViolated(faction, country, state);

        // Then: red line violated
        Assert.True(violated);
    }

    [Fact]
    public void RedLine_UnemploymentViolation()
    {
        // Given: faction with unemployment red line
        var faction = new Faction
        {
            Name = "Labor",
            RedLine = RedLineType.UnemploymentAbove,
            RedLineThreshold = 0.08
        };

        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            LaborForce = 1000,
            Employed = 850  // 15% unemployment
        };

        var state = SimulationState.CreateEmpty();

        // When: check violation
        bool violated = PoliticalSystem.IsRedLineViolated(faction, country, state);

        // Then: red line violated
        Assert.True(violated);
    }

    [Fact]
    public void RedLine_NoViolationWhenBelowThreshold()
    {
        // Given: faction with corporate tax red line
        var faction = new Faction
        {
            Name = "Chaebol",
            RedLine = RedLineType.CorporateTaxAbove,
            RedLineThreshold = 0.25
        };

        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            CorporateTaxRate = 0.20  // Below threshold
        };

        var state = SimulationState.CreateEmpty();

        // When: check violation
        bool violated = PoliticalSystem.IsRedLineViolated(faction, country, state);

        // Then: no violation
        Assert.False(violated);
    }

    [Fact]
    public void FactionPower_IncreasesWithHighSatisfaction()
    {
        // Given: faction with above-average satisfaction
        double currentPower = 0.30;
        double factionSatisfaction = 70.0;
        double avgSatisfaction = 50.0;

        // When: calculate new power
        double newPower = PoliticalSystem.CalculateNewFactionPower(
            currentPower, factionSatisfaction, avgSatisfaction);

        // Then: power increases
        Assert.True(newPower > currentPower);
    }

    [Fact]
    public void FactionPower_DecreasesWithLowSatisfaction()
    {
        // Given: faction with below-average satisfaction
        double currentPower = 0.30;
        double factionSatisfaction = 30.0;
        double avgSatisfaction = 50.0;

        // When: calculate new power
        double newPower = PoliticalSystem.CalculateNewFactionPower(
            currentPower, factionSatisfaction, avgSatisfaction);

        // Then: power decreases
        Assert.True(newPower < currentPower);
    }

    [Fact]
    public void FactionPower_MinimumFloor()
    {
        // Given: very low power and satisfaction
        double currentPower = 0.02;
        double factionSatisfaction = 10.0;
        double avgSatisfaction = 70.0;

        // When: calculate new power
        double newPower = PoliticalSystem.CalculateNewFactionPower(
            currentPower, factionSatisfaction, avgSatisfaction);

        // Then: power doesn't go below minimum
        Assert.True(newPower >= 0.01);
    }

    [Fact]
    public void WeightedSatisfaction_CalculatedCorrectly()
    {
        // Given: factions with different power and satisfaction
        var state = CreateTestState();

        state.Factions =
        [
            new Faction
            {
                Id = 0,
                CountryId = 0,
                Name = "Business",
                Power = 0.6,
                Satisfaction = 80
            },
            new Faction
            {
                Id = 1,
                CountryId = 0,
                Name = "Labor",
                Power = 0.4,
                Satisfaction = 40
            }
        ];

        // When: calculate weighted satisfaction
        double weighted = PoliticalSystem.CalculateWeightedSatisfaction(state, 0);

        // Then: power-weighted average
        // (0.6 × 80 + 0.4 × 40) / (0.6 + 0.4) = (48 + 16) / 1 = 64
        Assert.Equal(64.0, weighted, 1);
    }

    [Fact]
    public void RegionalUnrest_IncreasesWithUnemployment()
    {
        // Given: high unemployment
        double unrest = PoliticalSystem.CalculateRegionalUnrest(
            unemploymentRate: 0.15,
            foodInsecurity: 0.0,
            inequality: 0.0,
            corruption: 0.0);

        // Then: unrest from unemployment
        Assert.True(unrest > 0);
        Assert.Equal(15.0, unrest, 1);
    }

    [Fact]
    public void RegionalUnrest_IncreasesWithFoodInsecurity()
    {
        // Given: high food insecurity
        double unrest = PoliticalSystem.CalculateRegionalUnrest(
            unemploymentRate: 0.0,
            foodInsecurity: 0.5,
            inequality: 0.0,
            corruption: 0.0);

        // Then: significant unrest
        Assert.True(unrest > 50);
    }

    [Fact]
    public void StabilityScore_HighWhenStable()
    {
        // Given: stable country
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            Legitimacy = 80,
            AverageUnrest = 20
        };

        // When: calculate stability
        double stability = PoliticalSystem.CalculateStabilityScore(country);

        // Then: high stability
        // 0.6 × 80 + 0.4 × (100-20) = 48 + 32 = 80
        Assert.Equal(80.0, stability);
    }

    [Fact]
    public void GovernmentAtRisk_LowLegitimacy()
    {
        // Given: country with low legitimacy
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            Legitimacy = 25,
            AverageUnrest = 30
        };

        // When: check risk
        bool atRisk = PoliticalSystem.IsGovernmentAtRisk(country);

        // Then: at risk
        Assert.True(atRisk);
    }

    [Fact]
    public void GovernmentAtRisk_HighUnrest()
    {
        // Given: country with high unrest
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            Legitimacy = 50,
            AverageUnrest = 80
        };

        // When: check risk
        bool atRisk = PoliticalSystem.IsGovernmentAtRisk(country);

        // Then: at risk
        Assert.True(atRisk);
    }

    [Fact]
    public void GovernmentNotAtRisk_Stable()
    {
        // Given: stable country
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            Legitimacy = 60,
            AverageUnrest = 30
        };

        // When: check risk
        bool atRisk = PoliticalSystem.IsGovernmentAtRisk(country);

        // Then: not at risk
        Assert.False(atRisk);
    }

    private static SimulationState CreateTestState()
    {
        var state = SimulationState.CreateEmpty();
        state.Countries =
        [
            new Country { Id = 0, Code = "TST", Name = "Test" }
        ];
        return state;
    }
}
