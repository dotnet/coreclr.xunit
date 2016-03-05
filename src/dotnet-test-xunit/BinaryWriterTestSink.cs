using System.IO;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;

namespace Xunit.Runner.DotNet
{
    public abstract class BinaryWriterTestSink : ITestSink
    {
        protected BinaryWriter BinaryWriter { get; }

        protected BinaryWriterTestSink(BinaryWriter binaryWriter)
        {
            BinaryWriter = binaryWriter;
        }

        public void SendTestCompleted()
        {
            BinaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestRunner.TestCompleted"
            }));
        }

        public void SendWaitingCommand()
        {
            BinaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestRunner.WaitingCommand"
            }));
        }
    }
}
