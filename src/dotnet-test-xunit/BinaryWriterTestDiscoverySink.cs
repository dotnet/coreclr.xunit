using System;
using System.IO;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xunit.Runner.DotNet
{
    public class BinaryWriterTestDiscoverySink : BinaryWriterTestSink, ITestDiscoverySink
    {
        public BinaryWriterTestDiscoverySink(BinaryWriter binaryWriter) : base(binaryWriter)
        {
        }

        public void SendTestFound(Test test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            BinaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestDiscovery.TestFound",
                Payload = JToken.FromObject(test),
            }));
        }
    }
}
