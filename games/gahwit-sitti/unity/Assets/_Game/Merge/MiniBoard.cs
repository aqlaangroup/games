using System;
using System.Collections.Generic;
using System.Linq;
using Gahwit.Core;
using Newtonsoft.Json.Linq;

namespace Gahwit.Merge
{
    // The mini board event (سوق الجمعة; GAME_SYSTEMS.md section 13; missing/18): a small board with its own chain and
    // a stall that makes its first item, using the main energy. Two orders at a time ask for one item each; serving
    // one gives tokens, and token milestones give rewards. The same merge rule as the kitchen: two of a level make
    // the next. Settings are the template's (balance.json events.templates.miniBoard) with the event's own laid over
    // them, so a new theme (its chain names, milestones and rewards) needs no app update.
    public sealed class MiniBoard
    {
        public const string Chain = "event", Stall = "event_stall";

        readonly Game game;
        readonly Random rng;

        public MiniBoard(Game game, string eventId, JObject settings)
        {
            this.game = game;
            EventId = eventId;
            Cols = (int)settings["boardCols"];
            Rows = (int)settings["boardRows"];
            Names = settings["chain"].Select(item => item.Select(n => (string)n).ToArray()).ToList();
            TokensPerOrder = (int)settings["tokensPerOrder"];
            MilestoneTokens = settings["milestoneTokens"].Select(t => (int)t).ToList();
            MilestoneRewards = (JArray)settings["milestoneRewards"];
            if (!game.State.MiniBoards.TryGetValue(eventId, out var state))
            {
                ulong seed = 14695981039346656037;
                foreach (char c in eventId) seed = (seed ^ c) * 1099511628211;
                state = new MiniBoardState { Board = new Item[Cols * Rows], Rng = seed };
                state.Board[(Rows - 1) * Cols + Cols / 2] = new Item(Chain, 1, Stall);
                game.State.MiniBoards[eventId] = state;
            }
            State = state;
            rng = new Random((int)(State.Rng ^ (ulong)State.Tokens));
            while (State.Orders.Count < 2) State.Orders.Add(NewOrderLevel());
        }

        public string EventId { get; }
        public MiniBoardState State { get; }
        public int Cols { get; }
        public int Rows { get; }
        public int MaxLevel => Names.Count;
        public List<string[]> Names { get; }       // [arabic, english] per level
        public int TokensPerOrder { get; }
        public List<int> MilestoneTokens { get; }
        public JArray MilestoneRewards { get; }

        public string Name(int level) => Names[level - 1][game.English && Names[level - 1].Length > 1 ? 1 : 0];

        // Orders ask for higher levels as milestones are reached, from 2 up to the chain's top.
        int NewOrderLevel()
        {
            int cap = Math.Min(MaxLevel, 3 + State.Paid.Count / 2);
            return rng.Next(2, cap + 1);
        }

        public bool Tap(int cell, long nowMs)
        {
            var stall = State.Board[cell];
            if (stall == null || !stall.IsGenerator || game.State.Energy < 1) return false;
            int free = Nearest(cell);
            if (free < 0) return false;
            game.SpendEnergy(1, nowMs);
            game.Progress("tap_generators", 1);
            game.Progress("spend_energy", 1);
            State.Board[free] = new Item(Chain, 1);
            game.Touch();
            return true;
        }

        int Nearest(int from)
        {
            int best = -1, bestDistance = int.MaxValue;
            for (int i = 0; i < State.Board.Length; i++)
            {
                if (State.Board[i] != null) continue;
                int d = Math.Abs(i % Cols - from % Cols) + Math.Abs(i / Cols - from / Cols);
                if (d < bestDistance) { best = i; bestDistance = d; }
            }
            return best;
        }

        // Empty: move. Same level below the top: merge. Otherwise swap. The stall moves but never merges.
        public MoveResult Move(int from, int to)
        {
            var moving = State.Board[from];
            if (from == to || moving == null) return MoveResult.None;
            var target = State.Board[to];
            MoveResult result;
            if (target == null) result = MoveResult.Moved;
            else if (!moving.IsGenerator && !target.IsGenerator && target.Level == moving.Level && moving.Level < MaxLevel)
            {
                State.Board[to] = new Item(Chain, moving.Level + 1);
                State.Board[from] = null;
                game.Progress("merge", 1);
                game.Touch();
                return MoveResult.Merged;
            }
            else result = MoveResult.Swapped;
            State.Board[to] = moving;
            State.Board[from] = target;
            game.Touch();
            return result;
        }

        public bool CanServe(int order) => order < State.Orders.Count && Array.Exists(State.Board, i => i != null && !i.IsGenerator && i.Level == State.Orders[order]);

        // Serves one order: its item goes, tokens come, and a new order takes its place.
        public bool Serve(int order)
        {
            if (!CanServe(order)) return false;
            int level = State.Orders[order];
            State.Board[Array.FindIndex(State.Board, i => i != null && !i.IsGenerator && i.Level == level)] = null;
            State.Tokens += TokensPerOrder;
            State.Orders[order] = NewOrderLevel();
            game.Touch();
            return true;
        }

        // Milestones reached but not rewarded yet (1-based).
        public List<int> Due() =>
            Enumerable.Range(1, MilestoneTokens.Count).Where(n => State.Tokens >= MilestoneTokens[n - 1] && !State.Paid.Contains(n)).ToList();

        public int NextMilestoneTokens => MilestoneTokens.FirstOrDefault(t => t > State.Tokens);

        // Applies a milestone's coins, energy and decor once, and returns its reward. Its gems come through the
        // platform ("event:<id>:<n>").
        public JObject Pay(int milestone)
        {
            if (State.Paid.Contains(milestone) || State.Tokens < MilestoneTokens[milestone - 1]) return null;
            State.Paid.Add(milestone);
            var reward = (JObject)MilestoneRewards[milestone - 1];
            game.State.Coins += reward.Value<int?>("coins") ?? 0;
            game.State.Energy += reward.Value<int?>("energy") ?? 0;
            if (reward["decor"] != null) game.State.Entitlements.Add("decor:" + (string)reward["decor"]);
            if ((reward.Value<int?>("gems") ?? 0) > 0) game.State.PendingRewards.Add($"event:{EventId}:{milestone}");
            game.Touch();
            return reward;
        }

        // The template's settings with the event's own laid over them.
        public static JObject Settings(BalanceConfig config, JObject own)
        {
            var merged = (JObject)config.Events.Templates["miniBoard"].DeepClone();
            if (own != null) merged.Merge(own, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            return merged;
        }
    }
}
