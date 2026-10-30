# Wingwright: PC concept (parked for later)

> Parked. We start with the simple mobile version in `CONCEPT.md` to find the market first. This deep PC version may come later if the mobile game proves the audience.

**Pitch:** design real aircraft from real materials, build and upgrade their engines, then fly them through test campaigns to prove how far, how fast and how safely they go. Built on real aerospace engineering, so an aircraft engineer recognises every number.

**Working title only.** Check the name for trademarks and store availability before using it publicly.

## Who it's for

| Player | What they want |
|---|---|
| **Aerospace engineers and students** (core) | Real equations, real units, real trade-offs. A game that respects what they know. |
| Aviation enthusiasts and simmers | Flying something they designed themselves |
| Engineering-minded gamers (players of building and sandbox games) | Deep creative building with consequences |

**Promise:** if it works in the game, the engineering reason is real. If it fails, the game shows exactly why: the stress, the drag, the fuel.

## What makes it different

Vehicle-building sandboxes already exist. This one is different in five ways:

1. **Real engineering models, shown openly.** Every result can be opened in an "engineering notebook" that shows the equation, the inputs and the units. Examples: lift, drag build-up, Breguet range and spar bending stress.
2. **Materials you create.** Alloys, composites and heat treatments with real property data, and layups you design ply by ply.
3. **Engines as a design problem.** Choose the cycle (piston, turboprop, turbofan) and tune bypass ratio, pressure ratio and turbine temperature. Thrust, fuel burn, weight and cost follow.
4. **Requirements, like real programmes.** Customers issue a request for proposal: payload, range, runway, cost. You design to it and must prove it in test.
5. **Certification campaigns.** Structural load tests, stall tests, engine-out, flutter margin. Flight tests produce real telemetry you can export.

## Core loop

```
Read a requirement (RFP)
  → design the airframe: wing, fuselage, tail, structure, materials
  → choose and tune the engine
  → the analysis panel updates live: weight, drag, range, take-off distance, stability, cost
  → build a prototype, then fly the test campaign in 3D
  → telemetry and pass/fail per test, with the reason
  → improve the design, research new materials and engine tech
  → win the contract, sell aircraft, grow the company
```

## Systems

### 1. Materials lab

**Starting materials**, with real-world property values from published handbooks:
- metals: aluminium 2024-T3 and 7075-T6, titanium Ti-6Al-4V, steel 4130, and spruce
- composites: glass and carbon fibre

Each material has:
- density, Young's modulus, yield and ultimate strength
- fatigue limit, maximum service temperature
- cost per kg and how easy it is to manufacture

**Creating materials:**
- **Alloys:** research composition and heat-treatment steps. Properties come from simplified real relationships, inside realistic limits.
- **Composites:** fibre, resin, fibre volume fraction and ply angles (for example [0/±45/90]s). Stiffness comes from the rule of mixtures plus a simplified laminate model. Strength uses a first-ply-failure check.
- **Trade-offs are real:** carbon is light and stiff but costly and slow to make; aluminium is cheap and easy to repair.

### 2. Airframe designer (3D)

**Wing**
- Shape: span, root and tip chord, sweep, dihedral and twist.
- Airfoil: chosen from a generated NACA 4- and 5-digit family, or custom coordinates.
- Flaps and slats.
- **Structure:** spar caps and webs, ribs and skins, each with a material and a thickness. Sizing checks:
  - bending, shear and buckling at the limit load factor
  - with the standard 1.5 ultimate factor

**Other parts**
- **Fuselage:** length, diameter, nose and tail shape, cabin layout (seats, cargo), and the pressure cabin. Skin is checked for hoop stress.
- **Tail:** conventional, T-tail or V-tail, sized by tail volume coefficients. The game warns when they're outside normal ranges.
- **Landing gear:** layout and position. Tip-back and turnover angles are checked.
- **Weight:** structure from the sized parts, plus engines, systems (statistical estimates), payload and fuel. The centre of gravity is calculated for every loading case.

### 3. Engines

| Type | Inputs | Outputs |
|---|---|---|
| Piston + propeller | Displacement, compression ratio, rpm, propeller diameter and pitch | Power vs altitude, fuel flow, propeller efficiency |
| Turboprop | Pressure ratio, turbine temperature, gearbox | Shaft power, fuel use, weight |
| Turbofan | Bypass ratio, fan and overall pressure ratio, turbine entry temperature | Thrust vs speed and altitude, specific fuel consumption, weight, noise index |

- **The model:** a simplified real Brayton cycle with component efficiencies.
- **Upgrades:** new tech raises those efficiencies and the allowed turbine temperature, through materials such as single-crystal blades and thermal coatings.
- **Real limits:** engines get heavier and costlier as they get better, and hot sections wear faster.

### 4. Analysis (live while designing)

**Aerodynamics**
- Lift from finite-wing theory using aspect ratio and Oswald efficiency.
- Drag built up part by part: skin friction by Reynolds number, form factors, interference, induced drag, and wave drag at high subsonic speed.

**Performance**
- Stall speed, take-off and landing distance, climb rate, ceiling, maximum speed.
- **Range and endurance with the Breguet equations,** plus a mission profile: climb, cruise, descent and reserves.

**Stability and loads**
- **Stability:** neutral point, static margin, and simplified trim and control power checks.
- **Loads:** V-n diagram, gust loads, and a spanwise wing bending moment chart.

**Cost:** development, unit cost (from weight, materials and complexity), fuel cost per seat-km.

### 5. Flight testing (3D)

**The flight model:** a 6-degrees-of-freedom flight model driven by the same aerodynamic numbers as the analysis, so what you designed is what you fly. The pilot is the player, or autopilot for long range tests.

**Test cards**

| Test | Proves |
|---|---|
| Ground structural test | Wing holds 1.5 × limit load, and the bending shown is real |
| Stall and spin | Stall speed and behaviour; warning margin |
| Take-off and landing | Field length at maximum weight, hot day and high altitude |
| Climb and ceiling | Climb rate and service ceiling |
| Range flight | The real distance on the mission profile, with fuel left |
| Engine-out | Climb with one engine failed (multi-engine aircraft) |
| Flutter check | Speed margin from a simplified stiffness and mass check |

**Data:**
- **Telemetry** charts of speed, altitude, angle of attack, load factor, fuel and stresses, exportable as CSV.
- **Failures:** shown honestly, for example "Spar cap yielded at 2.9 g, 62% span, 7075-T6 at 18 mm".

### 6. Campaign and company

- **Story arc:** start as a small design office building trainers and crop sprayers, then grow to regional transports, business jets and finally a long-haul airliner.
- **Customers:** airlines, air forces (training and transport only), cargo operators and air ambulance services. Each issues requests for proposal with scoring for performance, cost and delivery date.
- **The business:**
  - **Research tree:** materials, engines, aerodynamics (winglets, supercritical airfoils), manufacturing.
  - **Money:** development budget, production cost and sales; loans and delays.
  - **Competitors** bid for the same requests for proposal.
- **Sandbox mode:** everything unlocked, no money limits.
- **Challenge mode:** weekly global challenges, for example "maximum range with 500 kg of fuel", with leaderboards.

### 7. Community

- Share designs: the design file plus its verified test results. Others can download, fly and modify them.
- Weekly challenge leaderboards. Results are recomputed on the server from the design file, so they can't be faked.
- Mod support later: custom airfoils, materials and engine parts as data files.

## Realism rules

- **SI units by default,** with imperial as an option. The notebook always shows units.
- **Models are simplified but valid.** Each is checked against known real aircraft in an automated test suite. Example: a Cessna 172-like design should predict range and stall speed within about ±10% of published figures. The tolerance is a target to confirm.
- **No real brands or trademarked aircraft names.** Generic types only (for example "4-seat trainer").
- **Military content stays non-combat:** trainers and transports. No weapons.

## Platform and business

**Decided:** Steam on PC, paid as a one-time purchase.


| Question | Recommendation | Why |
|---|---|---|
| Platform | **PC first (Steam)**, Mac later; tablets maybe later | Engineers want mouse, keyboard, large screens and deep panels; the simulation needs a PC's power |
| Engine | **Unity 6, URP** | Same tools and skills as the café game; good 3D; Steam builds supported |
| Model | **Premium game**, about $25–30, launched in **Early Access** | This audience pays once for depth and dislikes free-to-play mechanics |
| Payments | **Steam** sells it and pays the company | The shared platform records ownership via the Steam API and hosts designs, leaderboards and challenges |
| Later | Education licences for universities | Engineering schools are a natural second market |

## Biggest risks

| Risk | What we do |
|---|---|
| Realism makes it boring | Every screen gives an instant result; the 3D test flight is the reward; tutorials teach one concept at a time |
| Realism is wrong and engineers notice | The validation suite against real aircraft; a beta with aerospace engineers and students; published model notes |
| Niche audience | Steam wishlists and an engineering community before building too much; a demo with one aircraft class |
| Scope is huge | Start with one class: small propeller aircraft. Jets come in later Early Access updates |
| Similar games exist | Lead with what's unique: real materials, engine cycles, requests for proposal and certification, all shown in the notebook |

## First milestone (proposal)

**"Bush Plane" slice:**
- One request for proposal: a 4-seat utility aircraft for rough short airfields.
- Wing, fuselage and tail designer.
- 3 materials and the aluminium spar sizing check.
- Piston engine with a propeller.
- Live analysis panel.
- 3 test cards: structural, take-off, range.
- The 3D flight model and the notebook view.

**Goal:** 20 aerospace engineers and students play it and say the numbers feel right and they want more.
