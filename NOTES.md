# GeoSim Development Notes

Working notes from Claude during development. This captures reasoning, analysis, and decisions as they happen.

---

## 2026-02-17 — Project Kickoff

### MRIO Data Analysis

Examined the source data at `C:\Users\dougd\Downloads\2016-2022_SML.zip`. This contains OECD Inter-Country Input-Output tables from 2016-2022.

**File structure:**
- 7 CSV files, one per year (2016-2022)
- Each file ~87MB, ~616MB total uncompressed
- Matrix dimensions: ~4538 columns × ~4054 rows

**Matrix structure:**

The matrix follows OECD ICIO format:
- **Rows**: Country-sector combinations (e.g., `USA_C26` = US Computer/Electronics)
- **Columns**: Same country-sector codes for inter-industry flows, plus final demand
- **Special rows**: `V1` (header), `VA` (value added), `TLS` (taxes less subsidies), `OUT` (total output)

**Country coverage (81 total):**
AGO, ARE, ARG, AUS, AUT, BEL, BGD, BGR, BLR, BRA, BRN, CAN, CHE, CHL, CHN, CIV, CMR, COD, COL, CRI, CYP, CZE, DEU, DNK, EGY, ESP, EST, FIN, FRA, GBR, GRC, HKG, HRV, HUN, IDN, IND, IRL, ISL, ISR, ITA, JOR, JPN, KAZ, KHM, KOR, LAO, LTU, LUX, LVA, MAR, MEX, MLT, MMR, MYS, NGA, NLD, NOR, NZL, PAK, PER, PHL, POL, PRT, ROU, ROW, RUS, SAU, SEN, SGP, STP, SVK, SVN, SWE, THA, TUN, TUR, TWN, UKR, USA, VNM, ZAF

Note: ROW = Rest of World (aggregate for countries not individually modeled)

**Final demand categories:**
- `HFCE` — Household Final Consumption Expenditure
- `NPISH` — Non-Profit Institutions Serving Households
- `GGFC` — General Government Final Consumption
- `GFCF` — Gross Fixed Capital Formation (investment)
- `INVNT` — Changes in Inventories
- `DPABR` — Direct Purchases Abroad by Residents

**Sector codes (50 per country):**
Standard ISIC Rev 4 classification. Key sectors for simulation:
- Primary: A01-A03 (agriculture), B05-B09 (mining/energy)
- Manufacturing: C10T12 through C31T33
- Utilities: D (electricity/gas), E (water/waste)
- Services: F through T

### Design Decisions Made

1. **Use full 50-sector matrix internally**: The technical coefficients from real IO data capture actual production relationships (e.g., auto manufacturing needs steel, rubber, electronics). Simplifying to 10 sectors would lose important interdependencies.

2. **10 aggregate commodities for game UI**: Players shouldn't manage 50 sectors. Aggregation happens at the presentation layer, not in the simulation core.

3. **Store coefficients as 2D array**: The technical coefficient matrix A where A[i,j] = amount of input i needed per unit of output j. This is computed from the MRIO by dividing each column by total output.

4. **Weekly tick as base unit**: One game year = 52 ticks. Allows sub-quarterly dynamics while keeping computational load reasonable.

### Next Steps

1. Create docs/equations.md with formal equation specs
2. Create .gitignore
3. Scaffold .NET solution
4. Build data model types

### Questions to Address Later

- How to handle missing data in MRIO? (Some country-sector combinations may be zero or sparse)
- Should we interpolate between years or use single-year snapshot?
- How to model ROW interactions? (Treat as single mega-country or distribute to neighbors?)

---

## Decisions Made While Writing equations.md

### Decision: Soft Leontief instead of Hard Leontief

**Problem**: Standard Leontief production is Q = min(inputs/coefficients). This means if ANY single input is missing, output goes to zero. That's economically accurate but terrible for gameplay — one supply chain disruption would instantly crash entire economies.

**Decision**: Use a "soft" Leontief that blends bottleneck with average:
```
φ(I) = α·min(satisfaction) + (1-α)·mean(satisfaction)
```

**Rationale**:
- With α=0.7, a 50% shortage of one input reduces output to ~65% instead of 50%
- Still penalizes bottlenecks significantly (they dominate)
- Prevents cascade failures from single-point disruptions
- Can tune α per scenario (peacetime α=0.6, wartime α=0.8 for harsher bottlenecks)

**Trade-off**: Less realistic, but playable. Real economies DO have substitution and workarounds that pure Leontief ignores anyway.

### Decision: Weekly ticks, monthly fiscal

**Problem**: Need to balance simulation resolution against computational cost and gameplay pacing.

**Decision**: Base tick = 1 week. Production, prices, trade, labor run weekly. Fiscal and political run monthly (every 4 ticks).

**Rationale**:
- Weekly production allows capturing supply shocks, stockpile dynamics
- Monthly fiscal matches real tax collection cycles
- 52 ticks/year is manageable (can simulate decades in seconds)
- Political changes shouldn't swing wildly week-to-week anyway

### Decision: Price bounds at 0.1x to 10x initial

**Problem**: Price dynamics can explode to infinity or collapse to zero, breaking the simulation.

**Decision**: Hard bounds at [0.1·P₀, 10·P₀].

**Rationale**:
- 10x price increase represents severe crisis (hyperinflation territory)
- 0.1x floor prevents free goods breaking production value
- Real economies rarely see prices move more than 10x over simulation-relevant timescales
- If bounds are hit repeatedly, it signals scenario imbalance — intentional design feedback

### Decision: Debt risk premium threshold at 60%

**Problem**: Need a debt sustainability mechanic that creates fiscal pressure without arbitrary game-over.

**Decision**: Interest rate premium kicks in above 60% debt/GDP, scaling linearly.

**Rationale**:
- 60% is Maastricht Treaty threshold — real-world "danger zone"
- Premium creates escalating pressure without hard failure
- Countries CAN run higher debt but pay for it
- Allows player recovery strategies (austerity, growth, inflation)

### Decision: Keep full 50 sectors in simulation, aggregate for display

**Problem**: 50 ISIC sectors is too granular for player decisions, but aggregating loses information.

**Decision**: Full 50-sector matrix runs internally. The 10 "game commodities" are pure UI aggregation.

**Rationale**:
- Technical coefficients capture real production relationships (autos need steel, rubber, electronics)
- Aggregating to 10 sectors would lose these interdependencies
- Players see simplified view; simulation runs full fidelity
- Can later add "expert mode" exposing full sector detail

### Decision: Faction red lines as discrete events

**Problem**: How to model political constraints that cause regime change?

**Decision**: Red lines are policy thresholds. Crossing them triggers discrete legitimacy penalties scaled by faction power.

**Rationale**:
- Binary thresholds are clear to players ("don't raise taxes above X")
- Scaling by faction power makes small factions ignorable, large factions dangerous
- Avoids continuous "legitimacy drain" that's hard to diagnose
- Creates dramatic moments: "the military has lost confidence in your leadership"
