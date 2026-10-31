namespace Gahwit.Core
{
    // One switch per notification type (COMPLETE_SPEC.md sections 3 and 4). Saved under settings.notifications.
    public sealed class NotificationSwitches
    {
        public bool EnergyFull = true;
        public bool Regular = true;
        public bool Task = true;
        public bool Event = true;
        public bool Away = true;
        public bool Hosting = true;
    }
}
