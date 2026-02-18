namespace GeoSim.SimCore.Systems;

using GeoSim.SimCore.Data;

/// <summary>
/// Stateless political system implementing faction satisfaction and legitimacy.
/// From equations.md §7.
/// </summary>
public static class PoliticalSystem
{
    /// <summary>Default legitimacy adjustment speed λ.</summary>
    public const double DefaultLegitimacySpeed = 0.1;

    /// <summary>Minimum legitimacy adjustment speed.</summary>
    public const double MinLegitimacySpeed = 0.05;

    /// <summary>Maximum legitimacy adjustment speed.</summary>
    public const double MaxLegitimacySpeed = 0.2;

    /// <summary>Power adjustment rate μ for faction dynamics.</summary>
    public const double PowerAdjustmentRate = 0.02;

    /// <summary>Legitimacy drop for minor red line violation.</summary>
    public const double MinorRedLineLegitimacyDrop = 10.0;

    /// <summary>Legitimacy drop for major red line violation.</summary>
    public const double MajorRedLineLegitimacyDrop = 20.0;

    /// <summary>Power threshold for major faction.</summary>
    public const double MajorFactionThreshold = 0.3;

    /// <summary>Power threshold for dominant faction.</summary>
    public const double DominantFactionThreshold = 0.5;

    /// <summary>
    /// Update political state for a country (runs monthly = every 4 ticks).
    /// </summary>
    public static void UpdateCountry(SimulationState state, int countryId)
    {
        var country = state.GetCountry(countryId);

        // Update faction satisfaction
        foreach (var faction in state.GetFactionsForCountry(countryId))
        {
            UpdateFactionSatisfaction(faction, country);
            CheckRedLineViolation(faction, country, state);
        }

        // Calculate weighted average satisfaction
        double avgSatisfaction = CalculateWeightedSatisfaction(state, countryId);

        // Update legitimacy
        UpdateLegitimacy(country, avgSatisfaction, DefaultLegitimacySpeed);

        // Update faction power dynamics
        UpdateFactionPower(state, countryId, avgSatisfaction);

        // Update average unrest across regions
        UpdateAverageUnrest(state, country);
    }

    /// <summary>
    /// Calculate faction satisfaction based on policy preferences.
    /// S_f = Σ ω_k × u_k(x_k)
    /// From equations.md §7.1.
    /// </summary>
    public static void UpdateFactionSatisfaction(Faction faction, Country country)
    {
        double satisfaction = faction.BaseSatisfaction;

        // Corporate tax: negative preference means wants lower taxes
        satisfaction += faction.PrefCorporateTax * EvaluateTaxPolicy(country.CorporateTaxRate);

        // Income tax
        satisfaction += faction.PrefIncomeTax * EvaluateTaxPolicy(country.IncomeTaxRate);

        // Welfare spending
        satisfaction += faction.PrefWelfareSpending * EvaluateSpendingPolicy(country.WelfareSpendingShare);

        // Military spending
        satisfaction += faction.PrefMilitarySpending * EvaluateSpendingPolicy(country.DefenseSpendingShare);

        // Low unemployment (everyone likes low unemployment, but some care more)
        satisfaction += faction.PrefLowUnemployment * EvaluateUnemployment(country.UnemploymentRate);

        // Low corruption
        satisfaction += faction.PrefLowCorruption * EvaluateCorruption(country.Corruption);

        // Clamp to valid range
        faction.Satisfaction = Math.Clamp(satisfaction, 0, 100);
    }

    /// <summary>
    /// Calculate faction satisfaction from explicit policy values.
    /// </summary>
    public static double CalculateFactionSatisfaction(
        Faction faction,
        double corporateTaxRate,
        double incomeTaxRate,
        double welfareSpending,
        double defenseSpending,
        double unemploymentRate,
        double corruption)
    {
        double satisfaction = faction.BaseSatisfaction;

        satisfaction += faction.PrefCorporateTax * EvaluateTaxPolicy(corporateTaxRate);
        satisfaction += faction.PrefIncomeTax * EvaluateTaxPolicy(incomeTaxRate);
        satisfaction += faction.PrefWelfareSpending * EvaluateSpendingPolicy(welfareSpending);
        satisfaction += faction.PrefMilitarySpending * EvaluateSpendingPolicy(defenseSpending);
        satisfaction += faction.PrefLowUnemployment * EvaluateUnemployment(unemploymentRate);
        satisfaction += faction.PrefLowCorruption * EvaluateCorruption(corruption);

        return Math.Clamp(satisfaction, 0, 100);
    }

    /// <summary>
    /// Evaluate tax policy utility (negative = lower is better).
    /// Returns value in [-20, 20] range.
    /// </summary>
    public static double EvaluateTaxPolicy(double taxRate)
    {
        // Higher tax = negative utility for those who want low taxes
        // Centered around 20% as neutral
        return (0.20 - taxRate) * 100;
    }

    /// <summary>
    /// Evaluate spending policy utility.
    /// Returns value in [-20, 20] range.
    /// </summary>
    public static double EvaluateSpendingPolicy(double spendingShare)
    {
        // Higher spending = positive utility
        return (spendingShare - 0.10) * 100;
    }

    /// <summary>
    /// Evaluate unemployment utility (lower is better).
    /// Returns value in [-30, 30] range.
    /// </summary>
    public static double EvaluateUnemployment(double unemploymentRate)
    {
        // 5% is neutral, lower is better
        return (0.05 - unemploymentRate) * 200;
    }

    /// <summary>
    /// Evaluate corruption utility (lower is better).
    /// Returns value in [-30, 30] range.
    /// </summary>
    public static double EvaluateCorruption(double corruption)
    {
        // 0.2 is neutral, lower is better
        return (0.2 - corruption) * 100;
    }

    /// <summary>
    /// Check and apply red line violations.
    /// From equations.md §7.3.
    /// </summary>
    public static void CheckRedLineViolation(Faction faction, Country country, SimulationState state)
    {
        bool violated = IsRedLineViolated(faction, country, state);

        if (violated && !faction.RedLineViolated)
        {
            // New violation - apply penalty
            faction.RedLineViolated = true;
            faction.Satisfaction -= faction.RedLinePenalty;
            faction.Satisfaction = Math.Max(0, faction.Satisfaction);

            // Apply legitimacy drop based on faction power
            if (faction.Power >= DominantFactionThreshold)
            {
                country.Legitimacy -= MajorRedLineLegitimacyDrop;
            }
            else if (faction.Power >= MajorFactionThreshold)
            {
                country.Legitimacy -= MinorRedLineLegitimacyDrop;
            }

            country.Legitimacy = Math.Max(0, country.Legitimacy);
        }
        else if (!violated && faction.RedLineViolated)
        {
            // Violation resolved
            faction.RedLineViolated = false;
        }
    }

    /// <summary>
    /// Check if a faction's red line is violated.
    /// </summary>
    public static bool IsRedLineViolated(Faction faction, Country country, SimulationState state)
    {
        return faction.RedLine switch
        {
            RedLineType.None => false,
            RedLineType.CorporateTaxAbove => country.CorporateTaxRate > faction.RedLineThreshold,
            RedLineType.UnemploymentAbove => country.UnemploymentRate > faction.RedLineThreshold,
            RedLineType.DefenseSpendingBelow => country.DefenseSpendingShare < faction.RedLineThreshold,
            RedLineType.CorruptionAbove => country.Corruption > faction.RedLineThreshold,
            RedLineType.FoodImportsAbove => CalculateFoodImportRatio(state, country.Id) > faction.RedLineThreshold,
            RedLineType.DefenseBudgetCutAbove => false, // Would need previous budget tracking
            _ => false
        };
    }

    /// <summary>
    /// Calculate power-weighted average faction satisfaction.
    /// S̄ = Σ ρ_f × S_f
    /// </summary>
    public static double CalculateWeightedSatisfaction(SimulationState state, int countryId)
    {
        double weightedSum = 0;
        double totalPower = 0;

        foreach (var faction in state.GetFactionsForCountry(countryId))
        {
            weightedSum += faction.Power * faction.Satisfaction;
            totalPower += faction.Power;
        }

        return totalPower > 0 ? weightedSum / totalPower : 50.0;
    }

    /// <summary>
    /// Update government legitimacy.
    /// L_t+1 = L_t + λ × (S̄ - L_t)
    /// From equations.md §7.2.
    /// </summary>
    public static void UpdateLegitimacy(Country country, double avgSatisfaction, double speed)
    {
        speed = Math.Clamp(speed, MinLegitimacySpeed, MaxLegitimacySpeed);

        double newLegitimacy = country.Legitimacy + speed * (avgSatisfaction - country.Legitimacy);
        country.Legitimacy = Math.Clamp(newLegitimacy, 0, 100);
    }

    /// <summary>
    /// Calculate new legitimacy value.
    /// </summary>
    public static double CalculateNewLegitimacy(
        double currentLegitimacy,
        double avgSatisfaction,
        double speed)
    {
        speed = Math.Clamp(speed, MinLegitimacySpeed, MaxLegitimacySpeed);
        double newLegitimacy = currentLegitimacy + speed * (avgSatisfaction - currentLegitimacy);
        return Math.Clamp(newLegitimacy, 0, 100);
    }

    /// <summary>
    /// Apply a shock to legitimacy (scandal, crisis, etc.).
    /// </summary>
    public static void ApplyLegitimacyShock(Country country, double shock)
    {
        country.Legitimacy = Math.Clamp(country.Legitimacy - shock, 0, 100);
    }

    /// <summary>
    /// Update faction power based on relative satisfaction.
    /// ρ_f,t+1 = ρ_f,t + μ × ρ_f,t × (S_f - S̄)
    /// From equations.md §7.4.
    /// </summary>
    public static void UpdateFactionPower(SimulationState state, int countryId, double avgSatisfaction)
    {
        var factions = state.GetFactionsForCountry(countryId).ToList();
        if (factions.Count == 0) return;

        // Calculate power changes
        double totalNewPower = 0;
        var newPowers = new double[factions.Count];

        for (int i = 0; i < factions.Count; i++)
        {
            var faction = factions[i];
            double satisfactionDiff = faction.Satisfaction - avgSatisfaction;
            double powerChange = PowerAdjustmentRate * faction.Power * satisfactionDiff / 100.0;
            newPowers[i] = Math.Max(0.01, faction.Power + powerChange);
            totalNewPower += newPowers[i];
        }

        // Normalize to sum to 1
        for (int i = 0; i < factions.Count; i++)
        {
            factions[i].Power = newPowers[i] / totalNewPower;
        }
    }

    /// <summary>
    /// Calculate new faction power.
    /// </summary>
    public static double CalculateNewFactionPower(
        double currentPower,
        double factionSatisfaction,
        double avgSatisfaction)
    {
        double satisfactionDiff = factionSatisfaction - avgSatisfaction;
        double powerChange = PowerAdjustmentRate * currentPower * satisfactionDiff / 100.0;
        return Math.Max(0.01, currentPower + powerChange);
    }

    /// <summary>
    /// Update average unrest across regions.
    /// </summary>
    public static void UpdateAverageUnrest(SimulationState state, Country country)
    {
        double totalUnrest = 0;
        int regionCount = 0;

        foreach (var region in state.GetRegionsForCountry(country.Id))
        {
            totalUnrest += region.Unrest;
            regionCount++;
        }

        country.AverageUnrest = regionCount > 0 ? totalUnrest / regionCount : 0;
    }

    /// <summary>
    /// Calculate regional unrest based on economic factors.
    /// </summary>
    public static double CalculateRegionalUnrest(
        double unemploymentRate,
        double foodInsecurity,
        double inequality,
        double corruption)
    {
        // Base unrest from economic factors
        double unrest = 0;

        // Unemployment contributes to unrest
        unrest += unemploymentRate * 100;

        // Food insecurity is a major driver
        unrest += foodInsecurity * 150;

        // Inequality breeds discontent
        unrest += inequality * 50;

        // Corruption erodes trust
        unrest += corruption * 30;

        return Math.Clamp(unrest, 0, 100);
    }

    /// <summary>
    /// Check if government is at risk of collapse.
    /// </summary>
    public static bool IsGovernmentAtRisk(Country country)
    {
        // Low legitimacy + high unrest = instability
        return country.Legitimacy < 30 || country.AverageUnrest > 70;
    }

    /// <summary>
    /// Calculate government stability score.
    /// </summary>
    public static double CalculateStabilityScore(Country country)
    {
        // Weighted combination of legitimacy and inverse unrest
        double stabilityFromLegitimacy = country.Legitimacy;
        double stabilityFromUnrest = 100 - country.AverageUnrest;

        return (stabilityFromLegitimacy * 0.6 + stabilityFromUnrest * 0.4);
    }

    /// <summary>
    /// Calculate food import ratio (for rural faction red line).
    /// </summary>
    private static double CalculateFoodImportRatio(SimulationState state, int countryId)
    {
        double totalFoodSupply = 0;
        double importedFood = 0;

        foreach (var relation in state.TradeRelations)
        {
            if (relation.ToCountryId == countryId)
            {
                importedFood += relation.CurrentTradeVolume[(int)Commodity.Agriculture];
            }
        }

        foreach (var region in state.GetRegionsForCountry(countryId))
        {
            totalFoodSupply += region.Supply[(int)Commodity.Agriculture];
        }

        totalFoodSupply += importedFood;

        return totalFoodSupply > 0 ? importedFood / totalFoodSupply : 0;
    }

    /// <summary>
    /// Get most powerful faction in a country.
    /// </summary>
    public static Faction? GetDominantFaction(SimulationState state, int countryId)
    {
        Faction? dominant = null;
        double maxPower = 0;

        foreach (var faction in state.GetFactionsForCountry(countryId))
        {
            if (faction.Power > maxPower)
            {
                maxPower = faction.Power;
                dominant = faction;
            }
        }

        return dominant;
    }

    /// <summary>
    /// Get least satisfied faction in a country.
    /// </summary>
    public static Faction? GetMostDisgruntledFaction(SimulationState state, int countryId)
    {
        Faction? disgruntled = null;
        double minSatisfaction = double.MaxValue;

        foreach (var faction in state.GetFactionsForCountry(countryId))
        {
            if (faction.Satisfaction < minSatisfaction)
            {
                minSatisfaction = faction.Satisfaction;
                disgruntled = faction;
            }
        }

        return disgruntled;
    }
}
