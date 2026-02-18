# GeoSim — Geopolitical Economy Simulation Engine

## Project Overview

GeoSim is a headless C# simulation core modeling interconnected national economies using Input-Output (Leontief) economics. The simulation supports strategic decision-making in a game context where players manage nations competing for economic and geopolitical advantage.

**Core Design Philosophy:**
- Deterministic simulation — same inputs always produce same outputs
- Stateless systems — all mutable state lives in `SimulationState`
- Performance-conscious — hot paths use arrays and `Span<T>`, avoid LINQ allocations
- Data-driven — scenario configs and technical coefficients loaded from JSON/CSV

## Data Foundation

The simulation is grounded in real OECD Inter-Country Input-Output (ICIO) data:
- **Source**: 2016-2022 Supply and Use tables (SML format)
- **Coverage**: 81 countries/regions including ROW (Rest of World)
- **Sectors**: 50 ISIC Rev 4 industry classifications per country
- **Final Demand**: HFCE, NPISH, GGFC, GFCF, INVNT, DPABR per country

### Sector Classification (ISIC Rev 4)

| Code | Description |
|------|-------------|
| A01 | Crop and animal production |
| A02 | Forestry and logging |
| A03 | Fishing and aquaculture |
| B05-B09 | Mining and quarrying |
| C10T12 | Food, beverages, tobacco |
| C13T15 | Textiles, apparel, leather |
| C16 | Wood products |
| C17_18 | Paper, printing |
| C19 | Coke and refined petroleum |
| C20 | Chemicals |
| C21 | Pharmaceuticals |
| C22 | Rubber and plastics |
| C23 | Non-metallic minerals |
| C24A | Basic iron and steel |
| C24B | Other basic metals |
| C25 | Fabricated metals |
| C26 | Computer, electronic, optical |
| C27 | Electrical equipment |
| C28 | Machinery |
| C29 | Motor vehicles |
| C301 | Ships and boats |
| C302T309 | Other transport equipment |
| C31T33 | Furniture, other manufacturing |
| D | Electricity and gas |
| E | Water, sewerage, waste |
| F | Construction |
| G | Wholesale and retail trade |
| H49 | Land transport |
| H50 | Water transport |
| H51 | Air transport |
| H52 | Warehousing |
| H53 | Postal |
| I | Accommodation and food services |
| J58T60 | Publishing, audiovisual |
| J61 | Telecommunications |
| J62_63 | IT and information services |
| K | Financial and insurance |
| L | Real estate |
| M | Professional services |
| N | Administrative services |
| O | Public administration |
| P | Education |
| Q | Health |
| R | Arts and entertainment |
| S | Other services |
| T | Household activities |

## Architecture

### Directory Structure

```
geosim/
├── CLAUDE.md                    # This file — project spec and memory
├── GeoSim.sln                   # Solution file
├── src/
│   └── GeoSim.SimCore/
│       ├── Data/                # Core data types (records, enums)
│       │   ├── Commodity.cs
│       │   ├── Country.cs
│       │   ├── Region.cs
│       │   ├── Sector.cs
│       │   ├── Faction.cs
│       │   ├── TradeRelation.cs
│       │   ├── PopulationCohort.cs
│       │   ├── MilitaryFormation.cs
│       │   └── SimulationState.cs
│       ├── Systems/             # Stateless simulation systems
│       │   ├── ProductionSystem.cs
│       │   ├── PriceSystem.cs
│       │   ├── TradeSystem.cs
│       │   ├── LaborSystem.cs
│       │   ├── FiscalSystem.cs
│       │   ├── PoliticalSystem.cs
│       │   └── MilitarySystem.cs
│       ├── IO/                  # Serialization, scenario loading
│       │   ├── ScenarioLoader.cs
│       │   └── MRIOLoader.cs
│       └── Simulation.cs        # Main tick loop orchestrator
├── tests/
│   └── GeoSim.SimCore.Tests/
│       ├── Systems/
│       │   ├── ProductionSystemTests.cs
│       │   └── ...
│       └── Integration/
│           └── YearSimulationTests.cs
├── data/
│   ├── mrio/                    # OECD ICIO coefficient matrices
│   │   └── 2022_SML.csv
│   └── scenarios/               # JSON scenario definitions
│       └── south_korea_2024.json
└── docs/
    └── equations.md             # Authoritative equation specification
```

### Core Design Principles

1. **Single State Object**: `SimulationState` contains ALL mutable state
2. **Stateless Systems**: Systems are pure functions: `(SimulationState, deltaTime) → mutations`
3. **Tick-Based Updates**: Weekly tick as base unit; some systems run less frequently
4. **Determinism**: No random state in production; RNG seeded and stored in state
5. **JSON Configuration**: All scenario data via System.Text.Json (no Newtonsoft)

## V0.1 Deliverables

### Phase 1: Project Scaffolding
- [x] Git repository initialized
- [ ] .NET 8.0 solution structure
- [ ] Directory structure created
- [ ] Nullable reference types enabled
- [ ] Initial commit

### Phase 2: Data Model
- [ ] Commodity enum (aggregated from 50 ISIC sectors)
- [ ] Country record
- [ ] Region record
- [ ] Sector record
- [ ] Faction record
- [ ] TradeRelation record
- [ ] PopulationCohort record
- [ ] MilitaryFormation record
- [ ] TechnicalCoefficientMatrix class
- [ ] ScenarioConfig record
- [ ] SimulationState class
- [ ] TickSchedule enum/config

### Phase 3: Production System
- [ ] Leontief production function
- [ ] Soft Leontief bottleneck smoothing
- [ ] Labor constraint
- [ ] Infrastructure modifier
- [ ] Value added calculation
- [ ] Unit tests (6 cases)

### Phase 4: Price System
- [ ] Supply/demand price adjustment
- [ ] Price sensitivity clamping
- [ ] Price smoothing
- [ ] CPI calculation
- [ ] Unit tests (6 cases)

### Phase 5: Trade System
- [ ] Bilateral trade flows
- [ ] Tariff application
- [ ] Trade balance tracking
- [ ] Unit tests

### Phase 6: Labor System
- [ ] Employment by sector
- [ ] Unemployment calculation
- [ ] Wage dynamics
- [ ] Unit tests

### Phase 7: Fiscal System
- [ ] Tax collection (income, corporate, tariff)
- [ ] Government spending
- [ ] Debt dynamics
- [ ] Unit tests

### Phase 8: Political System
- [ ] Faction satisfaction calculation
- [ ] Legitimacy updates
- [ ] Red line violations
- [ ] Unit tests

### Phase 9: Integration
- [ ] Scenario loader
- [ ] MRIO coefficient loader
- [ ] Main simulation loop
- [ ] 52-tick stability test

## Commodity Aggregation

The 50 ISIC sectors aggregate into 10 game commodities for player-facing simplicity:

| Commodity | ISIC Sectors |
|-----------|--------------|
| Agriculture | A01, A02, A03 |
| Energy | B05, B06, D |
| Minerals | B07, B08, B09 |
| Manufacturing | C10T12 through C31T33 |
| Construction | F |
| Services | G through T (excluding transport) |
| Transport | H49, H50, H51, H52, H53 |
| Technology | C26, J61, J62_63 |
| Finance | K |
| Defense | Subset of C, O |

The full 50-sector technical coefficient matrix is used internally; aggregation happens only for UI/reporting.

## Coding Standards

- **Nullable Reference Types**: Enabled project-wide
- **LangVersion**: 12 (C# 12)
- **Target Framework**: net8.0
- **Hot Path Performance**: Use `Span<T>`, arrays, avoid LINQ allocations
- **Cold Path Clarity**: Prefer readability; LINQ acceptable
- **Commits**: Atomic, conventional format (`feat:`, `fix:`, `test:`, `docs:`)
- **No Git Sign-off**: Do not use `--signoff` or `Signed-off-by` trailers

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-17 | Use OECD ICIO 2022 as base data | Real-world technical coefficients ground the simulation |
| 2026-02-17 | 50 internal sectors, 10 game commodities | Full economic fidelity internally, simplified player interface |

## Current Phase

**Phase 1: Project Scaffolding** — In Progress

## Resume Points

To continue this project:
```
Continue building GeoSim (~/geosim/). Read CLAUDE.md for current phase and equations.md for specs. Pick up where the Decision Log and Phase checkboxes left off.
```
