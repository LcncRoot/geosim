namespace GeoSim.SimCore.Data;

/// <summary>
/// Resource and commodity types in the simulation.
/// Raw resources (0-5) are harvestable from deposits.
/// Manufactured goods (6-10) are produced by facilities.
/// Services (11) are produced by labor and cannot be stockpiled.
/// </summary>
public enum Commodity
{
    Agriculture = 0,    // Food — harvestable from arable hexes
    RareEarths = 1,     // REEs — harvestable from deposits
    Petroleum = 2,      // Oil/gas — harvestable from deposits
    Coal = 3,           // Coal — harvestable from deposits
    Ore = 4,            // Metals — harvestable from deposits
    Uranium = 5,        // Nuclear fuel — harvestable, rare
    Electricity = 6,    // Produced from fuel, CANNOT be stockpiled
    ConsumerGoods = 7,  // Manufactured
    IndustrialGoods = 8,// Manufactured
    MilitaryGoods = 9,  // Manufactured
    Electronics = 10,   // Semiconductors — manufactured
    Services = 11       // Cannot be stockpiled (100% spoilage)
}

/// <summary>
/// Constants for commodity system.
/// </summary>
public static class CommodityConstants
{
    public const int Count = 12;

    /// <summary>Raw resources that can be extracted from deposits.</summary>
    public static readonly Commodity[] RawResources =
    [
        Commodity.Agriculture,
        Commodity.RareEarths,
        Commodity.Petroleum,
        Commodity.Coal,
        Commodity.Ore,
        Commodity.Uranium
    ];

    /// <summary>Manufactured goods produced by facilities.</summary>
    public static readonly Commodity[] ManufacturedGoods =
    [
        Commodity.Electricity,
        Commodity.ConsumerGoods,
        Commodity.IndustrialGoods,
        Commodity.MilitaryGoods,
        Commodity.Electronics
    ];

    /// <summary>Commodities that cannot be stockpiled.</summary>
    public static readonly Commodity[] NonStockpileable =
    [
        Commodity.Electricity,
        Commodity.Services
    ];

    /// <summary>Check if a commodity can be stockpiled.</summary>
    public static bool CanStockpile(Commodity c) =>
        c != Commodity.Electricity && c != Commodity.Services;
}
