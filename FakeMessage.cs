namespace Rebus.NoDispatchHandlers.Tests
{
    public class FakeMessage: IFakeMessage {
        public bool isTimebomb { get; set; } = false;
    }
}