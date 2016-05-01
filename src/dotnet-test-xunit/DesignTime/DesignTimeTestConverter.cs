using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit.Abstractions;
using VsTestCase = Microsoft.Extensions.Testing.Abstractions.Test;

namespace Xunit.Runner.DotNet
{
    public static class DesignTimeTestConverter
    {
        const string Ellipsis = "...";
        const int MaximumDisplayNameLength = 447;

        private readonly static HashAlgorithm Hash = SHA1.Create();

        public static IDictionary<ITestCase, VsTestCase> Convert(IEnumerable<ITestCase> testcases)
        {
            // When tests have the same class name and method name, generate unique names for display
            var groups = testcases
                .Select(tc => new
                {
                    testcase = tc,
                    shortName = Escape(tc.DisplayName),
                    fullyQualifiedName = string.Format("{0}.{1}", tc.TestMethod.TestClass.Class.Name, tc.TestMethod.Method.Name)
                })
                .GroupBy(tc => tc.fullyQualifiedName);

            var results = new Dictionary<ITestCase, VsTestCase>();
            foreach (var group in groups)
            {
                var uniquifyNames = group.Count() > 1;
                foreach (var testcase in group)
                {
                    results.Add(
                        testcase.testcase,
                        Convert(
                            testcase.testcase,
                            testcase.shortName,
                            testcase.fullyQualifiedName,
                            uniquifyNames));
                }
            }

            return results;
        }

        static string Escape(string value)
        {
            if (value == null)
                return string.Empty;

            return Truncate(value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t"));
        }

        static string Truncate(string value)
        {
            if (value.Length <= MaximumDisplayNameLength)
                return value;

            return value.Substring(0, MaximumDisplayNameLength - Ellipsis.Length) + Ellipsis;
        }

        private static VsTestCase Convert(
            ITestCase testcase,
            string shortName,
            string fullyQualifiedName,
            bool uniquifyNames)
        {
            string uniqueName;
            if (uniquifyNames)
                uniqueName = string.Format("{0}({1})", fullyQualifiedName, testcase.UniqueID);
            else
                uniqueName = fullyQualifiedName;

            var result = new VsTestCase();
            result.DisplayName = shortName;
            result.FullyQualifiedName = uniqueName;

            result.Id = GuidFromString(testcase.UniqueID);

            if (testcase.SourceInformation != null)
            {
                result.CodeFilePath = testcase.SourceInformation.FileName;
                result.LineNumber = testcase.SourceInformation.LineNumber;
            }

            return result;
        }

        private static Guid GuidFromString(string data)
        {
            var hash = Hash.ComputeHash(Encoding.Unicode.GetBytes(data));
            var b = new byte[16];
            Array.Copy((Array)hash, (Array)b, 16);
            return new Guid(b);
        }
    }
}
