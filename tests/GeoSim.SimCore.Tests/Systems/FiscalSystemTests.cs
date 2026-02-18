using GeoSim.SimCore.Data;
using GeoSim.SimCore.Systems;

namespace GeoSim.SimCore.Tests.Systems;

public class FiscalSystemTests
{
    [Fact]
    public void IncomeTax_CalculatedFromWages()
    {
        // Given: wages and tax rate
        double taxRate = 0.15;
        long totalWages = 1_000_000;

        // When: calculate income tax
        long revenue = FiscalSystem.CalculateIncomeTaxRevenue(taxRate, totalWages);

        // Then: 15% of wages
        Assert.Equal(150_000, revenue);
    }

    [Fact]
    public void CorporateTax_CalculatedFromProfits()
    {
        // Given: profits and tax rate
        double taxRate = 0.20;
        double profits = 500_000;

        // When: calculate corporate tax
        long revenue = FiscalSystem.CalculateCorporateTaxRevenue(taxRate, profits);

        // Then: 20% of profits
        Assert.Equal(100_000, revenue);
    }

    [Fact]
    public void CorporateTax_NoProfits_NoTax()
    {
        // Given: no profits (loss)
        double taxRate = 0.20;
        double profits = -100_000;

        // When: calculate corporate tax
        long revenue = FiscalSystem.CalculateCorporateTaxRevenue(taxRate, profits);

        // Then: no tax
        Assert.Equal(0, revenue);
    }

    [Fact]
    public void VatRevenue_CalculatedFromConsumption()
    {
        // Given: consumption and VAT rate
        double vatRate = 0.10;
        double consumption = 800_000;

        // When: calculate VAT
        long revenue = FiscalSystem.CalculateVatRevenue(vatRate, consumption);

        // Then: 10% of consumption
        Assert.Equal(80_000, revenue);
    }

    [Fact]
    public void InterestPayment_CalculatedFromDebt()
    {
        // Given: debt and interest rate
        double interestRate = 0.05;  // 5% annual
        long debt = 10_000_000;

        // When: calculate interest (weekly)
        long payment = FiscalSystem.CalculateInterestPayment(interestRate, debt);

        // Then: annual rate / 52 weeks
        // 0.05 × 10M × (1/52) ≈ 9615
        Assert.True(payment > 0);
        Assert.True(payment > 9000 && payment < 10000);
    }

    [Fact]
    public void BudgetBalance_SurplusWhenRevenueExceedsSpending()
    {
        // Given: revenue > spending
        long revenue = 1_000_000;
        long spending = 800_000;

        // When: calculate balance
        long balance = FiscalSystem.CalculateBudgetBalance(revenue, spending);

        // Then: positive balance (surplus)
        Assert.Equal(200_000, balance);
    }

    [Fact]
    public void BudgetBalance_DeficitWhenSpendingExceedsRevenue()
    {
        // Given: spending > revenue
        long revenue = 800_000;
        long spending = 1_000_000;

        // When: calculate balance
        long balance = FiscalSystem.CalculateBudgetBalance(revenue, spending);

        // Then: negative balance (deficit)
        Assert.Equal(-200_000, balance);
    }

    [Fact]
    public void DebtUpdate_DeficitIncreasesDebt()
    {
        // Given: existing debt and deficit
        long currentDebt = 5_000_000;
        long budgetBalance = -100_000;  // Deficit

        // When: update debt
        long newDebt = FiscalSystem.UpdateDebt(currentDebt, budgetBalance);

        // Then: debt increases
        Assert.Equal(5_100_000, newDebt);
    }

    [Fact]
    public void DebtUpdate_SurplusDecreasesDebt()
    {
        // Given: existing debt and surplus
        long currentDebt = 5_000_000;
        long budgetBalance = 100_000;  // Surplus

        // When: update debt
        long newDebt = FiscalSystem.UpdateDebt(currentDebt, budgetBalance);

        // Then: debt decreases
        Assert.Equal(4_900_000, newDebt);
    }

    [Fact]
    public void DebtUpdate_CannotGoBelowZero()
    {
        // Given: small debt and large surplus
        long currentDebt = 50_000;
        long budgetBalance = 100_000;

        // When: update debt
        long newDebt = FiscalSystem.UpdateDebt(currentDebt, budgetBalance);

        // Then: debt is zero, not negative
        Assert.Equal(0, newDebt);
    }

    [Fact]
    public void InterestRate_NoRiskPremiumBelowThreshold()
    {
        // Given: debt below threshold
        double baseRate = 0.02;
        double debtToGdp = 0.50;  // Below 0.6 threshold

        // When: calculate interest rate
        double rate = FiscalSystem.CalculateInterestRate(baseRate, debtToGdp);

        // Then: just base rate
        Assert.Equal(0.02, rate);
    }

    [Fact]
    public void InterestRate_RiskPremiumAboveThreshold()
    {
        // Given: debt above threshold
        double baseRate = 0.02;
        double debtToGdp = 0.80;  // Above 0.6 threshold

        // When: calculate interest rate
        double rate = FiscalSystem.CalculateInterestRate(baseRate, debtToGdp);

        // Then: base + risk premium
        // premium = 0.02 × (0.80 - 0.60) = 0.02 × 0.20 = 0.004
        // total = 0.02 + 0.004 = 0.024
        Assert.Equal(0.024, rate, 4);
    }

    [Fact]
    public void InterestRate_HighDebt_HighPremium()
    {
        // Given: very high debt
        double baseRate = 0.02;
        double debtToGdp = 1.50;  // 150% debt/GDP

        // When: calculate interest rate
        double rate = FiscalSystem.CalculateInterestRate(baseRate, debtToGdp);

        // Then: significant premium
        // premium = 0.02 × (1.50 - 0.60) = 0.02 × 0.90 = 0.018
        // total = 0.02 + 0.018 = 0.038
        Assert.Equal(0.038, rate, 4);
    }

    [Fact]
    public void DebtToGdp_CalculatedCorrectly()
    {
        // Given: debt and GDP
        long debt = 60_000_000;
        long gdp = 100_000_000;

        // When: calculate ratio
        double ratio = FiscalSystem.CalculateDebtToGdp(debt, gdp);

        // Then: 60%
        Assert.Equal(0.60, ratio);
    }

    [Fact]
    public void DebtToGdp_ZeroGdp_ReturnsZero()
    {
        // Given: zero GDP
        long debt = 60_000_000;
        long gdp = 0;

        // When: calculate ratio
        double ratio = FiscalSystem.CalculateDebtToGdp(debt, gdp);

        // Then: zero (not infinity/NaN)
        Assert.Equal(0.0, ratio);
    }

    [Fact]
    public void GdpGrowthRate_Calculated()
    {
        // Given: GDP values
        long currentGdp = 105_000_000;
        long previousGdp = 100_000_000;

        // When: calculate growth
        double growth = FiscalSystem.CalculateGdpGrowthRate(currentGdp, previousGdp);

        // Then: 5% growth
        Assert.Equal(0.05, growth, 4);
    }

    [Fact]
    public void DebtSustainability_SustainableBelowThreshold()
    {
        // Given: country with manageable debt
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            Gdp = 100_000_000,
            Debt = 80_000_000  // 80% debt/GDP
        };

        // When: check sustainability
        bool sustainable = FiscalSystem.IsDebtSustainable(country);

        // Then: sustainable (below 150%)
        Assert.True(sustainable);
    }

    [Fact]
    public void DebtSustainability_UnsustainableAboveThreshold()
    {
        // Given: country with excessive debt
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            Gdp = 100_000_000,
            Debt = 200_000_000  // 200% debt/GDP
        };

        // When: check sustainability
        bool sustainable = FiscalSystem.IsDebtSustainable(country);

        // Then: unsustainable
        Assert.False(sustainable);
    }

    [Fact]
    public void SpendingByCategory_AllocatedCorrectly()
    {
        // Given: country with spending shares
        var country = new Country
        {
            Code = "TST",
            Name = "Test",
            WelfareSpendingShare = 0.35,
            EducationSpendingShare = 0.15,
            DefenseSpendingShare = 0.13,
            InfrastructureSpendingShare = 0.10,
            HealthcareSpendingShare = 0.08,
            InterestRate = 0.0,  // No interest for simplicity
            Debt = 0
        };

        long totalSpending = 1_000_000;

        // When: calculate by category
        var (welfare, education, defense, infrastructure, healthcare) =
            FiscalSystem.CalculateSpendingByCategory(country, totalSpending);

        // Then: allocated by shares
        Assert.Equal(350_000, welfare);
        Assert.Equal(150_000, education);
        Assert.Equal(130_000, defense);
        Assert.Equal(100_000, infrastructure);
        Assert.Equal(80_000, healthcare);
    }

    [Fact]
    public void FiscalMultiplier_BoostsGdp()
    {
        // Given: spending increase
        double spendingChange = 100_000;
        double multiplier = 1.5;

        // When: calculate GDP impact
        double gdpImpact = FiscalSystem.CalculateFiscalMultiplier(spendingChange, multiplier);

        // Then: multiplied effect
        Assert.Equal(150_000, gdpImpact);
    }
}
