using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runner.DotNet;

public class CommandLineTests
{
    public class UnknownSwitch
    {
        [Fact]
        public static void UnknownSwitchThrows()
        {
            var exception = Record.Exception(() => TestableCommandLine.Parse(new[] { "assemblyName.dll", "-unknown" }));

            Assert.IsType<ArgumentException>(exception);
            Assert.Equal("unknown option: -unknown", exception.Message);
        }
    }

    public class Filename
    {
        [Fact]
        public static void MissingAssemblyFileNameThrows()
        {
            var exception = Record.Exception(() => TestableCommandLine.Parse());

            Assert.IsType<ArgumentException>(exception);
            Assert.Equal("must specify at least one assembly", exception.Message);
        }

        [Fact]
        public static void ConfigFileDoesNotExist_Throws()
        {
            var arguments = new[] { "assemblyName.dll", "badConfig.json" };

            var exception = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(exception);
            Assert.Equal("config file not found: badConfig.json", exception.Message);
        }
    }

    public class DiagnosticsOption
    {
        [Fact]
        public static void DiagnosticsNotSetDebugIsFalse()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.False(commandLine.DiagnosticMessages);
        }

        [Fact]
        public static void DiagnosticsSetDebugIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-diagnostics" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.DiagnosticMessages);
        }
    }

    public class DebugOption
    {
        [Fact]
        public static void DebugNotSetDebugIsFalse()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.False(commandLine.Debug);
        }

        [Fact]
        public static void DebugSetDebugIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-debug" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.Debug);
        }
    }

    public class MaxThreadsOption
    {
        [Fact]
        public static void DefaultValueIsNull()
        {
            var commandLine = TestableCommandLine.Parse("assemblyName.dll");

            Assert.Null(commandLine.MaxParallelThreads);
        }

        [Fact]
        public static void MissingValue()
        {
            var ex = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-maxthreads"));

            Assert.Equal("missing argument for -maxthreads", ex.Message);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("abc")]
        public static void InvalidValues(string value)
        {
            var ex = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-maxthreads", value));

            Assert.Equal("incorrect argument value for -maxthreads (must be 'default', 'unlimited', or a positive number)", ex.Message);
        }

        [Theory]
        [InlineData("default", 0)]
        [InlineData("unlimited", -1)]
        [InlineData("16", 16)]
        public static void ValidValues(string value, int expected)
        {
            var commandLine = TestableCommandLine.Parse("assemblyName.dll", "-maxthreads", value);

            Assert.Equal(expected, commandLine.MaxParallelThreads);
        }
    }

    public class NoLogoOption
    {
        [Fact]
        public static void NoLogoNotSetNoLogoIsFalse()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.False(commandLine.NoLogo);
        }

        [Fact]
        public static void NoLogoSetNoLogoIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-nologo" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.NoLogo);
        }
    }

    public class WaitOption
    {
        [Fact]
        public static void WaitOptionNotPassedWaitFalse()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.False(commandLine.Wait);
        }

        [Fact]
        public static void WaitOptionWaitIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-wait" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.Wait);
        }

        [Fact]
        public static void WaitOptionIgnoreCaseWaitIsTrue()
        {
            var arguments = new[] { "assemblyName.dll", "-wAiT" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.Wait);
        }
    }

    public class TraitArgument
    {
        [Fact]
        public static void TraitArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.IncludedTraits.Count);
        }

        [Fact]
        public static void SingleValidTraitArgument()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.IncludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_SameName()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar", "-trait", "foo=baz" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits.Count);
            Assert.Equal(2, commandLine.Project.Filters.IncludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.IncludedTraits["foo"]);
            Assert.Contains("baz", commandLine.Project.Filters.IncludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_DifferentName()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar", "-trait", "baz=biff" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.IncludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.IncludedTraits["foo"]);
            Assert.Equal(1, commandLine.Project.Filters.IncludedTraits["baz"].Count());
            Assert.Contains("biff", commandLine.Project.Filters.IncludedTraits["baz"]);
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-trait" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -trait", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingEquals()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foobar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingName()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "=bar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionNameMissingValue()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void TooManyEqualsSigns()
        {
            var arguments = new[] { "assemblyName.dll", "-trait", "foo=bar=baz" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -trait (should be \"name=value\")", ex.Message);
        }
    }

    public class MinusTraitArgument
    {
        [Fact]
        public static void TraitArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.ExcludedTraits.Count);
        }

        [Fact]
        public static void SingleValidTraitArgument()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.ExcludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_SameName()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar", "-notrait", "foo=baz" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits.Count);
            Assert.Equal(2, commandLine.Project.Filters.ExcludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.ExcludedTraits["foo"]);
            Assert.Contains("baz", commandLine.Project.Filters.ExcludedTraits["foo"]);
        }

        [Fact]
        public static void MultipleValidTraitArguments_DifferentName()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar", "-notrait", "baz=biff" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.ExcludedTraits.Count);
            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits["foo"].Count());
            Assert.Contains("bar", commandLine.Project.Filters.ExcludedTraits["foo"]);
            Assert.Equal(1, commandLine.Project.Filters.ExcludedTraits["baz"].Count());
            Assert.Contains("biff", commandLine.Project.Filters.ExcludedTraits["baz"]);
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -notrait", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingEquals()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foobar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionValueMissingName()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "=bar" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void OptionNameMissingValue()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }

        [Fact]
        public static void TooManyEqualsSigns()
        {
            var arguments = new[] { "assemblyName.dll", "-notrait", "foo=bar=baz" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("incorrect argument format for -notrait (should be \"name=value\")", ex.Message);
        }
    }

    public class MethodArgument
    {
        [Fact]
        public static void MethodArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.IncludedMethods.Count);
        }

        [Fact]
        public static void SingleValidMethodArgument()
        {
            const string name = "Namespace.Class.Method1";

            var arguments = new[] { "assemblyName.dll", "-method", name };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedMethods.Count);
            Assert.True(commandLine.Project.Filters.IncludedMethods.Contains(name));
        }

        [Fact]
        public static void MultipleValidMethodArguments()
        {
            const string name1 = "Namespace.Class.Method1";
            const string name2 = "Namespace.Class.Method2";

            var arguments = new[] { "assemblyName.dll", "-method", name1, "-method", name2 };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.IncludedMethods.Count);
            Assert.True(commandLine.Project.Filters.IncludedMethods.Contains(name1));
            Assert.True(commandLine.Project.Filters.IncludedMethods.Contains(name2));
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-method" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -method", ex.Message);
        }
    }

    public class ClassArgument
    {
        [Fact]
        public static void ClassArgumentNotPassed()
        {
            var arguments = new[] { "assemblyName.dll" };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(0, commandLine.Project.Filters.IncludedMethods.Count);
        }

        [Fact]
        public static void SingleValidClassArgument()
        {
            const string name = "Namespace.Class";

            var arguments = new[] { "assemblyName.dll", "-class", name };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(1, commandLine.Project.Filters.IncludedClasses.Count);
            Assert.True(commandLine.Project.Filters.IncludedClasses.Contains(name));
        }

        [Fact]
        public static void MultipleValidClassArguments()
        {
            const string name1 = "Namespace.Class1";
            const string name2 = "Namespace.Class2";

            var arguments = new[] { "assemblyName.dll", "-class", name1, "-class", name2 };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(2, commandLine.Project.Filters.IncludedClasses.Count);
            Assert.True(commandLine.Project.Filters.IncludedClasses.Contains(name1));
            Assert.True(commandLine.Project.Filters.IncludedClasses.Contains(name2));
        }

        [Fact]
        public static void MissingOptionValue()
        {
            var arguments = new[] { "assemblyName.dll", "-class" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing argument for -class", ex.Message);
        }
    }

    public class ParallelizationOptions
    {
        [Fact]
        public static void ParallelizationOptionsAreNullByDefault()
        {
            var project = TestableCommandLine.Parse("assemblyName.dll");

            Assert.Null(project.ParallelizeAssemblies);
            Assert.Null(project.ParallelizeTestCollections);
        }

        [Fact]
        public static void FailsWithoutOptionOrWithIncorrectOptions()
        {
            var aex1 = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-parallel"));
            Assert.Equal("missing argument for -parallel", aex1.Message);

            var aex2 = Assert.Throws<ArgumentException>(() => TestableCommandLine.Parse("assemblyName.dll", "-parallel", "nonsense"));
            Assert.Equal("incorrect argument value for -parallel", aex2.Message);
        }

        [Theory]
        [InlineData("None", false, false)]
        [InlineData("Assemblies", true, false)]
        [InlineData("Collections", false, true)]
        [InlineData("All", true, true)]
        public static void ParallelCanBeTurnedOn(string parallelOption, bool expectedAssemblyParallelization, bool expectedCollectionsParallelization)
        {

            var project = TestableCommandLine.Parse("assemblyName.dll", "-parallel", parallelOption);

            Assert.Equal(expectedAssemblyParallelization, project.ParallelizeAssemblies);
            Assert.Equal(expectedCollectionsParallelization, project.ParallelizeTestCollections);
        }
    }

    public class Reporters
    {
        [Fact]
        public void NoReporters_UsesDefaultReporter()
        {
            var commandLine = TestableCommandLine.Parse("assemblyName.dll");

            Assert.IsType<DefaultRunnerReporter>(commandLine.Reporter);
        }

        [Fact]
        public void NoExplicitReporter_NoEnvironmentallyEnabledReporters_UsesDefaultReporter()
        {
            var implicitReporter = new MockRunnerReporter(isEnvironmentallyEnabled: false);

            var commandLine = TestableCommandLine.Parse(new[] { implicitReporter }, "assemblyName.dll");

            Assert.IsType<DefaultRunnerReporter>(commandLine.Reporter);
        }

        [Fact]
        public void ExplicitReporter_NoEnvironmentalOverride_UsesExplicitReporter()
        {
            var explicitReporter = new MockRunnerReporter("switch");

            var commandLine = TestableCommandLine.Parse(new[] { explicitReporter }, "assemblyName.dll", "-switch");

            Assert.Same(explicitReporter, commandLine.Reporter);
        }

        [Fact]
        public void ExplicitReporter_WithEnvironmentalOverride_UsesEnvironmentalOverride()
        {
            var explicitReporter = new MockRunnerReporter("switch");
            var implicitReporter = new MockRunnerReporter(isEnvironmentallyEnabled: true);

            var commandLine = TestableCommandLine.Parse(new[] { explicitReporter, implicitReporter }, "assemblyName.dll", "-switch");

            Assert.Same(implicitReporter, commandLine.Reporter);
        }

        [Fact]
        public void NoExplicitReporter_SelectsFirstEnvironmentallyEnabledReporter()
        {
            var explicitReporter = new MockRunnerReporter("switch");
            var implicitReporter1 = new MockRunnerReporter(isEnvironmentallyEnabled: true);
            var implicitReporter2 = new MockRunnerReporter(isEnvironmentallyEnabled: true);

            var commandLine = TestableCommandLine.Parse(new[] { explicitReporter, implicitReporter1, implicitReporter2 }, "assemblyName.dll");

            Assert.Same(implicitReporter1, commandLine.Reporter);
        }
    }

    public class Transform
    {
        [Fact]
        public static void OutputMissingFilename()
        {
            var arguments = new[] { "assemblyName.dll", "-xml" };

            var ex = Record.Exception(() => TestableCommandLine.Parse(arguments));

            Assert.IsType<ArgumentException>(ex);
            Assert.Equal("missing filename for -xml", ex.Message);
        }

        [Fact]
        public static void Output()
        {
            var arguments = new[] { "assemblyName.dll", "-xml", "foo.xml" };

            var commandLine = TestableCommandLine.Parse(arguments);

            var output = Assert.Single(commandLine.Project.Output);
            Assert.Equal("xml", output.Key);
            Assert.Equal("foo.xml", output.Value);
        }
    }

    public class DesignTimeSwitch
    {
        [Theory]
        [InlineData("-designtime")]
        [InlineData("--designtime")]
        public static void DesignTime(string arg)
        {
            var arguments = new[] { "assemblyName.dll", arg };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.DesignTime);
        }
    }

    public class ListSwitch
    {
        [Theory]
        [InlineData("-list")]
        [InlineData("--list")]
        public static void List(string arg)
        {
            var arguments = new[] { "assemblyName.dll", arg };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.True(commandLine.List);
        }
    }

    public class TestArgument
    {
        [Fact]
        public static void TestUniqueNames()
        {
            var arguments = new[]
            {
                "assemblyName.dll",
                "-test",
                "foo",
                "--test",
                "bar",
                "--test",
                "baz",
            };

            var commandLine = TestableCommandLine.Parse(arguments);

            Assert.Equal(3, commandLine.DesignTimeTestUniqueNames.Count);
            Assert.Contains("foo", commandLine.DesignTimeTestUniqueNames);
            Assert.Contains("bar", commandLine.DesignTimeTestUniqueNames);
            Assert.Contains("baz", commandLine.DesignTimeTestUniqueNames);
        }
    }

    class MockRunnerReporter : IRunnerReporter
    {
        // Need this here so the runner doesn't complain that this isn't a legal runner reporter. :-p
        public MockRunnerReporter() { }

        public MockRunnerReporter(string runnerSwitch = null, bool isEnvironmentallyEnabled = false)
        {
            RunnerSwitch = runnerSwitch;
            IsEnvironmentallyEnabled = isEnvironmentallyEnabled;
        }

        public string Description { get { return "The description"; } }

        public bool IsEnvironmentallyEnabled { get; private set; }

        public string RunnerSwitch { get; private set; }

        public IMessageSink CreateMessageHandler(IRunnerLogger logger)
        {
            throw new NotImplementedException();
        }
    }

    class TestableCommandLine : CommandLine
    {
        private TestableCommandLine(IReadOnlyList<IRunnerReporter> reporters, params string[] arguments)
            : base(reporters, arguments, filename => filename != "badConfig.json")
        {
        }

        public static TestableCommandLine Parse(params string[] arguments)
        {
            return new TestableCommandLine(new IRunnerReporter[0], arguments);
        }

        public new static TestableCommandLine Parse(IReadOnlyList<IRunnerReporter> reporters, params string[] arguments)
        {
            return new TestableCommandLine(reporters, arguments);
        }
    }
}
