namespace GeoSim.SimCore.Systems;

using GeoSim.SimCore.Data;

/// <summary>
/// Stateless fiscal system implementing tax collection, spending, and debt dynamics.
/// From equations.md §6.
/// </summary>
public static class FiscalSystem
{
    /// <summary>Debt-to-GDP threshold before interest premium applies.</summary>
    public const double DebtThreshold = 0.6;

    /// <summary>Interest rate premium coefficient κ.</summary>
    public const double DebtRiskPremium = 0.02;

    /// <summary>Weekly time step (1/52 of a year).</summary>
    public const double DeltaT = 1.0 / 52.0;

    /// <summary>
    /// Update fiscal state for a country (runs monthly = every 4 ticks).
    /// </summary>
    public static void UpdateCountry(SimulationState state, int countryId)
    {
        var country = state.GetCountry(countryId);

        // Calculate tax revenue
        long taxRevenue = CalculateTotalTaxRevenue(state, country);
        country.TaxRevenue = taxRevenue;

        // Calculate government spending
        long spending = CalculateGovernmentSpending(country);
        country.GovSpending = spending;

        // Update debt based on budget balance
        UpdateDebt(country);

        // Update interest rate based on debt level
        UpdateInterestRate(country);
    }

    /// <summary>
    /// Calculate total tax revenue from all sources.
    /// R = R_income + R_corporate + R_tariff + R_VAT
    /// From equations.md §6.1.
    /// </summary>
    public static long CalculateTotalTaxRevenue(SimulationState state, Country country)
    {
        long incomeRevenue = CalculateIncomeTaxRevenue(country);
        long corporateRevenue = CalculateCorporateTaxRevenue(state, country);
        long vatRevenue = CalculateVatRevenue(state, country);
        // Tariff revenue is already added during trade processing

        return incomeRevenue + corporateRevenue + vatRevenue;
    }

    /// <summary>
    /// Calculate income tax revenue.
    /// R_income = τ_inc × TotalWages
    /// </summary>
    public static long CalculateIncomeTaxRevenue(Country country)
    {
        return (long)(country.IncomeTaxRate * country.TotalWages);
    }

    /// <summary>
    /// Calculate income tax revenue from wages.
    /// </summary>
    public static long CalculateIncomeTaxRevenue(double taxRate, long totalWages)
    {
        return (long)(taxRate * totalWages);
    }

    /// <summary>
    /// Calculate corporate tax revenue.
    /// R_corporate = τ_corp × Σ max(0, VA - wages)
    /// From equations.md §6.1.
    /// </summary>
    public static long CalculateCorporateTaxRevenue(SimulationState state, Country country)
    {
        double totalProfits = 0;

        foreach (var region in state.GetRegionsForCountry(country.Id))
        {
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                ref var sector = ref region.Sectors[s];
                double sectorWages = sector.LaborEmployed * region.SectorWages[s];
                double profit = sector.ValueAdded - sectorWages;
                if (profit > 0)
                {
                    totalProfits += profit;
                }
            }
        }

        return (long)(country.CorporateTaxRate * totalProfits);
    }

    /// <summary>
    /// Calculate corporate tax from profits.
    /// </summary>
    public static long CalculateCorporateTaxRevenue(double taxRate, double profits)
    {
        if (profits <= 0) return 0;
        return (long)(taxRate * profits);
    }

    /// <summary>
    /// Calculate VAT revenue from consumption.
    /// R_VAT = τ_VAT × C_total
    /// </summary>
    public static long CalculateVatRevenue(SimulationState state, Country country)
    {
        // Estimate consumption from population cohorts
        double totalConsumption = 0;

        foreach (var cohort in state.PopulationCohorts)
        {
            if (IsInCountry(state, cohort, country.Id))
            {
                // Consumption = Income × (1 - SavingsRate)
                totalConsumption += cohort.Income * (1.0 - cohort.SavingsRate);
            }
        }

        return (long)(country.VatRate * totalConsumption);
    }

    /// <summary>
    /// Calculate VAT from consumption amount.
    /// </summary>
    public static long CalculateVatRevenue(double vatRate, double consumption)
    {
        return (long)(vatRate * consumption);
    }

    /// <summary>
    /// Calculate tariff revenue from imports.
    /// Already computed during trade processing, but can recalculate.
    /// </summary>
    public static long CalculateTariffRevenue(
        SimulationState state,
        int countryId)
    {
        long tariffRevenue = 0;

        foreach (var relation in state.TradeRelations)
        {
            if (relation.ToCountryId == countryId)
            {
                var exporter = state.GetCountry(relation.FromCountryId);
                for (int s = 0; s < CommodityConstants.Count; s++)
                {
                    double tariff = relation.TariffRates[s];
                    double volume = relation.CurrentTradeVolume[s];
                    double price = exporter.Prices[s];
                    tariffRevenue += (long)(tariff * price * volume);
                }
            }
        }

        return tariffRevenue;
    }

    /// <summary>
    /// Calculate government spending based on GDP share and allocations.
    /// G = G_consumption + G_transfers + G_investment + G_military + G_interest
    /// From equations.md §6.2.
    /// </summary>
    public static long CalculateGovernmentSpending(Country country)
    {
        // Base spending as share of GDP (typical: 30-50%)
        double baseSpending = country.Gdp * 0.35 * DeltaT; // Weekly share

        // Interest payments on debt
        long interestPayment = CalculateInterestPayment(country);

        return (long)baseSpending + interestPayment;
    }

    /// <summary>
    /// Calculate spending by category.
    /// </summary>
    public static (long welfare, long education, long defense, long infrastructure, long healthcare)
        CalculateSpendingByCategory(Country country, long totalSpending)
    {
        // Exclude interest from discretionary spending
        long interestPayment = CalculateInterestPayment(country);
        long discretionary = Math.Max(0, totalSpending - interestPayment);

        return (
            welfare: (long)(discretionary * country.WelfareSpendingShare),
            education: (long)(discretionary * country.EducationSpendingShare),
            defense: (long)(discretionary * country.DefenseSpendingShare),
            infrastructure: (long)(discretionary * country.InfrastructureSpendingShare),
            healthcare: (long)(discretionary * country.HealthcareSpendingShare)
        );
    }

    /// <summary>
    /// Calculate interest payment on debt.
    /// G_interest = i × D × Δt
    /// </summary>
    public static long CalculateInterestPayment(Country country)
    {
        return (long)(country.InterestRate * country.Debt * DeltaT);
    }

    /// <summary>
    /// Calculate interest payment from rate and debt.
    /// </summary>
    public static long CalculateInterestPayment(double interestRate, long debt)
    {
        return (long)(interestRate * debt * DeltaT);
    }

    /// <summary>
    /// Calculate budget balance.
    /// B = R - G
    /// From equations.md §6.3.
    /// </summary>
    public static long CalculateBudgetBalance(long revenue, long spending)
    {
        return revenue - spending;
    }

    /// <summary>
    /// Update debt based on budget balance.
    /// D_t+1 = D_t - B × Δt
    /// From equations.md §6.4.
    /// </summary>
    public static void UpdateDebt(Country country)
    {
        // Budget balance is negative when spending > revenue (deficit)
        // Deficit increases debt, surplus decreases debt
        long balance = country.BudgetBalance;

        // Debt changes by the budget balance
        // If balance is negative (deficit), debt increases
        // If balance is positive (surplus), debt decreases
        country.Debt -= balance;

        // Debt cannot go below zero
        if (country.Debt < 0)
        {
            country.Debt = 0;
        }
    }

    /// <summary>
    /// Update debt by a specific amount.
    /// </summary>
    public static long UpdateDebt(long currentDebt, long budgetBalance)
    {
        long newDebt = currentDebt - budgetBalance;
        return Math.Max(0, newDebt);
    }

    /// <summary>
    /// Update interest rate based on debt-to-GDP ratio.
    /// i = i_base + max(0, κ × (d - d_threshold))
    /// From equations.md §6.5.
    /// </summary>
    public static void UpdateInterestRate(Country country)
    {
        country.InterestRate = CalculateInterestRate(
            country.BaseInterestRate,
            country.DebtToGdp);
    }

    /// <summary>
    /// Calculate interest rate with debt risk premium.
    /// </summary>
    public static double CalculateInterestRate(double baseRate, double debtToGdp)
    {
        double riskPremium = 0;

        if (debtToGdp > DebtThreshold)
        {
            riskPremium = DebtRiskPremium * (debtToGdp - DebtThreshold);
        }

        return baseRate + riskPremium;
    }

    /// <summary>
    /// Calculate debt-to-GDP ratio.
    /// </summary>
    public static double CalculateDebtToGdp(long debt, long gdp)
    {
        if (gdp <= 0) return 0;
        return (double)debt / gdp;
    }

    /// <summary>
    /// Check if debt is sustainable (below critical threshold).
    /// </summary>
    public static bool IsDebtSustainable(Country country)
    {
        // Debt above 100% GDP is concerning
        // Debt above 150% GDP is critical
        return country.DebtToGdp < 1.5;
    }

    /// <summary>
    /// Calculate primary balance (excluding interest payments).
    /// </summary>
    public static long CalculatePrimaryBalance(Country country)
    {
        long interestPayment = CalculateInterestPayment(country);
        return country.TaxRevenue - (country.GovSpending - interestPayment);
    }

    /// <summary>
    /// Calculate fiscal multiplier effect on GDP.
    /// Simplified: spending increase boosts GDP by multiplier.
    /// </summary>
    public static double CalculateFiscalMultiplier(double spendingChange, double multiplier = 1.5)
    {
        return spendingChange * multiplier;
    }

    /// <summary>
    /// Estimate GDP from production.
    /// GDP = Σ VA (sum of value added across all sectors).
    /// From equations.md §9.2.
    /// </summary>
    public static long EstimateGdpFromProduction(SimulationState state, int countryId)
    {
        double totalVA = 0;

        foreach (var region in state.GetRegionsForCountry(countryId))
        {
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                totalVA += region.Sectors[s].ValueAdded;
            }
        }

        // Scale to annual (multiply by 52 weeks)
        return (long)(totalVA * 52);
    }

    /// <summary>
    /// Update GDP tracking.
    /// </summary>
    public static void UpdateGdp(SimulationState state, Country country)
    {
        country.GdpPrevious = country.Gdp;
        country.Gdp = EstimateGdpFromProduction(state, country.Id);
    }

    /// <summary>
    /// Calculate real GDP growth rate.
    /// </summary>
    public static double CalculateGdpGrowthRate(long currentGdp, long previousGdp)
    {
        if (previousGdp <= 0) return 0;
        return (double)(currentGdp - previousGdp) / previousGdp;
    }

    /// <summary>
    /// Helper to check if a cohort is in a country.
    /// </summary>
    private static bool IsInCountry(SimulationState state, PopulationCohort cohort, int countryId)
    {
        var region = state.GetRegion(cohort.RegionId);
        return region.CountryId == countryId;
    }
}
