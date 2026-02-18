namespace GeoSim.SimCore.Data;

/// <summary>
/// Bilateral trade relationship between two countries.
/// </summary>
public sealed class TradeRelation
{
    /// <summary>Exporting country ID.</summary>
    public int FromCountryId { get; init; }

    /// <summary>Importing country ID.</summary>
    public int ToCountryId { get; init; }

    // === Tariffs ===

    /// <summary>Tariff rates by commodity [0, 1]. Importer's tariff on exporter.</summary>
    public double[] TariffRates { get; } = new double[CommodityConstants.Count];

    // === Trade Volumes ===

    /// <summary>Base trade volume by commodity (from MRIO data).</summary>
    public double[] BaseTradeVolume { get; } = new double[CommodityConstants.Count];

    /// <summary>Current trade volume by commodity.</summary>
    public double[] CurrentTradeVolume { get; } = new double[CommodityConstants.Count];

    // === Relations ===

    /// <summary>Diplomatic relations score [-100, 100].</summary>
    public double RelationsScore { get; set; }

    /// <summary>Reliability score [0, 1] — how dependable is the exporter.</summary>
    public double Reliability { get; set; } = 1.0;

    /// <summary>Distance penalty [0, 1] — higher = more costly to trade.</summary>
    public double DistancePenalty { get; set; }

    /// <summary>Treaty bonus [0, 1] — FTAs, customs unions, etc.</summary>
    public double TreatyBonus { get; set; }

    // === Sanctions ===

    /// <summary>Sanction severity [0, 1]. 1 = full embargo.</summary>
    public double SanctionSeverity { get; set; }

    /// <summary>Transport cost per unit (flat cost added to price).</summary>
    public double TransportCost { get; set; }
}
