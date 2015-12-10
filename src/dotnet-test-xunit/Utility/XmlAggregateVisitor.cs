using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using Xunit.Abstractions;

namespace Xunit
{
    public class XmlAggregateVisitor : XmlTestExecutionVisitor, IExecutionVisitor
    {
        readonly ConcurrentDictionary<string, ExecutionSummary> _completionMessages;
        readonly IMessageSink _innerMessageSink;

        public XmlAggregateVisitor(IMessageSink innerMessageSink,
                                   ConcurrentDictionary<string, ExecutionSummary> completionMessages,
                                   XElement assemblyElement,
                                   Func<bool> cancelThunk)
            : base(assemblyElement, cancelThunk)
        {
            _innerMessageSink = innerMessageSink;
            _completionMessages = completionMessages;

            ExecutionSummary = new ExecutionSummary();
        }

        public ExecutionSummary ExecutionSummary { get; private set; }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            var result = base.Visit(assemblyFinished);

            ExecutionSummary = new ExecutionSummary
            {
                Total = assemblyFinished.TestsRun,
                Failed = assemblyFinished.TestsFailed,
                Skipped = assemblyFinished.TestsSkipped,
                Time = assemblyFinished.ExecutionTime,
                Errors = Errors
            };

            if (_completionMessages != null)
                _completionMessages.TryAdd(Path.GetFileNameWithoutExtension(assemblyFinished.TestAssembly.Assembly.AssemblyPath), ExecutionSummary);

            return result;
        }

        public override bool OnMessage(IMessageSinkMessage message)
        {
            var result = base.OnMessage(message);
            result = _innerMessageSink.OnMessage(message) || result;
            return result;
        }
    }
}
