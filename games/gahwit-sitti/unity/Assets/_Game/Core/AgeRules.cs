namespace Gahwit.Core
{
    public enum AgeBand { Unknown, Under13, From13To15, Over15 }

    // The age gate asks only for the birth year (COMPLETE_SPEC.md section 14). The birthday may not have come yet,
    // so a child is counted as the younger possibility. The platform uses the same rule.
    public static class AgeRules
    {
        public const int FirstYear = 1900;

        public static int YoungestAge(int birthYear, int currentYear) => currentYear - birthYear - 1;

        public static AgeBand Band(int? birthYear, int currentYear)
        {
            if (birthYear == null || birthYear < FirstYear || birthYear > currentYear) return AgeBand.Unknown;
            int age = YoungestAge(birthYear.Value, currentYear);
            return age < 13 ? AgeBand.Under13 : age < 16 ? AgeBand.From13To15 : AgeBand.Over15;
        }

        // Under-13s get no personalised ads and no social features until a parent agrees. An unknown age is
        // treated the same way. The 13–15 band is kept apart for Egypt's under-15 rules, pending legal review.
        public static bool NeedsParent(AgeBand band) => band == AgeBand.Under13 || band == AgeBand.Unknown;

        public static bool PersonalisedAdsAllowed(AgeBand band, bool parentConsent) => !NeedsParent(band) || parentConsent;

        public static bool SocialAllowed(AgeBand band, bool parentConsent) => !NeedsParent(band) || parentConsent;

        public static string Code(AgeBand band) => band switch
        {
            AgeBand.Under13 => "under13",
            AgeBand.From13To15 => "13to15",
            AgeBand.Over15 => "16plus",
            _ => "unknown"
        };
    }
}
