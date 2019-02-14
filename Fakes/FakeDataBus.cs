using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.DataBus;

namespace Rebus.NoDispatchHandlers.Tests.Fakes
{
    public class FakeDataBus : IDataBus
    {
        public Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<Dictionary<string, string>> GetMetadata(string dataBusAttachmentId)
        {
            throw new System.NotImplementedException();
        }

        public Task<Stream> OpenRead(string dataBusAttachmentId)
        {
            throw new System.NotImplementedException();
        }
    }
}