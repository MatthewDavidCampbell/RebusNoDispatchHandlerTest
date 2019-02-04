namespace Rebus.NoDispatchHandlers.Tests
{
    public interface IFakeMessage {
        bool isTimebomb { get; set; }
    }
}