using System;

namespace Gahwit.Core
{
    public enum WeatherKind { Clear, Rain, Fog }

    // Sunny most days; rain and fog in winter, from the date (SYSTEMS_B.md section 2). Mood only. The same date
    // always gives the same weather, so the café doesn't change when the app restarts.
    public static class Weather
    {
        public static WeatherKind For(DateTime date, WeatherConfig config)
        {
            if (!config.WinterMonths.Contains(date.Month)) return WeatherKind.Clear;
            // A small hash of the day, spread over 0..1.
            uint h = (uint)(date.Year * 10_000 + date.Month * 100 + date.Day);
            h ^= h >> 16;
            h *= 0x45d9f3b;
            h ^= h >> 16;
            double roll = (h % 10_000) / 10_000.0;
            if (roll < config.RainChance) return WeatherKind.Rain;
            return roll < config.RainChance + config.FogChance ? WeatherKind.Fog : WeatherKind.Clear;
        }
    }
}
