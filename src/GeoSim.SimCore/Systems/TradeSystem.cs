namespace GeoSim.SimCore.Systems;

using GeoSim.SimCore.Data;

/// <summary>
/// Stateless trade system implementing bilateral trade flows.
/// From equations.md §4.
/// </summary>
public static class TradeSystem
{
    /// <summary>Default trade elasticity γ (price sensitivity of trade).</summary>
    public const double DefaultTradeElasticity = 2.0;

    /// <summary>Minimum trade flow multiplier to prevent complete trade collapse.</summary>
    public const double MinTradeMultiplier = 0.01;

    /// <summary>Maximum trade flow multiplier to prevent explosion.</summary>
    public const double MaxTradeMultiplier = 10.0;

    /// <summary>
    /// Update all bilateral trade flows and country trade balances.
    /// </summary>
    public static void UpdateAllTrade(SimulationState state)
    {
        // Reset trade balances
        foreach (var country in state.Countries)
        {
            country.TradeBalance = 0;
        }

        // Calculate trade flows for each bilateral relation
        foreach (var relation in state.TradeRelations)
        {
            UpdateTradeRelation(state, relation);
        }

        // Update FX reserves based on trade balance
        foreach (var country in state.Countries)
        {
            UpdateFxReserves(country);
        }
    }

    /// <summary>
    /// Update trade flows for a single bilateral relationship.
    /// </summary>
    public static void UpdateTradeRelation(SimulationState state, TradeRelation relation)
    {
        var exporter = state.GetCountry(relation.FromCountryId);
        var importer = state.GetCountry(relation.ToCountryId);

        for (int s = 0; s < CommodityConstants.Count; s++)
        {
            // Calculate actual trade flow
            double tradeFlow = CalculateTradeFlow(
                relation.BaseTradeVolume[s],
                exporter.Prices[s],
                importer.Prices[s],
                relation.TariffRates[s],
                relation.SanctionSeverity,
                DefaultTradeElasticity);

            relation.CurrentTradeVolume[s] = tradeFlow;

            // Update trade balances
            // Exporter gains: price * volume
            // Importer pays: price * (1 + tariff) * volume
            double exportValue = exporter.Prices[s] * tradeFlow;
            double importCost = exporter.Prices[s] * (1.0 + relation.TariffRates[s]) * tradeFlow;

            exporter.TradeBalance += (long)exportValue;
            importer.TradeBalance -= (long)importCost;

            // Tariff revenue goes to importer's tax revenue
            double tariffRevenue = relation.TariffRates[s] * exporter.Prices[s] * tradeFlow;
            importer.TaxRevenue += (long)tariffRevenue;
        }
    }

    /// <summary>
    /// Calculate bilateral trade flow for a single commodity.
    /// T = T_base × (P_importer / (P_exporter × (1 + tariff)))^γ × sanction_factor
    /// From equations.md §4.1.
    /// </summary>
    public static double CalculateTradeFlow(
        double baseVolume,
        double exporterPrice,
        double importerPrice,
        double tariffRate,
        double sanctionSeverity,
        double tradeElasticity)
    {
        if (baseVolume <= 0) return 0;

        // Sanctions can completely block trade
        if (sanctionSeverity >= 1.0) return 0;
        double sanctionFactor = 1.0 - sanctionSeverity;

        // Prevent division by zero
        double effectiveExporterPrice = Math.Max(exporterPrice * (1.0 + tariffRate), 0.0001);

        // Price ratio: higher importer price relative to (exporter price + tariff) increases trade
        double priceRatio = importerPrice / effectiveExporterPrice;

        // Apply trade elasticity
        double priceMultiplier = Math.Pow(priceRatio, tradeElasticity);

        // Clamp multiplier to prevent extreme values
        priceMultiplier = Math.Clamp(priceMultiplier, MinTradeMultiplier, MaxTradeMultiplier);

        return baseVolume * priceMultiplier * sanctionFactor;
    }

    /// <summary>
    /// Calculate effective import price including tariff.
    /// </summary>
    public static double CalculateImportPrice(double basePrice, double tariffRate)
    {
        return basePrice * (1.0 + tariffRate);
    }

    /// <summary>
    /// Calculate tariff revenue from imports.
    /// R_tariff = τ × P × T
    /// From equations.md §6.1.
    /// </summary>
    public static double CalculateTariffRevenue(
        double tariffRate,
        double importPrice,
        double importVolume)
    {
        return tariffRate * importPrice * importVolume;
    }

    /// <summary>
    /// Update foreign exchange reserves based on trade balance.
    /// FX_t+1 = FX_t + TB × Δt
    /// From equations.md §4.3.
    /// </summary>
    public static void UpdateFxReserves(Country country)
    {
        // Weekly tick = 1/52 of a year
        const double deltaT = 1.0 / 52.0;

        // Add trade balance to FX reserves (scaled by time)
        country.FxReserves += (long)(country.TradeBalance * deltaT);
    }

    /// <summary>
    /// Calculate total exports for a country across all partners.
    /// </summary>
    public static void CalculateTotalExports(
        SimulationState state,
        int countryId,
        Span<double> exports)
    {
        exports.Clear();

        foreach (var relation in state.TradeRelations)
        {
            if (relation.FromCountryId == countryId)
            {
                for (int s = 0; s < CommodityConstants.Count; s++)
                {
                    exports[s] += relation.CurrentTradeVolume[s];
                }
            }
        }
    }

    /// <summary>
    /// Calculate total imports for a country across all partners.
    /// </summary>
    public static void CalculateTotalImports(
        SimulationState state,
        int countryId,
        Span<double> imports)
    {
        imports.Clear();

        foreach (var relation in state.TradeRelations)
        {
            if (relation.ToCountryId == countryId)
            {
                for (int s = 0; s < CommodityConstants.Count; s++)
                {
                    imports[s] += relation.CurrentTradeVolume[s];
                }
            }
        }
    }

    /// <summary>
    /// Get all trade partners for a country (both import and export).
    /// </summary>
    public static IEnumerable<int> GetTradePartners(SimulationState state, int countryId)
    {
        var partners = new HashSet<int>();

        foreach (var relation in state.TradeRelations)
        {
            if (relation.FromCountryId == countryId)
                partners.Add(relation.ToCountryId);
            else if (relation.ToCountryId == countryId)
                partners.Add(relation.FromCountryId);
        }

        return partners;
    }

    /// <summary>
    /// Calculate trade dependency ratio for a country on a specific partner.
    /// Higher values indicate greater dependency.
    /// </summary>
    public static double CalculateTradeDependency(
        SimulationState state,
        int countryId,
        int partnerCountryId)
    {
        double totalTradeWithPartner = 0;
        double totalTrade = 0;

        foreach (var relation in state.TradeRelations)
        {
            double relationVolume = 0;
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                relationVolume += relation.CurrentTradeVolume[s];
            }

            bool involvesCountry = relation.FromCountryId == countryId ||
                                   relation.ToCountryId == countryId;
            bool involvesPartner = relation.FromCountryId == partnerCountryId ||
                                   relation.ToCountryId == partnerCountryId;

            if (involvesCountry)
            {
                totalTrade += relationVolume;
                if (involvesPartner)
                {
                    totalTradeWithPartner += relationVolume;
                }
            }
        }

        return totalTrade > 0 ? totalTradeWithPartner / totalTrade : 0;
    }

    /// <summary>
    /// Apply sanctions between two countries.
    /// </summary>
    public static void ApplySanctions(
        SimulationState state,
        int sanctioningCountryId,
        int targetCountryId,
        double severity)
    {
        severity = Math.Clamp(severity, 0, 1);

        // Find relations in both directions
        var relation1 = state.GetTradeRelation(sanctioningCountryId, targetCountryId);
        var relation2 = state.GetTradeRelation(targetCountryId, sanctioningCountryId);

        if (relation1 != null)
            relation1.SanctionSeverity = severity;
        if (relation2 != null)
            relation2.SanctionSeverity = severity;
    }

    /// <summary>
    /// Set tariff rate for imports from a specific country.
    /// </summary>
    public static void SetTariff(
        SimulationState state,
        int importerCountryId,
        int exporterCountryId,
        Commodity commodity,
        double tariffRate)
    {
        tariffRate = Math.Clamp(tariffRate, 0, 1);

        var relation = state.GetTradeRelation(exporterCountryId, importerCountryId);
        if (relation != null)
        {
            relation.TariffRates[(int)commodity] = tariffRate;
        }
    }

    /// <summary>
    /// Set uniform tariff rate for all commodities from a specific country.
    /// </summary>
    public static void SetUniformTariff(
        SimulationState state,
        int importerCountryId,
        int exporterCountryId,
        double tariffRate)
    {
        tariffRate = Math.Clamp(tariffRate, 0, 1);

        var relation = state.GetTradeRelation(exporterCountryId, importerCountryId);
        if (relation != null)
        {
            for (int s = 0; s < CommodityConstants.Count; s++)
            {
                relation.TariffRates[s] = tariffRate;
            }
        }
    }
}
