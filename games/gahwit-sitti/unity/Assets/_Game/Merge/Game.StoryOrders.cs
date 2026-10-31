using System;
using System.Linq;

namespace Gahwit.Merge
{
    // Story orders (DEVLOG 2026-10-31): two special orders a chapter, from a character in that chapter's story, with
    // their own lines (storyorder.<id>.ask and .thanks). One comes once the chapter has its AtTask tasks done and a seat
    // is free, before any other customer, and once that character has no other order waiting (one each at a time); it
    // pays like any order (its reward is friendship, in CustomerRules) and never comes back once served.
    public sealed partial class Game
    {
        public event Action<Order> StoryOrderServed;

        void AddStoryOrders()
        {
            int seats = Config.OrderSlots(State.Level);
            foreach (var def in Content.StoryOrders)
            {
                if (State.Orders.Count >= seats) return;
                // One that missed its chapter (it ended before a seat came free) still comes early in the next, first;
                // older ones don't, so a save from before story orders isn't flooded with the whole season's.
                if (def.Chapter > State.Chapter || def.Chapter < State.Chapter - 1) continue;
                if (def.Chapter == State.Chapter && State.TaskIndex < def.AtTask) continue;
                if (State.StoryOrdersDone.Contains(def.Id) || State.Orders.Any(o => o.Customer == def.Character)) continue;
                if (def.Lines.Any(l => !Config.Chains.ContainsKey(l.Chain) || l.Level > MaxLevel(l.Chain))) continue;
                var lines = def.Lines.Select(l => new OrderLine { Chain = l.Chain, Level = l.Level, Count = l.Count }).ToList();
                var order = MakeOrder(def.Character, true, lines);
                order.Story = def.Id;
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
