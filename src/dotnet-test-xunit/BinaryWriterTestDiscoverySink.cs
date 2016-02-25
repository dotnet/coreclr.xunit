using System;
using System.IO;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xunit.Runner.DotNet
{
    public class BinaryWriterTestDiscoverySink : ITestDiscoverySink
    {
        private readonly BinaryWriter _binaryWriter;

        public BinaryWriterTestDiscoverySink(BinaryWriter binaryWriter)
        {
            _binaryWriter = binaryWriter;
        }

        public void SendTestCompleted()
        {
            _binaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestRunner.TestCompleted"
            }));
        }

        public void SendTestFound(Test test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            _binaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestDiscovery.TestFound",
                Payload = JToken.FromObject(test),
            }));
        }
    }
}
