using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Gahwit.Core.Tests
{
    // The café's spaces (SYSTEMS_B.md section 1): the floor, the kitchen corner, the rooftop from chapter 6, and Teta's
    // back room for memories only.
    public sealed class CafeSpacesTests
    {
        ChapterContent content;

        [SetUp]
        public void SetUp() => content = ChapterContent.Load();

        [Test]
        public void TheRooftopOpensWithItsStairsAndTheBackRoomNeverShows()
        {
            var opensWith = content.Spaces["rooftop"].OpensWith;
            CollectionAssert.AreEqual(new[] { ChapterContent.FloorSpace, "kitchen" }, content.OpenSpaces(new List<string>()));
            CollectionAssert.AreEqual(new[] { ChapterContent.FloorSpace, "kitchen", "rooftop" }, content.OpenSpaces(new List<string> { opensWith }));
            Assert.IsTrue(content.Spaces["backroom"].MemoriesOnly);
        }

        [Test]
        public void EverySpaceTheContentNamesExists()
        {
            var tasks = content.Chapters.SelectMany(c => c.Tasks).ToList();
            Assert.AreEqual(ChapterContent.FloorSpace, content.Spaces.Keys.First(), "the floor comes first");
            foreach (var space in content.Spaces.Values.Where(s => s.OpensWith != null))
                Assert.IsTrue(tasks.Any(t => t.Id == space.OpensWith), $"no task {space.OpensWith}");
            foreach (var memory in content.Memories)
                if (memory.Value.Space != null) Assert.IsTrue(content.Spaces.ContainsKey(memory.Value.Space), $"{memory.Key}: no space {memory.Value.Space}");
            foreach (var task in tasks.Where(t => t.Props != null))
            foreach (var prop in task.Props.Where(p => p.Space != null))
                Assert.IsTrue(content.Spaces.ContainsKey(prop.Space), $"{task.Id}: no space {prop.Space}");
        }
    }
}
