namespace GeoSim.SimCore.Data;

/// <summary>
/// Type of military formation.
/// </summary>
public enum FormationType
{
    Infantry = 0,
    Armor = 1,
    Artillery = 2,
    AirForce = 3,
    Navy = 4,
    SpecialForces = 5,
    Reserves = 6
}

/// <summary>
/// An abstract military formation (division, squadron, fleet).
/// </summary>
public sealed class MilitaryFormation
{
    /// <summary>Unique formation ID.</summary>
    public int Id { get; init; }

    /// <summary>Owning country ID.</summary>
    public int CountryId { get; init; }

    /// <summary>Display name (e.g., "1st Infantry Division").</summary>
    public required string Name { get; init; }

    /// <summary>Formation type.</summary>
    public FormationType Type { get; init; }

    // === Strength ===

    /// <summary>Base strength at full complement.</summary>
    public double BaseStrength { get; init; }

    /// <summary>Current strength (reduced by casualties).</summary>
    public double Strength { get; set; }

    /// <summary>Personnel count.</summary>
    public int Personnel { get; set; }

    // === Readiness ===

    /// <summary>Training level [0, 1].</summary>
    public double TrainingLevel { get; set; } = 0.5;

    /// <summary>Equipment maintenance level [0, 1].</summary>
    public double Maintenance { get; set; } = 1.0;

    /// <summary>Morale [0, 1].</summary>
    public double Morale { get; set; } = 0.7;

    /// <summary>Overall readiness (computed from training, maintenance, morale).</summary>
    public double Readiness => (TrainingLevel + Maintenance + Morale) / 3.0;

    // === Equipment ===

    /// <summary>Base equipment quality [0, 2]. 1.0 = standard.</summary>
    public double BaseEquipmentQuality { get; init; } = 1.0;

    /// <summary>Equipment age in ticks (affects quality via depreciation).</summary>
    public int EquipmentAge { get; set; }

    /// <summary>Depreciation rate per tick.</summary>
    public double DepreciationRate { get; init; } = 0.001;

    /// <summary>Current equipment quality (after depreciation).</summary>
    public double EquipmentQuality => BaseEquipmentQuality * (1.0 - DepreciationRate * EquipmentAge);

    // === Supply ===

    /// <summary>MilitaryGoods required per tick for maintenance.</summary>
    public double MaintenanceCost { get; init; }

    /// <summary>MilitaryGoods required for combat operations (multiplied during combat).</summary>
    public double CombatSupplyCost { get; init; }

    /// <summary>Current supply status [0, 1].</summary>
    public double SupplyStatus { get; set; } = 1.0;

    // === Deployment ===

    /// <summary>Whether formation is actively deployed.</summary>
    public bool IsDeployed { get; set; }

    /// <summary>Current hex position (if deployed).</summary>
    public int HexId { get; set; }

    /// <summary>Whether engaged in combat this tick.</summary>
    public bool InCombat { get; set; }

    // === Combat ===

    /// <summary>Combat power = strength * readiness * supplyStatus * equipmentQuality.</summary>
    public double CombatPower => Strength * Readiness * SupplyStatus * Math.Max(0.1, EquipmentQuality);

    /// <summary>Supply consumption this tick (higher if in combat).</summary>
    public double GetSupplyConsumption()
    {
        return InCombat ? CombatSupplyCost * 3.0 : MaintenanceCost;
    }
}
