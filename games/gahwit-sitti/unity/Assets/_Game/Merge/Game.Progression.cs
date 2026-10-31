namespace Gahwit.Merge
{
    // Player levels and finishing a chapter (GAME_SYSTEMS.md sections 8–9).
    public sealed partial class Game
    {
        // Level-up gems are granted by the platform, never here; listeners report the level-up to it.
        void AddXp(int xp)
        {
            State.Xp += xp;
            while (State.Xp >= Config.XpToNext(State.Level))
            {
                State.Xp -= Config.XpToNext(State.Level);
                State.Level++;
                State.Energy += Config.PlayerLevels.Rewards.Energy;
                LeveledUp?.Invoke(State.Level);
            }
        }

        // The chapter's gem reward is granted by the platform, like every gem. Its generator copy lands on the board
        // to be merged with the matching generator.
        void FinishChapter()
        {
            var chapter = Config.Chapter(State.Chapter);
            State.Energy += chapter.Reward.Energy;
            var def = Config.GeneratorDef(chapter.Reward.GeneratorCopy);
            if (def != null) Place(new Item(def.Chain, 1, def.Id));
            int finished = State.Chapter;
            State.Chapter++;
            State.TaskIndex = 0;
            UnlockGenerators();
            ChapterCompleted?.Invoke(finished);
        }
    }
}
