namespace Rebus.NoDispatchHandlers.Tests
{
    public class TimebombChangedEvent: ChangedEvent {
        public bool isTimebomb { get; set; } = false;
    }
}