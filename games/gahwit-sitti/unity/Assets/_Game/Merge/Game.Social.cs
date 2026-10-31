namespace Gahwit.Merge
{
    // Hosting (missing/20): any item counts toward a big order, so the game gives the piece the player needs least:
    // the lowest level that no order wants, never a generator.
    public sealed partial class Game
    {
        public int SparePiece()
        {
            int best = -1;
            for (int i = 0; i < State.Board.Length; i++)
            {
                var item = State.Board[i];
                if (item == null || item.IsGenerator || IsWanted(item)) continue;
                if (best < 0 || item.Level < State.Board[best].Level) best = i;
            }
            return best;
        }

        // Takes a piece off the board after the platform counted it.
        public bool GivePiece(int cell)
        {
            var item = cell >= 0 && cell < State.Board.Length ? State.Board[cell] : null;
            if (item == null || item.IsGenerator) return false;
            State.Board[cell] = null;
            Progress("help_hosting", 1);
            Changed?.Invoke();
            return true;
        }
    }
}
