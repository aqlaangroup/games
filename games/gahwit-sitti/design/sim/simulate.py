"""Pacing simulator for Gahwit Sitti season 1.

Reads ../balance.json and plays 35 days for several player types.
It answers: when does each chapter finish, what player level is reached,
how many orders a day does the player serve, and does the season pass fill up.

Run:  python3 simulate.py            (prints a report and writes results.md)
"""
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
B = json.load(open(os.path.join(HERE, "..", "balance.json"), encoding="utf-8"))

ENERGY = B["energy"]
REGEN_PER_HOUR = 3600 / ENERGY["regenSeconds"]
YIELD = {int(k): v["unitsPerTap"] for k, v in B["generators"]["levelOutput"].items()}
COINS_PER_UNIT = B["orders"]["coinsPerUnit"]
XP_PER_UNIT = B["orders"]["xpPerUnit"]
# Walk-in customers tip when served (SYSTEMS_B.md section 3); regulars and story characters don't.
# Cosiness raises tips, but decor isn't modelled yet, so this is the tip at cosiness 0.
WALK_IN_SHARE = 1 - B["orders"]["characterOrderChance"]
TIP_AVG = sum(B["levelB"]["walkIns"]["tipCoins"]) / 2
PASS = B["events"]["templates"]["pass"]
CHAPTERS = B["chapters"]
GEN_CHAIN = {g["id"]: g["chain"] for g in B["generators"]["list"]}
GEN_UNLOCK = {g["chain"]: g["unlockChapter"] for g in B["generators"]["list"]}

PROFILES = {
    # sessions a day, rewarded ads watched a day, share of energy turned into served orders, gem/IAP energy a day
    "casual":  {"sessions": 3, "ads": 0, "eff": 0.80, "bought": 0},
    "typical": {"sessions": 4, "ads": 3, "eff": 0.85, "bought": 0},
    "engaged": {"sessions": 6, "ads": 5, "eff": 0.88, "bought": 0},
    "payer":   {"sessions": 5, "ads": 2, "eff": 0.88, "bought": 200},
}
AWAKE_HOURS = 16


def xp_to_next(level):
    return 60 + 30 * level + 2 * level * level


def order_size(level):
    return 6 + 1.6 * level


def daily_free_energy(sessions):
    gap = AWAKE_HOURS / sessions
    per_session = min(ENERGY["cap"], gap * REGEN_PER_HOUR)
    return per_session * sessions


class Gens:
    """Generators per chain. Copies arrive at level 1; two equal levels merge up."""

    def __init__(self):
        self.by_chain = {}

    def unlock(self, chain):
        self.by_chain.setdefault(chain, [1])

    def add_copy(self, chain):
        levels = self.by_chain.setdefault(chain, [])
        levels.append(1)
        changed = True
        while changed:
            changed = False
            for lv in sorted(set(levels)):
                if levels.count(lv) >= 2 and lv < 5:
                    levels.remove(lv)
                    levels.remove(lv)
                    levels.append(lv + 1)
                    changed = True
                    break

    def avg_yield(self):
        best = [YIELD[max(lvls)] for lvls in self.by_chain.values() if lvls]
        return sum(best) / len(best)


def run(profile, days=35):
    p = PROFILES[profile]
    level, xp = 1, 0.0
    coins = 0.0
    chapter, task = 0, 0  # indexes into CHAPTERS / taskCosts
    finished = {}
    gens = Gens()
    for chain, ch in GEN_UNLOCK.items():
        if ch == 1:
            gens.unlock(chain)
    pass_points = 0.0
    rows = []
    for day in range(1, days + 1):
        energy = daily_free_energy(p["sessions"]) + p["ads"] * ENERGY["rewardedAdAmount"] + p["bought"]
        if day == 1:
            energy += ENERGY["startingEnergy"]
        orders_today = 0.0
        spent_today = 0.0
        # spend energy in small chunks so level-ups and chapter rewards feed back in
        while energy >= 1:
            chunk = min(energy, 10)
            energy -= chunk
            spent_today += chunk
            units = chunk * p["eff"] * gens.avg_yield()
            orders = units / order_size(level)
            coins += units * COINS_PER_UNIT + orders * WALK_IN_SHARE * TIP_AVG
            xp += units * XP_PER_UNIT
            orders_today += orders
            while xp >= xp_to_next(level):
                xp -= xp_to_next(level)
                level += 1
                energy += ENERGY["levelUpReward"]
            while chapter < len(CHAPTERS):
                cost = CHAPTERS[chapter]["taskCosts"][task]
                if coins < cost:
                    break
                coins -= cost
                xp += 10 + 2 * (chapter + 1)
                task += 1
                if task == len(CHAPTERS[chapter]["taskCosts"]):
                    reward = CHAPTERS[chapter]["reward"]
                    energy += reward.get("energy", 0)
                    copy = reward.get("generatorCopy")
                    if copy:
                        gens.add_copy(GEN_CHAIN[copy])
                    finished[chapter + 1] = day
                    chapter += 1
                    task = 0
                    for chain, ch in GEN_UNLOCK.items():
                        if ch == chapter + 1:
                            gens.unlock(chain)
        pass_points += spent_today * PASS["pointsPerEnergySpent"]
        rows.append({
            "day": day, "level": level, "chapter": min(chapter + 1, len(CHAPTERS)),
            "energy": round(spent_today), "orders": round(orders_today), "yield": round(gens.avg_yield(), 2),
            "passTier28": None,
        })
    tier_at_28 = min(PASS["tiers"], int(sum(r["energy"] for r in rows[:28]) * PASS["pointsPerEnergySpent"] // PASS["pointsPerTier"]))
    return {"finished": finished, "rows": rows, "passTier28": tier_at_28}


TARGETS = {1: 1, 2: 1, 3: 2, 4: 3, 5: 5, 6: 7, 7: 10, 8: 14, 9: 21, 10: 30}


def main():
    out = ["# Pacing simulation results", "",
           "Generated by `simulate.py` from `balance.json`. Day = calendar day of play.", ""]
    out.append("## Day each chapter finishes")
    out.append("")
    out.append("| Chapter | Target day (typical player) | " + " | ".join(PROFILES) + " |")
    out.append("|---|---|" + "---|" * len(PROFILES))
    results = {k: run(k) for k in PROFILES}
    for n in range(1, len(CHAPTERS) + 1):
        cells = [str(results[k]["finished"].get(n, "after 35")) for k in PROFILES]
        out.append("| %d. %s | %d | %s |" % (n, CHAPTERS[n - 1]["en"], TARGETS[n], " | ".join(cells)))
    out.append("")
    out.append("## Typical player, day by day")
    out.append("")
    out.append("| Day | Player level | Working on chapter | Energy spent | Orders served | Avg generator output |")
    out.append("|---|---|---|---|---|---|")
    for r in results["typical"]["rows"]:
        if r["day"] in (1, 2, 3, 5, 7, 10, 14, 21, 28, 30, 35):
            out.append("| %d | %d | %d | %d | %d | %.2f |" % (r["day"], r["level"], r["chapter"], r["energy"], r["orders"], r["yield"]))
    out.append("")
    out.append("## Season pass (28 days, %d tiers)" % PASS["tiers"])
    out.append("")
    for k in PROFILES:
        out.append("- %s: reaches tier %d" % (k, results[k]["passTier28"]))
    out.append("")
    # target check for the typical player
    misses = []
    for n, d in TARGETS.items():
        got = results["typical"]["finished"].get(n)
        if got is None or abs(got - d) > max(1, round(d * 0.25)):
            misses.append("chapter %d: target day %d, got %s" % (n, d, got))
    out.append("## Check against targets (typical player, ±25%)")
    out.append("")
    out.append("All chapters on target." if not misses else "\n".join("- " + m for m in misses))
    text = "\n".join(out) + "\n"
    open(os.path.join(HERE, "results.md"), "w", encoding="utf-8").write(text)
    print(text)


if __name__ == "__main__":
    main()
