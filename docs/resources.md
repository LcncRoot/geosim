# GeoSim: Resource Deposits & Extraction Facilities
## Supplement to equations.md

---

## 1. Resource Deposit Model

Each hex on the map can contain zero or more **resource deposits**. A deposit is a fixed
geographic feature — it exists whether or not you exploit it.

```
ResourceDeposit:
    hex_id:          int              // which hex on the map
    resource_type:   Commodity        // what commodity it produces (Food, Energy, Metals, etc.)
    subtype:         string           // specific resource (e.g. "anthracite", "tungsten", "rice_paddy")
    total_reserves:  long             // total extractable units (depletes over time)
    remaining:       long             // current remaining reserves
    base_yield:      double           // units extractable per facility level per tick
    difficulty:      double [0.5-2.0] // extraction cost multiplier (deep mines = high, surface = low)
    discovery_state: enum             // Unknown, Surveyed, Proven
```

Deposits that are `Unknown` must be found via geological survey (costs money, takes time).
`Surveyed` deposits have estimated reserves (±30% accuracy). `Proven` deposits have accurate data.

---

## 2. Extraction Facility Model

To turn a deposit into commodity flow, you must **build a facility** on the hex.

```
ExtractionFacility:
    hex_id:                int
    deposit_id:            int            // which deposit this facility extracts
    facility_type:         string         // "coal_mine", "oil_refinery", "rice_farm", "fishing_port", etc.
    level:                 int [0-5]      // upgrade level (0 = not built)
    condition:             double [0-1]   // degrades without maintenance, destroyed by combat
    workers:               int            // current labor allocated
    workers_required:      int            // labor needed for full operation at this level
    build_cost:            CommodityBundle // what it costs to build/upgrade (IndustrialGoods, Metals, Construction, money)
    build_time:            int            // ticks to construct
    maintenance_cost:      CommodityBundle // per-tick upkeep (Energy, money, spare parts)
    under_construction:    bool
    construction_progress: double [0-1]
```

---

## 3. Extraction Output Equation

```
extraction_output(facility, t) =
    deposit.base_yield
    * facility.level
    * workforce_factor(facility)
    * condition_factor(facility.condition)
    * infrastructure_factor(hex, t)
    * technology_modifier(country, deposit.resource_type)

workforce_factor = min(1.0, facility.workers / facility.workers_required)
condition_factor = facility.condition ^ 0.5   // degrades gracefully, not linearly

// Depletion:
deposit.remaining -= extraction_output
if deposit.remaining <= 0: extraction_output = 0 (deposit exhausted)
```

The extraction output feeds directly into the Production system as **domestic supply** of that
commodity in that region:

```
domestic_raw_supply(r, commodity, t) = sum over facilities in region r:
    extraction_output(facility, t)

// This becomes an INPUT to downstream manufacturing sectors via the IO matrix
```

For **extractive sectors** (Food, Energy, Metals), the capital stock K(r,s,t) in the production
function from equations.md §2.1 is determined by extraction facility levels on resource
deposits in the region.

For **manufacturing sectors** (Electronics, IndustrialGoods, MilitaryGoods, etc.), K is
determined by built factory capacity (investment), NOT by resource deposits.

---

## 4. Facility Construction

Building a facility requires:
1. **Commodities**: IndustrialGoods, Metals, Construction (consumed from stockpile/market)
2. **Money**: Government or private investment (comes from budget or corporate profits)
3. **Labor**: Construction workers (drawn from labor pool during build phase)
4. **Time**: Multiple ticks depending on facility type and level

```
build_cost(facility_type, target_level) =
    base_cost(facility_type) * level_scaling(target_level) * difficulty(deposit)

level_scaling(level) = level ^ 1.5   // each upgrade costs more than the last

// Construction progress per tick:
progress_per_tick = construction_workforce / total_workforce_needed
                  * material_satisfaction(available_materials / required_materials)
                  * infrastructure_factor(hex)
```

The player decides WHERE to build (which hex/deposit), WHAT to build (facility type),
and WHEN to upgrade. This is the core strategic investment decision.

---

## 5. Facility Degradation and Destruction

```
// Without maintenance:
condition(t+1) = condition(t) - degradation_rate(facility_type)
// Typical degradation: 0.01/tick (facilities fall apart over ~100 ticks without upkeep)

// With maintenance:
maintenance_satisfaction = min(1.0, maintenance_delivered / maintenance_required)
condition(t+1) = min(1.0, condition(t) - degradation_rate + repair_rate * maintenance_satisfaction)

// Combat damage:
if hex under bombardment or captured:
    condition -= combat_damage_factor * attack_intensity
    // Severe damage can set condition to 0 (facility destroyed, must rebuild)
```

This is why war is economically devastating — capturing a mining region doesn't just
change the border, it destroys the facilities that made the region valuable. Rebuilding
takes years and resources.

---

## 6. Geological Survey System

Players can invest in geological surveys to discover Unknown deposits:

```
survey_cost = base_survey_cost * hex_area * difficulty_terrain
survey_time = base_survey_time * difficulty_terrain
survey_success_probability = 0.3 + 0.5 * (survey_investment / optimal_investment)

// On success: deposit.discovery_state → Surveyed (with ±30% reserve estimate)
// Additional investment can upgrade to Proven (accurate reserves)
```

This adds an exploration mechanic: you might discover a deposit that changes your
strategic calculus, or you might invest in surveys and find nothing.

---

## 7. Stockpile System

### 7.1 Strategic Reserves

Unlike Victoria 3 (which has no stockpiles), this sim allows **bounded commodity stockpiles**.
This is essential for blockade gameplay and strategic planning.

```
stockpile(c, commodity, t+1) = stockpile(c, commodity, t)
    + production(c, commodity, t)
    + imports(c, commodity, t)
    - consumption(c, commodity, t)
    - exports(c, commodity, t)
    - spoilage(commodity, stockpile_size)

// Bounded:
stockpile = clamp(stockpile, 0, max_stockpile_capacity(c, commodity))
```

Spoilage rates by commodity:
- Food: 2%/tick (perishable — can't hoard indefinitely)
- Energy: 0.5%/tick (stored fuel slowly degrades/leaks)
- Metals: 0.1%/tick (essentially permanent)
- MilitaryGoods: 0.5%/tick (equipment degrades in storage)
- Electronics: 1%/tick (obsolescence)
- Services: 100%/tick (services can't be stockpiled — use it or lose it)

### 7.2 Strategic Reserve Policy (Player Decision)

The player can set a **reserve target** for critical commodities:

```
reserve_target(c, commodity) = X days of consumption

// System automatically diverts production/imports to reserves until target met
// Costs money (storage facilities) and ties up commodities that could be consumed
reserve_maintenance_cost = stockpile_size * storage_cost_per_unit(commodity)
```

South Korea real-world reference: KORES maintains a 60-day supply of 10 strategic metals.
The Korean government maintains a ~90-day strategic petroleum reserve.

---

## 8. Real Resource Placement — South Korea

Based on USGS Minerals Yearbook, Korean National Atlas, mindat.org, and mining records.

### South Korea (resource-poor — this scarcity IS the gameplay tension)

| Province/Region | Deposit Subtype | Commodity | Difficulty | Notes |
|----------------|-----------------|-----------|------------|-------|
| Gangwon (Sangdong) | Tungsten (scheelite/skarn) | Metals | 1.5 | One of world's largest W deposits. Operated 1947-1992, reopening under Almonty Industries. Deep underground mine. |
| Gangwon (Taebaek) | Anthracite coal | Energy | 1.3 | Declining production, small-scale. Korea's only significant domestic coal. |
| South Jeolla (Naju Plain) | Rice paddies | Food | 0.7 | Major rice-growing region. Flat alluvial plains. |
| North Jeolla (Honam Plain) | Rice paddies, barley, soybeans | Food | 0.7 | Agricultural heartland of South Korea. |
| South Chungcheong | Rice paddies | Food | 0.8 | Secondary agricultural zone. |
| South Gyeongsang (Busan coast) | Fisheries | Food | 0.8 | Major fishing fleet, seafood processing. |
| Jeju | Fisheries (offshore) | Food | 0.9 | Smaller scale, tourism competes for coast. |
| Ulsan / South Gyeongsang | Oil refining complex | Energy | 1.0 | NO crude deposits — processes imported oil. SK Energy, S-Oil, GS Caltex refineries. Treat as manufacturing facility, not resource extraction. |
| Offshore East Sea | Minor natural gas (Donghae-1) | Energy | 1.8 | Small, mostly depleted. Discovered 1998, declining. |
| North Chungcheong | Limestone | Construction | 0.6 | Abundant. Cement production feedstock. |
| North Chungcheong | Graphite | Metals | 1.2 | Minor deposits. |
| Gangwon (various) | Gold, silver (trace) | Metals | 1.6 | Historically mined, mostly uneconomic now. |
| Gunsan (west coast) | KORES strategic stockpile | — | — | Not a deposit — government-maintained 60-day supply of 10 rare metals (antimony, chromium, gallium, molybdenum, niobium, rare earths, selenium, titanium, tungsten, zirconium). Model as starting stockpile, not extraction. |

**Key insight:** South Korea has almost no oil, gas, iron, or copper. Its "resources" are
its industrial facilities (refineries, semiconductor fabs, shipyards) and human capital —
NOT extractable deposits. The player must IMPORT raw materials and BUILD manufacturing
facilities. Resource scarcity drives the entire strategic calculus.

### North Korea (resource-rich, infrastructure-poor)

| Region | Deposit Subtype | Commodity | Difficulty | Notes |
|--------|-----------------|-----------|------------|-------|
| South Hamgyong (Tanchon) | Magnesite | Metals | 1.3 | 2nd largest reserves in world (~6 billion tonnes). Multiple mine complexes. |
| North/South Hwanghae | Iron ore (high grade) | Metals | 1.0 | Major deposits, relatively accessible. |
| Musan (North Hamgyong) | Iron ore (low grade) | Metals | 1.2 | Largest single mine on peninsula. Enormous volume but lower quality. |
| Pyongan (Anju basin) | Anthracite coal | Energy | 0.8 | Largest coal deposits on peninsula. Along Taedong River. |
| Yanggang (Paegam) | Anthracite + lignite | Energy | 1.4 | Remote, harsh climate, poor transport links. |
| South Hamgyong (Komdok) | Zinc, lead | Metals | 1.1 | Largest zinc mine in East Asia. Operating since 1932. |
| Hamgyong (various) | Copper | Metals | 1.3 | Multiple deposits. Hyesan mine largest (flooded, partially reopened). |
| Pyongan (various) | Gold | Metals | 1.5 | Scattered deposits. Daebong mine historically >150kg/year. |
| Various | Tungsten, molybdenum | Metals | 1.2 | 6th largest tungsten reserves globally. Exported to China. |
| Various | Rare earth elements | Metals | 1.8 | Poorly surveyed. Estimated massive reserves. High difficulty = need foreign tech. |
| Various | Graphite | Metals | 1.0 | Significant deposits. Used in batteries, steel, nuclear. |

**North Korea gameplay implications:**
- Resources are enormous but infrastructure is collapsed (roads, rail, power grid)
- Most mines operate at fraction of capacity due to electricity shortages
- Reunification or economic opening would unlock massive mineral wealth
- BUT requires decade-long infrastructure investment to exploit
- China has existing mining investments/relationships in the North
- UN sanctions currently restrict mineral exports (except tungsten/molybdenum)

### Other Countries (simplified — trade nodes, not hex-level resources)

| Country | Key Resource Endowments | Commodity | Trade Role |
|---------|------------------------|-----------|------------|
| China | Rare earths (dominant global supplier), coal (largest producer), iron ore, bauxite, tungsten | Metals, Energy | Primary supplier of raw materials to South Korea |
| Japan | Minimal natural resources (similar to South Korea) | — | Competitor for imports, manufacturing peer |
| USA | Oil/gas (net exporter), agricultural (massive), diverse minerals, coal | Energy, Food, Metals | Self-sufficient, strategic ally, LNG supplier |
| Taiwan | Minimal natural resources | — | Semiconductor manufacturing peer, not resource supplier |

---

## 9. Manufacturing Facilities (Non-Extractive)

Not all facilities extract from deposits. Manufacturing facilities transform imported/domestic
raw materials into higher-value goods. These are placed on hexes based on infrastructure
and labor, NOT on resource deposits.

| Facility Type | Input Commodities | Output Commodity | Placement Requirement |
|--------------|-------------------|------------------|----------------------|
| Semiconductor fab | Metals, Chemicals, Energy, Electronics | Electronics | High infrastructure, skilled labor |
| Shipyard | Metals, IndustrialGoods, Energy | IndustrialGoods (+ military variant) | Coastal hex, port infrastructure |
| Steel mill | Metals (iron ore), Energy (coal/electricity) | Metals (processed) | Heavy infrastructure, energy access |
| Oil refinery | Energy (crude imports) | Energy (refined products) | Coastal hex (tanker access), infrastructure |
| Automotive plant | Metals, IndustrialGoods, Electronics, Energy | ConsumerGoods / IndustrialGoods | Infrastructure, skilled labor |
| Arms factory | Metals, IndustrialGoods, Electronics, Chemicals | MilitaryGoods | Security clearance, government investment |
| Petrochemical plant | Energy (naphtha), Chemicals | Chemicals | Energy access, infrastructure |
| Textile factory | Chemicals, Energy | ConsumerGoods | Labor (can be lower-skill) |

These use the same facility model (level, condition, workers, build cost, maintenance) but
their capacity comes from capital investment, not from resource deposits in the ground.

South Korea's real industrial geography:
- **Gyeonggi / Seoul metro**: Electronics, services, R&D
- **Ulsan**: Oil refining, automotive (Hyundai), shipbuilding
- **Busan**: Port, shipbuilding, logistics
- **Changwon**: Defense industry (Hanwha, LIG Nex1), heavy machinery
- **Icheon/Pyeongtaek**: Semiconductor fabs (Samsung, SK Hynix)
- **Yeosu/Gwangyang**: Petrochemicals, steel (POSCO)
- **Geoje Island**: Shipbuilding (HD Hyundai, Samsung Heavy)
