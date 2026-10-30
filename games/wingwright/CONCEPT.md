# Wingwright: mobile concept (working title)

**Pitch:** fly your plane as far as it can go, then upgrade it with real engineering. Every flight teaches you why you landed where you did.

| | |
|---|---|
| Platform | iOS and Android, portrait |
| Business model | Free to play: rewarded ads and in-app purchases |
| Engine | Unity, the same tools and shared platform as the café game |
| Look | Clean, bright 3D, simple shapes, satisfying flight |
| Session | 1–3 minutes per flight |
| Goal of version 1 | **Find the market** with a small, cheap game before building anything deep |

The deep PC version is parked in `CONCEPT_PC_LATER.md`.

## The core loop

```
Launch → fly as far as you can → land (or run out of fuel)
  → see the distance, the coins earned and an engineer's note on WHY
  → upgrade one part: wing, engine, propeller, fuel tank, body material, landing gear
  → fly again, further
  → reach the next city on the route map → unlock the next aircraft
```

This upgrade-and-fly-further loop is a proven mobile genre. What makes ours different is that **every upgrade follows real engineering**, and the game shows the trade-off in plain words.

## How a flight works

A real but simple model of a plane seen from the side:

| Force | Real formula (kept in code, shown simply in game) |
|---|---|
| Lift | L = ½ ρ V² S C_L |
| Drag | D = ½ ρ V² S (C_D0 + C_L² / (π e AR)) |
| Thrust | From the engine's power and the propeller's efficiency |
| Weight | Empty weight + fuel; gets lighter as fuel burns |
| Fuel burn | From the engine's fuel consumption at the power used |

**Phases of a flight**
1. **Take-off run:** a longer runway is needed if the plane is heavy or the wing is small.
2. **Climb:** the player holds to pitch up and releases to level off.
3. **Cruise:** fly until the fuel runs out.
4. **Glide:** a better wing glides further.
5. **Landing:** a soft landing gives bonus coins; a hard one costs a repair.

**Skill matters a little:** flying at the best speed for the plane goes further. A small green band on the speed indicator shows the best-range speed, which is real engineering presented as a target. Pulling too hard can overstress the wing: it bends, and past its limit it breaks.

## Upgrades with real trade-offs

Each part has 5–10 levels. Better parts usually add weight or drag somewhere else, so players must think a little.

| Part | Improves | Costs you | Real idea taught |
|---|---|---|---|
| **Wing span** | Less induced drag, longer glide | More weight, more structure | Aspect ratio |
| **Wing area** | Shorter take-off, slower stall | More drag at cruise | Wing loading |
| **Airfoil** | Better lift-to-drag | Cost | Airfoil shape |
| **Body material** (wood → aluminium → carbon) | Lighter and stronger | Expensive | Specific strength |
| **Engine** (piston → turboprop → jet) | More thrust and speed | More fuel burn and weight | Power vs efficiency |
| **Propeller** | Better efficiency at cruise | Weight | Propeller efficiency |
| **Fuel tank** | More fuel, further range | Weight; needs a longer runway | The range equation |
| **Landing gear** (fixed → retractable) | Much less drag | Weight, cost | Parasite drag |
| **Winglets** (later) | Less induced drag | Cost | Wingtip vortices |

### The engineer's note after every flight

A friendly character, the chief engineer, gives one short tip based on the real numbers of that flight:
- "You ran out of fuel at 412 km. A bigger tank adds range, but the extra weight needs a longer runway."
- "28% of your drag came from the landing gear. Retractable gear would save a lot."
- "You flew too fast. Your best-range speed is 180 km/h."

**Tap the note** to see the simple version of the real formula. This is the hook for engineers and curious players without scaring casual players.

## Progression

**The world map:** a route of real cities with real distances. The first route is in the Middle East; later routes cross continents.

| Stage | Aircraft | Example goal |
|---|---|---|
| 1 | Paper glider (tutorial) | Fly off the rooftop |
| 2 | Wooden trainer | Amman → Dead Sea (about 50 km) |
| 3 | Bush plane | Amman → Aqaba (about 330 km) |
| 4 | Twin turboprop | Amman → Beirut → Cyprus |
| 5 | Business jet | Across the Gulf |
| 6 | Airliner | Europe |
| 7 | Long-range record plane | Around the world |

Distances shown in the game must be checked against real maps when the routes are set.

- **Each new aircraft** starts weak, so the upgrade loop restarts with new parts and new engineering ideas.
- **Special planes** are unlocked later: a solar plane (flies all day, slow), a seaplane, an aerobatic plane.

## Daily and social

- **Daily challenge:** a set plane and a set rule, for example "furthest flight with only 20 kg of fuel". Global and friends leaderboards. The server replays the plane's numbers so scores can't be faked.
- **Hangar:** paint and decal your planes. This is self-expression and a source of purchases.
- **Share:** a short replay clip of your record flight.

## Free to play, done fairly

| Source | What it is |
|---|---|
| Rewarded ads | Double coins after a flight; a free refuel boost; a free repair |
| Starter pack | Coins + a paint job + one engine level, once |
| Coin and gem packs | To upgrade faster |
| Hangar pass (monthly) | Daily gems, exclusive paint, no forced ads |
| Paint and decal packs | Cosmetic only |
| Remove ads | One-time purchase that removes the optional between-flight ads |

**Rules**
- No paid random boxes.
- Every upgrade can be earned by playing.
- Between-flight ads appear at most once every 3 flights, never during the first session, and never for paying players.

## Why this can find a market

- **The genre is proven:** upgrade-and-fly-further games are simple to understand and easy to advertise. A short video of a plane flying further after each upgrade explains itself.
- **The engineering angle:** the "why" notes, real formulas and real cities make it smarter than its rivals and attractive to students, parents and aviation fans.
- **Cheap to build:** one scene, a side-view flight model, an upgrade screen and a map. A small team, or you with Claude Code, can build a first version in weeks, not months.

## Find the market first: test plan

| Step | What | Pass to continue | Rough cost |
|---|---|---|---|
| 1 | **Ad test before building:** 3 short videos (upgrade loop, engineering note, record flight) on TikTok and Meta in 3 regions (Middle East, US/UK, India or South-East Asia), pointing to a store page | Cheap clicks and installs compared with similar casual games; at least one video and region clearly ahead | $1.5–3K |
| 2 | **Playable prototype:** flight model, 4 upgrades, 1 plane, 3 cities | Players fly 10+ times in the first session | Your time |
| 3 | **Soft launch on Android** in the cheapest region that showed interest | Day-1 return 35%+, day-7 12%+, at least 5 rewarded ads per daily player | $2–5K of ads |
| 4 | **Grow** in the best region; add iOS | Ad revenue + purchases pay back the cost of installs within about 90–180 days | From step 3 |

These thresholds are typical casual-game targets, not research figures. We'll set the final ones before testing.

## What's next

1. A playable web prototype of one flight with four upgrades, to feel the loop in a day.
2. The systems spec with the formulas, part stats and upgrade costs in a `balance.json`.
3. Ad-test video prompts for Veo.
4. The build guide for you + Claude Code + Unity.
