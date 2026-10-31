using System;
using System.Collections.Generic;
using Gahwit.Core;

namespace Gahwit.Memories
{
    // "Trace" memory (SYSTEMS_B.md section 5): sew by following a line with a finger. Each point of the line is
    // stitched when the finger passes within the tolerance, in order; accuracy is the share stitched when the finger
    // lifts. Distances are in centimetres on the screen, so the line feels the same on every phone. Numbers come
    // from balance.json levelB.memories.trace; the easy assist widens the tolerance.
    public sealed class TraceGame : IMemoryGame
    {
        readonly TraceConfig config;
        readonly MemoriesConfig memories;
        readonly (float x, float y)[] path;
        readonly bool[] stitched;
        readonly float tolerance;
        int next;

        public TraceGame(MemoriesConfig memories, IList<(float x, float y)> line, bool easyAssist)
        {
            this.memories = memories;
            config = memories.Trace;
            path = Resample(line, config.PathPoints);
            stitched = new bool[path.Length];
            tolerance = config.ToleranceCm * (easyAssist ? config.EasyToleranceScale : 1f);
        }

        public float ToleranceCm => tolerance;
        public int Count => path.Length;
        public (float x, float y) Point(int i) => path[i];
        public bool Stitched(int i) => stitched[i];
        public int Next => next;
        public bool Started { get; private set; }
        public bool Finished { get; private set; }

        public float Accuracy
        {
            get
            {
                int count = 0;
                foreach (bool s in stitched) if (s) count++;
                return count / (float)stitched.Length;
            }
        }

        public int Stars => MemoryStars.From(Accuracy, config.StarsAtAccuracy, memories.Stars);

        public void Tick(float seconds) { }

        // The finger at a point. The line is sewn in order: the first point ahead that the finger is near is
        // stitched, and points it skipped past stay unstitched, so a finger that wanders off can rejoin further on.
        public void Touch(float x, float y)
        {
            if (Finished) return;
            for (int i = next; i < path.Length; i++)
            {
                float dx = x - path[i].x, dy = y - path[i].y;
                if (dx * dx + dy * dy > tolerance * tolerance) continue;
                if (!Started && i != 0) return; // the seam starts at the first point
                Started = true;
                stitched[i] = true;
                next = i + 1;
                if (next >= path.Length) Finished = true;
                return;
            }
        }

        // Lifting the finger after starting ends the seam.
        public void Lift()
        {
            if (Started) Finished = true;
        }

        // Evenly spaced points along the drawn line.
        static (float x, float y)[] Resample(IList<(float x, float y)> line, int count)
        {
            if (line.Count < 2 || count < 2) throw new ArgumentException("A trace line needs at least two points.");
            var lengths = new float[line.Count];
            for (int i = 1; i < line.Count; i++)
            {
                float dx = line[i].x - line[i - 1].x, dy = line[i].y - line[i - 1].y;
                lengths[i] = lengths[i - 1] + (float)Math.Sqrt(dx * dx + dy * dy);
            }
            float total = lengths[line.Count - 1];
            var points = new (float x, float y)[count];
            int segment = 1;
            for (int p = 0; p < count; p++)
            {
                float d = total * p / (count - 1);
                while (segment < line.Count - 1 && lengths[segment] < d) segment++;
                float span = lengths[segment] - lengths[segment - 1];
                float t = span > 0 ? (d - lengths[segment - 1]) / span : 0;
                points[p] = (line[segment - 1].x + (line[segment].x - line[segment - 1].x) * t,
                             line[segment - 1].y + (line[segment].y - line[segment - 1].y) * t);
            }
            return points;
        }
    }
}
