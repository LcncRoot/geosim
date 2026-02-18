# GeoSim Equation Specification

This document is the authoritative reference for all simulation equations. Systems implement exactly what is specified here. Any deviation must be documented in CLAUDE.md Decision Log.

## Notation Conventions

| Symbol | Meaning |
|--------|---------|
| $c$ | Country index |
| $r$ | Region index (within country) |
| $s$ | Sector index (1-50 ISIC) |
| $t$ | Time tick (weekly) |
| $\Delta t$ | Time step (1 week = 1/52 year) |

## §1 Data Structures

### §1.1 Technical Coefficient Matrix

The technical coefficient matrix $A$ is derived from MRIO data:

$$
a_{ij} = \frac{Z_{ij}}{X_j}
$$

Where:
- $Z_{ij}$ = intermediate flow from sector $i$ to sector $j$ (in monetary units)
- $X_j$ = total output of sector $j$
- $a_{ij}$ = units of input $i$ required per unit of output $j$

The matrix is stored as `double[numSectors, numSectors]` per country, with cross-country trade handled separately.

### §1.2 Final Demand Components

Total final demand $F_s$ for sector $s$:

$$
F_s = C_s + G_s + I_s + \Delta INV_s + X_s - M_s
$$

Where:
- $C_s$ = Household consumption (HFCE + NPISH)
- $G_s$ = Government consumption (GGFC)
- $I_s$ = Investment (GFCF)
- $\Delta INV_s$ = Inventory changes (INVNT)
- $X_s$ = Exports
- $M_s$ = Imports

---

## §2 Production System

### §2.1 Leontief Production Function

Output of sector $s$ in region $r$ at time $t$:

$$
Q_{r,s,t} = \min\left(K_{r,s}, \frac{L_{r,s}}{\ell_s}, \phi(\mathbf{I}_{r,s})\right) \cdot \eta_{r} \cdot \epsilon_{s,t}
$$

Where:
- $K_{r,s}$ = installed capacity (capital stock)
- $L_{r,s}$ = labor employed in sector
- $\ell_s$ = labor coefficient (workers per unit output)
- $\phi(\mathbf{I})$ = input availability function (soft Leontief)
- $\eta_r$ = regional infrastructure modifier ∈ [0.5, 1.5]
- $\epsilon_{s,t}$ = efficiency/technology factor

### §2.2 Soft Leontief Input Function

Standard Leontief is too brittle for gameplay (single missing input → zero output). We use a softened version:

$$
\phi(\mathbf{I}) = \alpha \cdot \min_i\left(\frac{I_i}{a_{is} \cdot Q^{target}}\right) + (1-\alpha) \cdot \frac{1}{n}\sum_i\left(\frac{I_i}{a_{is} \cdot Q^{target}}\right)
$$

Where:
- $I_i$ = available quantity of input $i$
- $a_{is}$ = technical coefficient (input $i$ per unit output $s$)
- $Q^{target}$ = target output level
- $\alpha$ = bottleneck dominance factor ∈ [0.6, 0.9] (default: 0.7)
- $n$ = number of required inputs

**Interpretation**: Output is weighted average of worst bottleneck and average input satisfaction. With α=0.7, a single input at 50% availability reduces output to roughly 65% rather than 50%.

### §2.3 Value Added

Value added by sector:

$$
VA_{r,s,t} = P_s \cdot Q_{r,s,t} - \sum_i a_{is} \cdot P_i \cdot Q_{r,s,t}
$$

Where $P_s$ is the price of sector $s$ output.

### §2.4 Input Consumption

When production runs, inputs are consumed:

$$
\text{Consumed}_i = a_{is} \cdot Q_{r,s,t} \cdot \min\left(1, \frac{I_i^{available}}{I_i^{required}}\right)
$$

Partial input availability leads to partial consumption, preventing negative inventories.

---

## §3 Price System

### §3.1 Market Clearing Price Adjustment

Prices adjust based on excess demand:

$$
P_{s,t+1} = P_{s,t} \cdot \left(1 + \sigma_s \cdot \text{clamp}\left(\frac{D_s - S_s}{S_s}, -\delta_{max}, \delta_{max}\right)\right)
$$

Where:
- $D_s$ = total demand for sector $s$ (intermediate + final)
- $S_s$ = total supply (production + imports + inventory drawdown)
- $\sigma_s$ = price sensitivity ∈ [0.01, 0.1] (sector-specific)
- $\delta_{max}$ = maximum price change per tick (default: 0.05 = 5%)

### §3.2 Price Smoothing

To prevent oscillation, apply exponential smoothing:

$$
P_{s,t+1}^{smooth} = \beta \cdot P_{s,t+1} + (1-\beta) \cdot P_{s,t}
$$

Where $\beta$ ∈ [0.3, 0.7] (default: 0.5).

### §3.3 Consumer Price Index

CPI for country $c$:

$$
CPI_{c,t} = \frac{\sum_s w_s \cdot P_{s,t}}{\sum_s w_s \cdot P_{s,0}}
$$

Where $w_s$ is the consumption basket weight for sector $s$, derived from HFCE shares in MRIO data.

### §3.4 Price Bounds

Prices are bounded to prevent explosion:

$$
P_s \in [P_s^{floor}, P_s^{ceiling}]
$$

Where:
- $P_s^{floor} = 0.1 \cdot P_s^{initial}$
- $P_s^{ceiling} = 10 \cdot P_s^{initial}$

---

## §4 Trade System

### §4.1 Bilateral Trade Flow

Trade flow from country $c$ to country $c'$ for sector $s$:

$$
T_{c \to c',s} = T_{c \to c',s}^{base} \cdot \left(\frac{P_{s,c'}}{P_{s,c} \cdot (1 + \tau_{c',c,s})}\right)^{\gamma}
$$

Where:
- $T^{base}$ = baseline trade volume from MRIO data
- $\tau_{c',c,s}$ = tariff rate imposed by $c'$ on imports from $c$
- $\gamma$ = trade elasticity ∈ [1.0, 3.0] (default: 2.0)

### §4.2 Trade Balance

Trade balance for country $c$:

$$
TB_c = \sum_s \sum_{c' \neq c} P_s \cdot T_{c \to c',s} - \sum_s \sum_{c' \neq c} P_s \cdot (1 + \tau_{c,c',s}) \cdot T_{c' \to c,s}
$$

### §4.3 Foreign Exchange Reserves

$$
FX_{c,t+1} = FX_{c,t} + TB_c \cdot \Delta t - \text{DebtService}_c
$$

---

## §5 Labor System

### §5.1 Labor Demand

Labor demand by sector:

$$
L_{s,t}^{demand} = \ell_s \cdot Q_{s,t}^{target}
$$

Where $\ell_s$ is the labor coefficient from MRIO (compensation of employees / output).

### §5.2 Employment

Actual employment is constrained by labor supply:

$$
L_{s,t} = \min\left(L_{s,t}^{demand}, L_s^{available}\right)
$$

Labor allocation across sectors follows wage differentials with friction.

### §5.3 Unemployment Rate

$$
U_{c,t} = 1 - \frac{\sum_s L_{s,t}}{LF_c}
$$

Where $LF_c$ is total labor force of country $c$.

### §5.4 Wage Dynamics

Sector wages adjust based on labor market tightness:

$$
W_{s,t+1} = W_{s,t} \cdot \left(1 + \omega \cdot \left(\frac{L_s^{demand}}{L_s^{supply}} - 1\right)\right)
$$

Where $\omega$ ∈ [0.01, 0.05] is wage adjustment speed.

---

## §6 Fiscal System

### §6.1 Tax Revenue

Total tax revenue:

$$
R_c = R_c^{income} + R_c^{corporate} + R_c^{tariff} + R_c^{VAT}
$$

Components:
- $R^{income} = \tau^{inc} \cdot \sum_s W_s \cdot L_s$
- $R^{corporate} = \tau^{corp} \cdot \sum_s \max(0, VA_s - W_s \cdot L_s)$
- $R^{tariff} = \sum_s \sum_{c'} \tau_{c,c',s} \cdot P_s \cdot T_{c' \to c,s}$
- $R^{VAT} = \tau^{VAT} \cdot C_{total}$

### §6.2 Government Spending

$$
G_c = G_c^{consumption} + G_c^{transfers} + G_c^{investment} + G_c^{military} + G_c^{interest}
$$

Where $G^{interest} = i \cdot D_c$ (interest rate times debt).

### §6.3 Budget Balance

$$
B_c = R_c - G_c
$$

### §6.4 Debt Dynamics

$$
D_{c,t+1} = D_{c,t} - B_c \cdot \Delta t
$$

(Negative balance increases debt.)

### §6.5 Debt Sustainability

Debt-to-GDP ratio:

$$
d_c = \frac{D_c}{GDP_c}
$$

Interest rate premium for high debt:

$$
i_c = i^{base} + \max(0, \kappa \cdot (d_c - d^{threshold}))
$$

Where $d^{threshold}$ ≈ 0.6 (60% debt/GDP) and $\kappa$ ≈ 0.02.

---

## §7 Political System

### §7.1 Faction Satisfaction

Satisfaction of faction $f$ in country $c$:

$$
S_{f,t} = \sum_k \omega_{f,k} \cdot u_k(x_{k,t})
$$

Where:
- $\omega_{f,k}$ = weight faction $f$ places on policy dimension $k$
- $x_{k,t}$ = current value of dimension $k$ (tax rate, trade openness, etc.)
- $u_k$ = utility function for dimension $k$ (linear or piecewise)

### §7.2 Government Legitimacy

$$
L_{c,t+1} = L_{c,t} + \lambda \cdot \left(\bar{S}_t - L_{c,t}\right) - \text{shocks}
$$

Where:
- $\bar{S}_t$ = power-weighted average faction satisfaction
- $\lambda$ ∈ [0.05, 0.2] = legitimacy adjustment speed
- shocks = discrete events (scandals, crises)

### §7.3 Red Line Violations

Each faction $f$ has red lines — policy positions that trigger severe consequences:

$$
\text{Violation}_f = \mathbf{1}[x_k < x_k^{red,f}]
$$

Consequences scale with faction power $\rho_f$:
- $\rho_f > 0.3$: Legitimacy drops 10-20 points
- $\rho_f > 0.5$: Risk of government collapse

### §7.4 Faction Power Dynamics

Faction power shifts based on economic conditions:

$$
\rho_{f,t+1} = \rho_{f,t} + \mu \cdot \rho_{f,t} \cdot (S_{f,t} - \bar{S}_t)
$$

Power is normalized: $\sum_f \rho_f = 1$.

---

## §8 Military System (Placeholder)

### §8.1 Military Strength

$$
M_c = \sum_u \text{Strength}_u \cdot \text{Readiness}_u
$$

### §8.2 Supply Requirements

Military units consume supplies from production:

$$
\text{MilitaryDemand}_s = \sum_u m_{u,s} \cdot \text{Active}_u
$$

Where $m_{u,s}$ = sector $s$ consumption per unit of formation $u$.

---

## §9 Aggregated GDP Calculation

### §9.1 GDP by Expenditure

$$
GDP_c = C_c + I_c + G_c + (X_c - M_c)
$$

### §9.2 GDP by Production

$$
GDP_c = \sum_s VA_s
$$

Both methods should approximately equal if the simulation is balanced.

---

## §10 Tick Schedule

| System | Frequency | Rationale |
|--------|-----------|-----------|
| Production | Weekly | Core economic loop |
| Price | Weekly | Market clearing |
| Trade | Weekly | International flows |
| Labor | Weekly | Employment adjustment |
| Fiscal | Monthly (4 ticks) | Tax collection/spending |
| Political | Monthly (4 ticks) | Satisfaction updates |
| Military | Weekly | Supply consumption |
| Elections | Triggered | Event-based |

---

## Appendix A: Parameter Defaults

| Parameter | Symbol | Default | Range |
|-----------|--------|---------|-------|
| Bottleneck dominance | α | 0.7 | [0.6, 0.9] |
| Price sensitivity | σ | 0.03 | [0.01, 0.1] |
| Max price change | δ_max | 0.05 | [0.02, 0.10] |
| Price smoothing | β | 0.5 | [0.3, 0.7] |
| Trade elasticity | γ | 2.0 | [1.0, 3.0] |
| Wage adjustment | ω | 0.02 | [0.01, 0.05] |
| Legitimacy adjustment | λ | 0.1 | [0.05, 0.2] |
| Debt threshold | d_thresh | 0.6 | [0.4, 0.8] |
| Debt risk premium | κ | 0.02 | [0.01, 0.05] |

---

## Appendix B: MRIO Data Mapping

The OECD ICIO data provides:

| MRIO Field | Simulation Use |
|------------|----------------|
| Inter-industry flows (Z) | Technical coefficients A = Z / X |
| Value added row (VA) | Initial VA shares, labor coefficients |
| HFCE columns | Consumption basket weights |
| GGFC columns | Government consumption structure |
| GFCF columns | Investment structure |
| Total output (OUT) | Initial production levels |

Loading procedure:
1. Parse CSV into raw flow matrix
2. Compute technical coefficients per country: A_c[i,j] = Z_c[i,j] / X_c[j]
3. Extract final demand structure
4. Initialize sector capacities from output levels
