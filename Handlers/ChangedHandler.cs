using System.Threading.Tasks;
using Rebus.Handlers;

namespace Rebus.NoDispatchHandlers.Tests.Handlers
{
    public class ChangedHandler : IHandleMessages<ChangedEvent>
    {
        public Task Handle(ChangedEvent message)
        {
            return Task.CompletedTask;
        }
    }
}