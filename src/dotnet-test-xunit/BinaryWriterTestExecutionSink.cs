using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xunit.Runner.DotNet
{
    public class BinaryWriterTestExecutionSink : ITestExecutionSink
    {
        private readonly BinaryWriter _binaryWriter;
        private readonly ConcurrentDictionary<string, TestState> _runningTests;

        public BinaryWriterTestExecutionSink(BinaryWriter binaryWriter)
        {
            _binaryWriter = binaryWriter;
            _runningTests = new ConcurrentDictionary<string, TestState>();
        }

        public void SendTestCompleted()
        {
            _binaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestRunner.TestCompleted"
            }));
        }

        public void SendTestStarted(Test test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            if (test.FullyQualifiedName != null)
            {
                var state = new TestState() { StartTime = DateTimeOffset.Now, };
                _runningTests.TryAdd(test.FullyQualifiedName, state);
            }

            _binaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestExecution.TestStarted",
                Payload = JToken.FromObject(test),
            }));
        }

        public void SendTestResult(TestResult testResult)
        {
            if (testResult == null)
            {
                throw new ArgumentNullException(nameof(testResult));
            }

            if (testResult.StartTime == default(DateTimeOffset) && testResult.Test.FullyQualifiedName != null)
            {
                TestState state;
                _runningTests.TryRemove(testResult.Test.FullyQualifiedName, out state);

                testResult.StartTime = state.StartTime;
            }

            if (testResult.EndTime == default(DateTimeOffset))
            {
                testResult.EndTime = DateTimeOffset.Now;
            }

            _binaryWriter.Write(JsonConvert.SerializeObject(new Message
            {
                MessageType = "TestExecution.TestResult",
                Payload = JToken.FromObject(testResult),
            }));
        }

        private class TestState
        {
            public DateTimeOffset StartTime { get; set; }
        }
    }
}
