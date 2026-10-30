using System;

namespace Gahwit.Core
{
    // Compares game versions such as "0.1.0" and "1.12.3" part by part. Missing parts count as 0.
    public static class VersionRules
    {
        public static bool IsOlder(string version, string minimum)
        {
            if (string.IsNullOrWhiteSpace(minimum)) return false;
            var a = Parts(version);
            var b = Parts(minimum);
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                int x = i < a.Length ? a[i] : 0, y = i < b.Length ? b[i] : 0;
                if (x != y) return x < y;
            }
            return false;
        }

        static int[] Parts(string version) =>
            Array.ConvertAll((version ?? "0").Split('.'), p => int.TryParse(p, out int n) ? n : 0);
    }
}
