using System;
using System.Linq;

namespace Gahwit.Merge
{
    // Teta's kitchen notebook (DEVLOG 2026-10-31): the first time the player makes an item, its page opens with Teta's
    // note about it (notebook.<chain>_<level>) and a few coins; finding every page of a chain gives energy.
    public sealed partial class Game
    {
        public event Action<string, int> PageFound;     // chain, level
        public event Action<string> NotebookChainDone;  // chain

        public static string PageId(string chain, int level) => chain + "_" + level;

        public bool HasPage(string chain, int level) => State.NotebookPages.Contains(PageId(chain, level));

        public int PagesFound(string chain) => State.NotebookPages.Count(p => p.StartsWith(chain + "_", StringComparison.Ordinal));

        // Called for every item the player makes: from a generator or by merging.
        void Discover(Item item)
        {
            if (item == null || item.IsGenerator || HasPage(item.Chain, item.Level)) return;
            State.NotebookPages.Add(PageId(item.Chain, item.Level));
            // Pages open during the tutorial too, but pay only after it, so its coin steps stay as written.
            if (State.TutorialDone) State.Coins += Config.LevelB.Kitchen.NotebookCoinsPerPage;
            PageFound?.Invoke(item.Chain, item.Level);
            if (PagesFound(item.Chain) < MaxLevel(item.Chain)) return;
            State.Energy += Config.LevelB.Kitchen.NotebookChainEnergy;
            NotebookChainDone?.Invoke(item.Chain);
        }
    }
}
