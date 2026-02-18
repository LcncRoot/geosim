namespace GeoSim.SimCore.Data;

/// <summary>
/// Aggregated commodity types for player-facing UI.
/// Maps to underlying ISIC sectors for simulation.
/// </summary>
public enum Commodity
{
    Agriculture = 0,
    Energy = 1,
    Minerals = 2,
    Manufacturing = 3,
    Construction = 4,
    Services = 5,
    Transport = 6,
    Technology = 7,
    Finance = 8,
    Defense = 9
}
