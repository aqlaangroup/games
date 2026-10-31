using System;
using System.Linq;

namespace Gahwit.Merge
{
    // Story orders (DEVLOG 2026-10-31): two special orders a chapter, from a character in that chapter's story, with
    // their own lines (storyorder.<id>.ask and .thanks). One comes once the chapter has its AtTask tasks done, in a slot
    // of its own, pays storyOrderCoinMultiplier and storyOrderXpMultiplier, and never comes back once served.
    public sealed partial class Game
    {
        public event Action<Order> StoryOrderServed;

        void AddStoryOrders()
        {
            var kitchen = Config.LevelB.Kitchen;
            foreach (var def in Content.StoryOrders)
            {
                if (def.Chapter != State.Chapter || State.TaskIndex < def.AtTask) continue;
                if (State.StoryOrdersDone.Contains(def.Id) || State.Orders.Any(o => o.Story == def.Id)) continue;
                if (def.Lines.Any(l => !Config.Chains.ContainsKey(l.Chain) || l.Level > MaxLevel(l.Chain))) continue;
                var lines = def.Lines.Select(l => new OrderLine { Chain = l.Chain, Level = l.Level, Count = l.Count }).ToList();
                var order = MakeOrder(def.Character, true, lines);
                order.Story = def.Id;
                order.Coins = (int)Math.Round(order.Coins * kitchen.StoryOrderCoinMultiplier);
                order.Xp = (int)Math.Round(order.Xp * kitchen.StoryOrderXpMultiplier);
                State.Orders.Add(order);
            }
        }

        void FinishStoryOrder(Order order)
        {
            if (order.Story == null) return;
            if (!State.StoryOrdersDone.Contains(order.Story)) State.StoryOrdersDone.Add(order.Story);
            StoryOrderServed?.Invoke(order);
        }
    }
}
