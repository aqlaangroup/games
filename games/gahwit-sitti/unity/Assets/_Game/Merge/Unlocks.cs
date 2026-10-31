using System.Collections.Generic;
using System.Linq;

namespace Gahwit.Merge
{
    // The one unlock schedule (balance.json unlocks): what the player has reached, and which unlock card to show
    // next. Only features with a card get one (they exist in this build), at most unlockCardsPerSession a session,
    // and each only once (COMPLETE_SPEC.md section 2).
    public sealed class Unlocks
    {
        readonly Game game;
        readonly HashSet<string> withCards;
        int shownThisSession;

        public Unlocks(Game game, IEnumerable<string> featuresWithCards)
        {
            this.game = game;
            withCards = new HashSet<string>(featuresWithCards);
        }

        // Features reached past the start: levels above 1, and chapters reached.
        public IEnumerable<string> Reached
        {
            get
            {
                var unlocks = game.Config.Unlocks;
                foreach (var pair in unlocks.ByPlayerLevel)
                    if (int.Parse(pair.Key) > 1 && int.Parse(pair.Key) <= game.State.Level)
                        foreach (var feature in pair.Value) yield return feature;
                foreach (var pair in unlocks.ByChapter)
                    if (int.Parse(pair.Key) <= game.State.Chapter)
                        foreach (var feature in pair.Value) yield return feature;
            }
        }

        public string NextCard()
        {
            if (shownThisSession >= game.Config.Unlocks.UnlockCardsPerSession) return null;
            return Reached.FirstOrDefault(f => withCards.Contains(f) && !game.State.UnlocksShown.Contains(f));
        }

        public void Shown(string feature)
        {
            if (!game.State.UnlocksShown.Contains(feature)) game.State.UnlocksShown.Add(feature);
            shownThisSession++;
            game.Touch();
        }
    }
}
