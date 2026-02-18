namespace GeoSim.SimCore.Data;

/// <summary>
/// Aggregated commodity types for player-facing UI.
/// Maps to underlying ISIC sectors for simulation.
/// </summary>
public enum Commodity
{
    Food = 0,           // Agriculture + Food products
    Energy = 1,         // Mining (oil/gas/coal) + Utilities
    Metals = 2,         // Basic metals + Metal products
    Chemicals = 3,      // Chemicals + Pharma
    IndustrialGoods = 4,// Machinery + Electrical equipment
    Electronics = 5,    // Computer/electronic/optical (semiconductors)
    ConsumerGoods = 6,  // Textiles + Other manufacturing
    MilitaryGoods = 7,  // Derived from IndustrialGoods + Electronics
    Services = 8,       // All service sectors
    Construction = 9    // Construction
}
