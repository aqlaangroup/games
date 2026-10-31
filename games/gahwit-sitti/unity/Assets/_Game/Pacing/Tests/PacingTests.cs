using System;
using System.Linq;
using System.Text;
using Gahwit.Core;
using Gahwit.Customers;
using Gahwit.Merge;
using NUnit.Framework;

namespace Gahwit.Pacing.Tests
{
    // design/sim/simulate.py's typical player, played through the real rules (BUILD_GUIDE.md section 9), so the
    // simulator and the C# game can't drift apart unnoticed. A simple bot spends each session's energy: it serves
    // what's ready, merges up to what orders want, sells when the board is full, and restores tasks when it can.
    // Chapters past the ones with content (missing/11) are paid from their balance.json task costs.
    public sealed class PacingTests
    {
        // The simulator's typical player (PROFILES["typical"], AWAKE_HOURS). Efficiency is the share of what she
        // makes that ends up served; the bot plays nearly perfectly, so it throws away the rest.
        const int Sessions = 4, AdsPerDay = 3, AwakeHours = 16, Days = 35;
        const double Efficiency = 0.85;
        double waste;
        const double Tolerance = 0.25;

        BalanceConfig config;
        ChapterContent content;
        Game game;
        int[] finished;
        int day, virtualTask;
        long clock;
        long energySpent, orderCoins, tipCoins, sellCoins, unitsServed, unitsMade;
        int served, sold;

        [Test]
        public void TheTypicalPlayerFinishesEachChapterNearItsTargetDay()
        {
            config = BalanceConfig.Load();
            content = ChapterContent.Load();
            game = new Game(config, content, Game.NewState(config, content, 2026, 0));
            var customers = new CustomerRules(game); // walk-ins tip, as in the simulator
            customers.Tipped += (_, coins) => tipCoins += coins;
            game.OrderServed += order =>
            {
                served++;
                orderCoins += order.Coins;
                unitsServed += order.Units;
            };
            game.ItemSpawned += cell => unitsMade += config.ItemUnits(game.ItemAt(cell).Level);
            finished = new int[config.Chapters.Count + 1];
            game.ChapterCompleted += chapter => finished[chapter] = day;

            int perSession = (int)Math.Min(config.Energy.Cap, AwakeHours * 3600.0 / Sessions / config.Energy.RegenSeconds);
            game.State.Energy = config.Energy.StartingEnergy;
            for (day = 1; day <= Days && game.State.Chapter <= config.Chapters.Count; day++)
                for (int session = 0; session < Sessions; session++)
                {
                    game.State.Energy += perSession + (session == 0 ? AdsPerDay * config.Energy.RewardedAdAmount : 0);
                    Play();
                }

            var report = new StringBuilder($"energy {energySpent}, coins per energy {(orderCoins + tipCoins + sellCoins) / (double)Math.Max(1, energySpent):0.00} " +
                $"(orders {orderCoins}, tips {tipCoins}, sold {sellCoins}), units served per unit made {unitsServed / (double)Math.Max(1, unitsMade):0.00}, " +
                $"orders {served}, sold {sold}, level {game.State.Level}\nchapter: target / played\n");
            bool ok = true;
            foreach (var chapter in config.Chapters)
            {
                int played = finished[chapter.N] == 0 ? Days + 1 : finished[chapter.N];
                double allowed = Math.Max(1, chapter.TargetDay * Tolerance);
                bool near = Math.Abs(played - chapter.TargetDay) <= allowed;
                ok &= near;
                report.AppendLine($"{chapter.N}: {chapter.TargetDay} / {(played > Days ? "after " + Days : played.ToString())}{(near ? "" : "  <-- off")}");
            }
            UnityEngine.Debug.Log("[Pacing] " + report);
            Assert.IsTrue(ok, report.ToString());
        }

        void Play()
        {
            int cost = config.Generators.TapEnergyCost;
            for (int guard = 0; guard < 5000 && game.State.Energy >= cost; guard++)
            {
                ServeReady();
                while (MergeUseful()) ServeReady();
                PayTasks();
                if (game.EmptyCells == 0 && !SellUnwanted()) break;
                int generator = PickGenerator();
                if (generator < 0) break;
                clock += 1000;
                if (game.TapGenerator(generator, clock, out int spawned) != TapResult.Spawned) break;
                energySpent += cost;
                waste += 1 - Efficiency;
                if (waste >= 1)
                {
                    waste -= 1;
                    sellCoins += game.Sell(spawned);
                    sold++;
                }
            }
            ServeReady();
            while (MergeUseful()) ServeReady();
            PayTasks();
        }

        void ServeReady()
        {
            for (var order = game.Orders.FirstOrDefault(game.CanServe); order != null; order = game.Orders.FirstOrDefault(game.CanServe))
                game.Serve(order.Id);
        }

        // The highest level any order wants from a chain, or 0.
        int Wanted(string chain) => game.Orders.SelectMany(o => o.Lines).Where(l => l.Chain == chain).Select(l => l.Level).DefaultIfEmpty(0).Max();

        // How many of chain/level the orders ask for, and how many the board has.
        int Needed(string chain, int level) => game.Orders.SelectMany(o => o.Lines).Where(l => l.Chain == chain && l.Level == level).Sum(l => l.Count);
        int Have(string chain, int level) => game.State.Board.Count(i => i != null && i.Is(chain, level));

        // Merges spare generator copies (more output, as the simulator assumes), and a pair of items only when two
        // spare ones exist beyond what orders need at that level and an order wants something higher.
        bool MergeUseful()
        {
            var board = game.State.Board;
            for (int a = 0; a < board.Length; a++)
            {
                var item = board[a];
                if (item == null) continue;
                if (!item.IsGenerator && (item.Level >= Wanted(item.Chain) || Have(item.Chain, item.Level) - 2 < Needed(item.Chain, item.Level))) continue;
                for (int b = a + 1; b < board.Length; b++)
                    if (item.SameAs(board[b]) && game.CanMerge(a, b))
                    {
                        game.Move(a, b);
                        return true;
                    }
            }
            return false;
        }

        bool SellUnwanted()
        {
            var board = game.State.Board;
            int cell = Enumerable.Range(0, board.Length)
                .Where(i => board[i] != null && game.CanSell(board[i]) && board[i].Level > Wanted(board[i].Chain))
                .DefaultIfEmpty(-1)
                .OrderBy(i => i < 0 ? 0 : board[i].Level).First();
            if (cell < 0)
                cell = Enumerable.Range(0, board.Length).Where(i => board[i] != null && game.CanSell(board[i]))
                    .OrderBy(i => board[i].Level).DefaultIfEmpty(-1).First();
            if (cell < 0) return false;
            sellCoins += game.Sell(cell);
            sold++;
            return true;
        }

        // The generator of the chain the orders are shortest of, counted in units (2^(level-1) each).
        int PickGenerator()
        {
            var board = game.State.Board;
            int best = -1;
            double bestShort = double.MinValue;
            for (int cell = 0; cell < board.Length; cell++)
            {
                var generator = board[cell];
                if (generator == null || !generator.IsGenerator) continue;
                double need = game.Orders.SelectMany(o => o.Lines).Where(l => l.Chain == generator.Chain).Sum(l => l.Count * config.ItemUnits(l.Level));
                double have = board.Where(i => i != null && !i.IsGenerator && i.Chain == generator.Chain).Sum(i => config.ItemUnits(i.Level));
                double shortBy = need - have + generator.Level * 0.01; // the better generator wins a tie
                if (shortBy > bestShort)
                {
                    bestShort = shortBy;
                    best = cell;
                }
            }
            return best;
        }

        // Restores what the coins cover: real tasks while the chapter has content, then balance.json's task costs.
        void PayTasks()
        {
            while (game.CurrentTask != null && game.CanAffordTask) game.CompleteTask(game.CurrentTask.Styles?.FirstOrDefault());
            if (game.CurrentTask != null || game.State.Chapter > config.Chapters.Count) return;
            var chapter = config.Chapter(game.State.Chapter);
            while (virtualTask < chapter.TaskCosts.Count && game.State.Coins >= chapter.TaskCosts[virtualTask])
                game.State.Coins -= chapter.TaskCosts[virtualTask++];
            if (virtualTask < chapter.TaskCosts.Count) return;
            finished[chapter.N] = day;
            game.AddEnergy(chapter.Reward.Energy);
            game.State.Chapter++;
            virtualTask = 0;
        }
    }
}
