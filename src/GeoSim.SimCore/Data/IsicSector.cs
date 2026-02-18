namespace GeoSim.SimCore.Data;

/// <summary>
/// ISIC Rev 4 sector codes as used in OECD ICIO data.
/// Integer indices for array access; string codes for data loading.
/// </summary>
public static class IsicSector
{
    public const int Count = 50;

    // Primary sectors
    public const int A01 = 0;   // Crop and animal production
    public const int A02 = 1;   // Forestry and logging
    public const int A03 = 2;   // Fishing and aquaculture
    public const int B05 = 3;   // Coal and lignite
    public const int B06 = 4;   // Crude petroleum and gas
    public const int B07 = 5;   // Metal ores
    public const int B08 = 6;   // Other mining
    public const int B09 = 7;   // Mining support services

    // Manufacturing
    public const int C10T12 = 8;    // Food, beverages, tobacco
    public const int C13T15 = 9;    // Textiles, apparel, leather
    public const int C16 = 10;      // Wood products
    public const int C17_18 = 11;   // Paper, printing
    public const int C19 = 12;      // Coke and refined petroleum
    public const int C20 = 13;      // Chemicals
    public const int C21 = 14;      // Pharmaceuticals
    public const int C22 = 15;      // Rubber and plastics
    public const int C23 = 16;      // Non-metallic minerals
    public const int C24A = 17;     // Basic iron and steel
    public const int C24B = 18;     // Other basic metals
    public const int C25 = 19;      // Fabricated metals
    public const int C26 = 20;      // Computer, electronic, optical
    public const int C27 = 21;      // Electrical equipment
    public const int C28 = 22;      // Machinery
    public const int C29 = 23;      // Motor vehicles
    public const int C301 = 24;     // Ships and boats
    public const int C302T309 = 25; // Other transport equipment
    public const int C31T33 = 26;   // Furniture, other manufacturing

    // Utilities
    public const int D = 27;    // Electricity and gas
    public const int E = 28;    // Water, sewerage, waste

    // Construction
    public const int F = 29;    // Construction

    // Services
    public const int G = 30;    // Wholesale and retail trade
    public const int H49 = 31;  // Land transport
    public const int H50 = 32;  // Water transport
    public const int H51 = 33;  // Air transport
    public const int H52 = 34;  // Warehousing
    public const int H53 = 35;  // Postal
    public const int I = 36;    // Accommodation and food services
    public const int J58T60 = 37;   // Publishing, audiovisual
    public const int J61 = 38;      // Telecommunications
    public const int J62_63 = 39;   // IT and information services
    public const int K = 40;    // Financial and insurance
    public const int L = 41;    // Real estate
    public const int M = 42;    // Professional services
    public const int N = 43;    // Administrative services
    public const int O = 44;    // Public administration
    public const int P = 45;    // Education
    public const int Q = 46;    // Health
    public const int R = 47;    // Arts and entertainment
    public const int S = 48;    // Other services
    public const int T = 49;    // Household activities

    /// <summary>
    /// ISIC code strings for parsing MRIO data.
    /// Index matches the const values above.
    /// </summary>
    public static readonly string[] Codes =
    [
        "A01", "A02", "A03", "B05", "B06", "B07", "B08", "B09",
        "C10T12", "C13T15", "C16", "C17_18", "C19", "C20", "C21", "C22", "C23",
        "C24A", "C24B", "C25", "C26", "C27", "C28", "C29", "C301", "C302T309", "C31T33",
        "D", "E", "F", "G",
        "H49", "H50", "H51", "H52", "H53",
        "I", "J58T60", "J61", "J62_63",
        "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T"
    ];

    /// <summary>
    /// Human-readable names for each sector.
    /// </summary>
    public static readonly string[] Names =
    [
        "Crop and animal production",
        "Forestry and logging",
        "Fishing and aquaculture",
        "Coal and lignite",
        "Crude petroleum and gas",
        "Metal ores",
        "Other mining",
        "Mining support services",
        "Food, beverages, tobacco",
        "Textiles, apparel, leather",
        "Wood products",
        "Paper, printing",
        "Coke and refined petroleum",
        "Chemicals",
        "Pharmaceuticals",
        "Rubber and plastics",
        "Non-metallic minerals",
        "Basic iron and steel",
        "Other basic metals",
        "Fabricated metals",
        "Computer, electronic, optical",
        "Electrical equipment",
        "Machinery",
        "Motor vehicles",
        "Ships and boats",
        "Other transport equipment",
        "Furniture, other manufacturing",
        "Electricity and gas",
        "Water, sewerage, waste",
        "Construction",
        "Wholesale and retail trade",
        "Land transport",
        "Water transport",
        "Air transport",
        "Warehousing",
        "Postal",
        "Accommodation and food services",
        "Publishing, audiovisual",
        "Telecommunications",
        "IT and information services",
        "Financial and insurance",
        "Real estate",
        "Professional services",
        "Administrative services",
        "Public administration",
        "Education",
        "Health",
        "Arts and entertainment",
        "Other services",
        "Household activities"
    ];

    /// <summary>
    /// Maps each ISIC sector to its aggregated commodity type.
    /// Based on equations.md commodity definitions.
    /// </summary>
    public static readonly Commodity[] ToCommodity =
    [
        // A01-A03: Food (Agriculture + Food products)
        Commodity.Food, Commodity.Food, Commodity.Food,
        // B05-B06: Energy (Mining - oil/gas/coal)
        Commodity.Energy, Commodity.Energy,
        // B07-B09: Metals (Metal ores, other mining)
        Commodity.Metals, Commodity.Metals, Commodity.Metals,
        // C10T12: Food (Food products, beverages, tobacco)
        Commodity.Food,
        // C13T15-C16: ConsumerGoods (Textiles, wood)
        Commodity.ConsumerGoods, Commodity.ConsumerGoods,
        // C17_18: ConsumerGoods (Paper, printing)
        Commodity.ConsumerGoods,
        // C19: Energy (Coke and refined petroleum)
        Commodity.Energy,
        // C20-C21: Chemicals (Chemicals, Pharma)
        Commodity.Chemicals, Commodity.Chemicals,
        // C22-C23: Chemicals (Rubber, plastics, non-metallic minerals)
        Commodity.Chemicals, Commodity.Chemicals,
        // C24A-C25: Metals (Basic metals, fabricated metals)
        Commodity.Metals, Commodity.Metals, Commodity.Metals,
        // C26: Electronics (Computer, electronic, optical)
        Commodity.Electronics,
        // C27-C28: IndustrialGoods (Electrical equipment, Machinery)
        Commodity.IndustrialGoods, Commodity.IndustrialGoods,
        // C29-C302T309: IndustrialGoods (Vehicles, transport equipment)
        Commodity.IndustrialGoods, Commodity.IndustrialGoods, Commodity.IndustrialGoods,
        // C31T33: ConsumerGoods (Furniture, other manufacturing)
        Commodity.ConsumerGoods,
        // D: Energy (Electricity and gas)
        Commodity.Energy,
        // E: Services (Water, sewerage, waste)
        Commodity.Services,
        // F: Construction
        Commodity.Construction,
        // G-T: Services (all service sectors)
        Commodity.Services, // G: Wholesale/retail
        Commodity.Services, Commodity.Services, Commodity.Services, // H49-H51: Transport
        Commodity.Services, Commodity.Services, // H52-H53: Warehousing, Postal
        Commodity.Services, // I: Accommodation
        Commodity.Services, // J58T60: Publishing
        Commodity.Services, // J61: Telecom
        Commodity.Services, // J62_63: IT services
        Commodity.Services, // K: Finance
        Commodity.Services, // L: Real estate
        Commodity.Services, // M: Professional
        Commodity.Services, // N: Administrative
        Commodity.Services, // O: Public admin
        Commodity.Services, // P: Education
        Commodity.Services, // Q: Health
        Commodity.Services, // R: Arts
        Commodity.Services, // S: Other services
        Commodity.Services  // T: Household
    ];
}
