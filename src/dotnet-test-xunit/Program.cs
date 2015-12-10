using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Testing.Abstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Loader;
using NuGet.Frameworks;
using Xunit.Abstractions;
using ISourceInformationProvider = Xunit.Abstractions.ISourceInformationProvider;
using VsTestCase = Microsoft.Extensions.Testing.Abstractions.Test;

namespace Xunit.Runner.DotNet
{
    public class Program
    {
#pragma warning disable 0649
        volatile bool _cancel;
#pragma warning restore 0649
        readonly ConcurrentDictionary<string, ExecutionSummary> _completionMessages = new ConcurrentDictionary<string, ExecutionSummary>();
        bool _failed;
        IRunnerLogger _logger;
        IMessageSink _reporterMessageHandler;
        readonly ITestDiscoverySink _testDiscoverySink;
        readonly ITestExecutionSink _testExecutionSink;

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var dllPath = args[0];
            var projectPath = GetProjectPathFromDllPath(dllPath);
            AssemblyLoadContext.InitializeDefaultContext(ProjectContext.Create(projectPath, FrameworkConstants.CommonFrameworks.DnxCore50, new[] { RuntimeIdentifier.Current }).CreateLoadContext());

            return new Program().Run(args);
        }

        // This is a temporary workaround.
        private static string GetProjectPathFromDllPath(string dllPath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(dllPath));
            while (directory != directory.Root && directory.EnumerateFiles().All(f => f.Name != "project.json"))
            {
                directory = directory.Parent;
            }
            
            var projectFile = directory.EnumerateFiles().FirstOrDefault(f => f.Name == "project.json");

            return projectFile?.FullName;
        }

        public Program()
        {
            _testDiscoverySink = new StreamingTestDiscoverySink(Console.OpenStandardOutput());

            _testExecutionSink = new StreamingTestExecutionSink(Console.OpenStandardOutput());
        }

        public int Run(string[] args)
        {
            try
            {
                var reporters = GetAvailableRunnerReporters();

                if (args.Length == 0 || args.Any(arg => arg == "-?"))
                {
                    PrintHeader();
                    PrintUsage(reporters);
                    return 2;
                }

#if !DNXCORE50
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#endif

                var commandLine = CommandLine.Parse(reporters, args);

#if !DNXCORE50
                if (commandLine.Debug)
                    Debugger.Launch();
#else
                if (commandLine.Debug)
                {
                    Console.WriteLine("Debug support is not available in DNX Core.");
                    return -1;
                }
#endif

                _logger = new ConsoleRunnerLogger(!commandLine.NoColor);
                _reporterMessageHandler = commandLine.Reporter.CreateMessageHandler(_logger);

                if (!commandLine.NoLogo)
                    PrintHeader();

                var failCount = RunProject(commandLine.Project, commandLine.ParallelizeAssemblies, commandLine.ParallelizeTestCollections,
                                           commandLine.MaxParallelThreads, commandLine.DiagnosticMessages, commandLine.NoColor,
                                           commandLine.DesignTime, commandLine.List, commandLine.DesignTimeTestUniqueNames);

                if (commandLine.Wait)
                {
                    Console.WriteLine();

                    Console.Write("Press ENTER to continue...");
                    Console.ReadLine();

                    Console.WriteLine();
                }

                return failCount > 0 ? 1 : 0;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                return 3;
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine("{0}", ex.Message);
                return 4;
            }
            finally
            {
                Console.ResetColor();
            }
        }

#if !DNXCORE50
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (ex != null)
                Console.WriteLine(ex.ToString());
            else
                Console.WriteLine("Error of unknown type thrown in application domain");

            Environment.Exit(1);
        }
#endif
        static List<IRunnerReporter> GetAvailableRunnerReporters()
        {
            var result = new List<IRunnerReporter>();
            var runnerPath = Path.GetDirectoryName(AppContext.BaseDirectory);

            foreach (var dllFile in Directory.GetFiles(runnerPath, "*.dll").Select(f => Path.Combine(runnerPath, f)))
            {
                TypeInfo[] types;

                var dllName = Path.GetFileNameWithoutExtension(dllFile);

                try
                {
                    var assembly = Assembly.Load(new AssemblyName(dllName));
                    types = assembly.DefinedTypes.ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type == typeof(DefaultRunnerReporter).GetTypeInfo() || type.ImplementedInterfaces.All(i => i != typeof (IRunnerReporter)))
                        continue;

                    var ctor = type.DeclaredConstructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                    if (ctor == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Type {type.FullName} in assembly {dllFile} appears to be a runner reporter, but does not have an empty constructor.");
                        Console.ResetColor();
                        continue;
                    }

                    result.Add((IRunnerReporter)ctor.Invoke(new object[0]));
                }
            }

            return result;
        }

        void PrintHeader()
        {
            Console.WriteLine("xUnit.net DNX Runner ({0}-bit {1})", IntPtr.Size * 8, RuntimeIdentifier.Current);
        }

        static void PrintUsage(IReadOnlyList<IRunnerReporter> reporters)
        {
            Console.WriteLine("Copyright (C) 2015 Outercurve Foundation.");
            Console.WriteLine();
            Console.WriteLine("usage: xunit.runner.dnx [configFile.json] [options] [reporter] [resultFormat filename [...]]");
            Console.WriteLine();
            Console.WriteLine("Valid options:");
            Console.WriteLine("  -nologo                : do not show the copyright message");
            Console.WriteLine("  -nocolor               : do not output results with colors");
            Console.WriteLine("  -parallel option       : set parallelization based on option");
            Console.WriteLine("                         :   none        - turn off all parallelization");
            Console.WriteLine("                         :   collections - only parallelize collections");
            Console.WriteLine("                         :   assemblies  - only parallelize assemblies");
            Console.WriteLine("                         :   all         - parallelize collections and assemblies");
            Console.WriteLine("  -maxthreads count      : maximum thread count for collection parallelization");
            Console.WriteLine("                         :   default   - run with default (1 thread per CPU thread)");
            Console.WriteLine("                         :   unlimited - run with unbounded thread count");
            Console.WriteLine("                         :   (number)  - limit task thread pool size to 'count'");
            Console.WriteLine("  -wait                  : wait for input after completion");
            Console.WriteLine("  -diagnostics           : enable diagnostics messages for all test assemblies");
#if !DNXCORE50
            Console.WriteLine("  -debug                 : launch the debugger to debug the tests");
#endif
            Console.WriteLine("  -trait \"name=value\"    : only run tests with matching name/value traits");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine("  -notrait \"name=value\"  : do not run tests with matching name/value traits");
            Console.WriteLine("                         : if specified more than once, acts as an AND operation");
            Console.WriteLine("  -method \"name\"         : run a given test method (should be fully specified;");
            Console.WriteLine("                         : i.e., 'MyNamespace.MyClass.MyTestMethod')");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine("  -class \"name\"          : run all methods in a given test class (should be fully");
            Console.WriteLine("                         : specified; i.e., 'MyNamespace.MyClass')");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine("  -namespace \"name\"      : run all methods in a given namespace (i.e.,");
            Console.WriteLine("                         : 'MyNamespace.MySubNamespace')");
            Console.WriteLine("                         : if specified more than once, acts as an OR operation");
            Console.WriteLine();

            var switchableReporters = reporters.Where(r => !string.IsNullOrWhiteSpace(r.RunnerSwitch)).ToList();
            if (switchableReporters.Count > 0)
            {
                Console.WriteLine("Reporters: (optional, choose only one)");

                foreach (var reporter in switchableReporters.OrderBy(r => r.RunnerSwitch))
                    Console.WriteLine("  -{0} : {1}", reporter.RunnerSwitch.ToLowerInvariant().PadRight(21), reporter.Description);

                Console.WriteLine();
            }

            Console.WriteLine("Result formats: (optional, choose one or more)");

            foreach (var transform in TransformFactory.AvailableTransforms)
                Console.WriteLine("  {0} : {1}",
                                  string.Format("-{0} <filename>", transform.CommandLine).PadRight(22).Substring(0, 22),
                                  transform.Description);
        }

        int RunProject(XunitProject project,
                       bool? parallelizeAssemblies,
                       bool? parallelizeTestCollections,
                       int? maxThreadCount,
                       bool diagnosticMessages,
                       bool noColor,
                       bool designTime,
                       bool list,
                       IReadOnlyList<string> designTimeFullyQualifiedNames)
        {
            XElement assembliesElement = null;
            var xmlTransformers = TransformFactory.GetXmlTransformers(project);
            var needsXml = xmlTransformers.Count > 0;
            var consoleLock = new object();

            if (!parallelizeAssemblies.HasValue)
                parallelizeAssemblies = project.All(assembly => assembly.Configuration.ParallelizeAssemblyOrDefault);

            if (needsXml)
                assembliesElement = new XElement("assemblies");

            var originalWorkingFolder = Directory.GetCurrentDirectory();

            using (AssemblyHelper.SubscribeResolve())
            {
                var clockTime = Stopwatch.StartNew();

                if (parallelizeAssemblies.GetValueOrDefault())
                {
                    var tasks = project.Assemblies.Select(assembly => TaskRun(() => ExecuteAssembly(consoleLock, assembly, needsXml, parallelizeTestCollections, maxThreadCount, diagnosticMessages, noColor, project.Filters, designTime, list, designTimeFullyQualifiedNames)));
                    var results = Task.WhenAll(tasks).GetAwaiter().GetResult();
                    foreach (var assemblyElement in results.Where(result => result != null))
                        assembliesElement.Add(assemblyElement);
                }
                else
                {
                    foreach (var assembly in project.Assemblies)
                    {
                        var assemblyElement = ExecuteAssembly(consoleLock, assembly, needsXml, parallelizeTestCollections, maxThreadCount, diagnosticMessages, noColor, project.Filters, designTime, list, designTimeFullyQualifiedNames);
                        if (assemblyElement != null)
                            assembliesElement.Add(assemblyElement);
                    }
                }

                clockTime.Stop();

                if (_completionMessages.Count > 0)
                    _reporterMessageHandler.OnMessage(new TestExecutionSummary(clockTime.Elapsed, _completionMessages.OrderBy(kvp => kvp.Key).ToList()));
            }

            Directory.SetCurrentDirectory(originalWorkingFolder);

            foreach (var transformer in xmlTransformers)
                transformer(assembliesElement);

            return _failed ? 1 : _completionMessages.Values.Sum(summary => summary.Failed);
        }

        XElement ExecuteAssembly(object consoleLock,
                                 XunitProjectAssembly assembly,
                                 bool needsXml,
                                 bool? parallelizeTestCollections,
                                 int? maxThreadCount,
                                 bool diagnosticMessages,
                                 bool noColor,
                                 XunitFilters filters,
                                 bool designTime,
                                 bool listTestCases,
                                 IReadOnlyList<string> designTimeFullyQualifiedNames)
        {
            if (_cancel)
                return null;

            var assemblyElement = needsXml ? new XElement("assembly") : null;

            try
            {
                // Turn off pre-enumeration of theories when we're not running in Visual Studio
                if (!designTime)
                    assembly.Configuration.PreEnumerateTheories = false;

                if (diagnosticMessages)
                    assembly.Configuration.DiagnosticMessages = true;

                var discoveryOptions = TestFrameworkOptions.ForDiscovery(assembly.Configuration);
                var executionOptions = TestFrameworkOptions.ForExecution(assembly.Configuration);
                if (maxThreadCount.HasValue)
                    executionOptions.SetMaxParallelThreads(maxThreadCount);
                if (parallelizeTestCollections.HasValue)
                    executionOptions.SetDisableParallelization(!parallelizeTestCollections.GetValueOrDefault());

                var assemblyDisplayName = Path.GetFileNameWithoutExtension(assembly.AssemblyFilename);
                var diagnosticMessageVisitor = new DiagnosticMessageVisitor(consoleLock, assemblyDisplayName, assembly.Configuration.DiagnosticMessagesOrDefault, noColor);

                var sourceInformationProvider = GetSourceInformationProviderAdapater(assembly);


                using (var controller = new XunitFrontController(AppDomainSupport.Denied, assembly.AssemblyFilename, assembly.ConfigFilename, false, diagnosticMessageSink: diagnosticMessageVisitor, sourceInformationProvider: sourceInformationProvider))
                using (var discoveryVisitor = new TestDiscoveryVisitor())
                {
                    var includeSourceInformation = designTime && listTestCases;

                    // Discover & filter the tests
                    _reporterMessageHandler.OnMessage(new TestAssemblyDiscoveryStarting(assembly, false, false, discoveryOptions));

                    controller.Find(includeSourceInformation: includeSourceInformation, messageSink: discoveryVisitor, discoveryOptions: discoveryOptions);
                    discoveryVisitor.Finished.WaitOne();

                    IDictionary<ITestCase, VsTestCase> vsTestCases = null;
                    if (designTime)
                        vsTestCases = DesignTimeTestConverter.Convert(discoveryVisitor.TestCases);

                    if (listTestCases)
                    {
                        lock (consoleLock)
                        {
                            if (designTime)
                            {
                                foreach (var testcase in vsTestCases.Values)
                                {
                                    _testDiscoverySink?.SendTest(testcase);

                                    Console.WriteLine(testcase.FullyQualifiedName);
                                }
                            }
                            else
                            {
                                foreach (var testcase in discoveryVisitor.TestCases)
                                    Console.WriteLine(testcase.DisplayName);
                            }
                        }

                        return assemblyElement;
                    }

                    IExecutionVisitor resultsVisitor;

                    if (designTime)
                    {
                        resultsVisitor = new DesignTimeExecutionVisitor(_testExecutionSink, vsTestCases, _reporterMessageHandler);
                    }
                    else
                        resultsVisitor = new XmlAggregateVisitor(_reporterMessageHandler, _completionMessages, assemblyElement, () => _cancel);

                    IList<ITestCase> filteredTestCases;
                    var testCasesDiscovered = discoveryVisitor.TestCases.Count;
                    if (!designTime || designTimeFullyQualifiedNames.Count == 0)
                        filteredTestCases = discoveryVisitor.TestCases.Where(filters.Filter).ToList();
                    else
                        filteredTestCases = vsTestCases.Where(t => designTimeFullyQualifiedNames.Contains(t.Value.FullyQualifiedName)).Select(t => t.Key).ToList();
                    var testCasesToRun = filteredTestCases.Count;

                    _reporterMessageHandler.OnMessage(new TestAssemblyDiscoveryFinished(assembly, discoveryOptions, testCasesDiscovered, testCasesToRun));

                    if (filteredTestCases.Count == 0)
                        _completionMessages.TryAdd(Path.GetFileName(assembly.AssemblyFilename), new ExecutionSummary());
                    else
                    {
                        _reporterMessageHandler.OnMessage(new TestAssemblyExecutionStarting(assembly, executionOptions));

                        controller.RunTests(filteredTestCases, resultsVisitor, executionOptions);
                        resultsVisitor.Finished.WaitOne();

                        _reporterMessageHandler.OnMessage(new TestAssemblyExecutionFinished(assembly, executionOptions, resultsVisitor.ExecutionSummary));
                    }
                }
            }
            catch (Exception ex)
            {
                _failed = true;

                var e = ex;
                while (e != null)
                {
                    Console.WriteLine("{0}: {1}", e.GetType().FullName, e.Message);
                    e = e.InnerException;
                }
            }

            return assemblyElement;
        }

        private static ISourceInformationProvider GetSourceInformationProviderAdapater(XunitProjectAssembly assembly)
        {
            var directoryPath = Path.GetDirectoryName(assembly.AssemblyFilename);
            var assemblyName = Path.GetFileNameWithoutExtension(assembly.AssemblyFilename);
            var pdbPath = Path.Combine(directoryPath, assemblyName + FileNameSuffixes.DotNet.ProgramDatabase);

            return File.Exists(pdbPath) 
                ? new SourceInformationProviderAdapater(new SourceInformationProvider(pdbPath, null)) 
                : null;
        }

        static Task<T> TaskRun<T>(Func<T> function)
        {
            var tcs = new TaskCompletionSource<T>();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    tcs.SetResult(function());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}
