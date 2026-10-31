using System;
using System.Collections.Generic;
using System.Linq;

namespace Gahwit.Merge
{
    // Orders (GAME_SYSTEMS.md section 6): who orders what, serving, and customers who leave.
    public sealed partial class Game
    {
        public int CountOnBoard(string chain, int level) => State.Board.Count(item => item != null && item.Is(chain, level));

        public bool CanServe(Order order) => order.Lines.All(line => CountOnBoard(line.Chain, line.Level) >= line.Count);

        // Loops, not LINQ: the board asks this for all 63 squares after every change.
        public bool IsWanted(Item item)
        {
            if (item == null || item.IsGenerator) return false;
            foreach (var order in State.Orders)
            foreach (var line in order.Lines)
                if (line.Chain == item.Chain && line.Level == item.Level) return true;
            return false;
        }

        // A customer who leaves unserved takes the order away; a new customer takes the slot.
        public bool DropOrder(int orderId)
        {
            var order = State.Orders.Find(o => o.Id == orderId);
            if (order == null) return false;
            State.Orders.Remove(order);
            OrderDropped?.Invoke(order);
            FillOrders();
            Changed?.Invoke();
            return true;
        }

        public bool Serve(int orderId)
        {
            var order = State.Orders.Find(o => o.Id == orderId);
            if (order == null || !CanServe(order)) return false;

            foreach (var line in order.Lines)
            {
                int left = line.Count;
                for (int i = 0; i < CellCount && left > 0; i++)
                {
                    if (State.Board[i] == null || !State.Board[i].Is(line.Chain, line.Level)) continue;
                    State.Board[i] = null;
                    left--;
                }
            }

            State.Orders.Remove(order);
            State.OrdersServed++;
            // A double weekend (missing/18) multiplies the coins while it runs.
            LastServedCoins = order.Coins * Math.Max(1, CoinMultiplier);
            State.Coins += LastServedCoins;
            Progress("serve", 1);
            Progress("earn_coins", LastServedCoins);
            if (order.IsCharacter) Progress("serve_regular", 1);
            OrderServed?.Invoke(order);
            AddXp(order.Xp);
            FillOrders();
            Changed?.Invoke();
            return true;
        }

        void FillOrders()
        {
            var first = Content.FirstOrder;
            if (State.OrdersServed == 0 && State.Orders.Count == 0 && first != null)
            {
                var line = new OrderLine { Chain = first.Chain, Level = first.Level, Count = first.Count };
                State.Orders.Add(MakeOrder(first.Customer, true, new List<OrderLine> { line }));
            }

            int slots = Config.OrderSlots(State.Level);
            while (State.Orders.Count < slots) State.Orders.Add(NewOrder());
        }

        // Found recipes whose chains are all in the given pool; set by Recipes.RecipeBook.
        public Func<ICollection<string>, List<Signature>> Signatures;

        Order NewOrder()
        {
            // Fairness (GAME_SYSTEMS.md section 6): orders only use chains whose generator the player has.
            var pool = AllPieces.Where(p => p.IsGenerator).Select(p => p.Chain).Distinct().OrderBy(c => c).ToList();
            if (State.EasyOrdersLeft > 0 && pool.Count > 0) return EasyOrder(pool);
            var signature = Signatures != null && rng.NextDouble() < Config.LevelB.Recipes.SignatureOrderChance ? Signatures(pool) : null;
            if (signature != null && signature.Count > 0) return SignatureOrder(signature[rng.Range(0, signature.Count)]);
            float size = Config.OrderSizeUnits(State.Level);
            bool twoLines = State.Level >= Config.Orders.TwoItemOrderFromLevel && pool.Count > 1 &&
                            rng.NextDouble() < Config.Orders.TwoItemOrderChance;
            int lineCount = twoLines ? 2 : 1;

            var lines = new List<OrderLine>();
            for (int n = 0; n < lineCount; n++)
            {
                string chain = pool[rng.Range(0, pool.Count)];
                pool.Remove(chain);
                lines.Add(MakeLine(chain, size / lineCount));
            }

            return ForCustomer(lines);
        }

        // A regular in their hours, or a walk-in.
        Order ForCustomer(List<OrderLine> lines)
        {
            var schedule = Config.LevelB.Regulars.Schedule;
            var named = Content.Characters
                .Where(kv => kv.Value.PlacesOrders && kv.Value.FromChapter <= State.Chapter)
                .Where(kv => !schedule.TryGetValue(kv.Key, out var times) || times.Contains(TimeOfDay))
                .Select(kv => kv.Key).OrderBy(id => id).ToList();
            if (named.Count > 0 && rng.NextDouble() < Config.Orders.CharacterOrderChance)
                return MakeOrder(named[rng.Range(0, named.Count)], true, lines);
            return MakeOrder(null, false, lines);
        }

        // A signature dish: the recipe's two items, paying signatureCoinMultiplier × a normal order of that size.
        Order SignatureOrder(Signature signature)
        {
            var lines = signature.Lines.Select(l => new OrderLine { Chain = l.Chain, Level = l.Level, Count = l.Count }).ToList();
            var order = ForCustomer(lines);
            order.Recipe = signature.Recipe;
            order.Coins = (int)Math.Round(order.Coins * Config.LevelB.Recipes.SignatureCoinMultiplier);
            return order;
        }

        // Right after the tutorial (COMPLETE_SPEC.md section 1, step 11): one low-level item.
        Order EasyOrder(List<string> pool)
        {
            State.EasyOrdersLeft--;
            var easy = Config.Tutorial.EasyOrder;
            var lines = new List<OrderLine>();
            for (int n = 0; n < easy.Lines && pool.Count > 0; n++)
            {
                string chain = pool[rng.Range(0, pool.Count)];
                pool.Remove(chain);
                int level = Math.Min(easy.MaxItemLevel, Math.Min(Config.MaxOrderItemLevel(State.Level), MaxLevel(chain)));
                lines.Add(new OrderLine { Chain = chain, Level = rng.Range(1, level + 1), Count = 1 });
            }
            return MakeOrder(null, false, lines);
        }

        OrderLine MakeLine(string chain, float targetUnits)
        {
            var pick = Config.Formulas.OrderLevelPick;
            int cap = Math.Min(Config.MaxOrderItemLevel(State.Level), MaxLevel(chain));
            int level = rng.NextDouble() < pick.TopLevelChance ? cap : rng.Range(Math.Max(1, cap - pick.Spread), cap + 1);
            int count = (int)Math.Round(targetUnits / Config.ItemUnits(level));
            count = Math.Max(1, Math.Min(Config.Formulas.OrderMaxItemsPerLine, count));
            return new OrderLine { Chain = chain, Level = level, Count = count };
        }

        // Walk-ins have no character; their shown name is picked from the order id, so it needs no save field.
        public Order MakeOrder(string character, bool isCharacter, List<OrderLine> lines, int id = 0)
        {
            int units = lines.Sum(line => line.Count * Config.ItemUnits(line.Level));
            if (id == 0) id = State.NextOrderId++;
            return new Order
            {
                Id = id,
                CreatedMs = ClockMs,
                Customer = isCharacter ? character : Content.WalkIns[id % Content.WalkIns.Count],
                IsCharacter = isCharacter,
                Lines = lines,
                Units = units,
                Coins = Math.Max(Config.Formulas.OrderMinCoins, (int)Math.Round(units * Config.Orders.CoinsPerUnit)),
                Xp = Math.Max(1, (int)Math.Round(units * Config.Orders.XpPerUnit))
            };
        }
    }
}
