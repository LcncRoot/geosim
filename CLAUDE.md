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
- [x] .NET 8.0 solution structure
- [x] Directory structure created
- [x] Nullable reference types enabled
- [x] Initial commit

### Phase 2: Data Model
- [x] Commodity enum (10 game commodities)
- [x] Country class
- [x] Region class
- [x] Sector struct
- [x] Faction class with red lines
- [x] TradeRelation class
- [x] PopulationCohort class with consumption curves
- [x] MilitaryFormation class
- [x] TechnicalCoefficientMatrix class
- [x] ScenarioConfig class (JSON-loadable)
- [x] SimulationState class
- [x] Resource system: ResourceDeposit, ExtractionFacility, ManufacturingFacility
- [x] Unit tests for data model

### Phase 3: Production System
- [x] Leontief production function
- [x] Soft Leontief bottleneck smoothing (α = 0.6)
- [x] Labor constraint
- [x] Infrastructure modifier
- [x] Value added calculation
- [x] Extraction facility output
- [x] Unit tests (10 cases)

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

The 50 ISIC sectors aggregate into 10 game commodities:

| ID | Commodity | OECD ICIO Mapping |
|----|-----------|-------------------|
| 0 | Food | Agriculture + Food products |
| 1 | Energy | Mining (oil/gas/coal) + Utilities |
| 2 | Metals | Basic metals + Metal products |
| 3 | Chemicals | Chemicals + Pharma |
| 4 | IndustrialGoods | Machinery + Electrical equipment |
| 5 | Electronics | Computer/electronic/optical (semiconductors) |
| 6 | ConsumerGoods | Textiles + Other manufacturing |
| 7 | MilitaryGoods | Derived from IndustrialGoods + Electronics |
| 8 | Services | All service sectors |
| 9 | Construction | Construction |

See `docs/equations.md` for full simulation equations and `docs/resources.md` for resource/facility system.

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
| 2026-02-17 | 10 game commodities (Food, Energy, Metals, etc.) | Aligned with equations.md spec |
| 2026-02-17 | Sector as struct, Country/Region as class | Sectors are small/hot-path; Countries have identity |
| 2026-02-17 | Integer IDs over string keys | O(1) array access for hundreds of countries |
| 2026-02-17 | Resource deposits + facilities model | Separate extraction (on deposits) from manufacturing (capital investment) |
| 2026-02-17 | Stockpiles with spoilage | Enable blockade gameplay; Services can't stockpile |

## Current Phase

**Phase 4: Price System** — Next

## Resume Points

To continue this project:
```
Continue building GeoSim (~/geosim/). Read CLAUDE.md for current phase and equations.md for specs. Pick up where the Decision Log and Phase checkboxes left off.
```
