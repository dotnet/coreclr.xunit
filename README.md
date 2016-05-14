## <a href="https://github.com/xunit/xunit"><img src="https://raw.github.com/xunit/media/master/full-logo.png" title="xUnit.net CoreCLR Runner" /></a>

This runner supports [xUnit.net](https://github.com/xunit/xunit) tests for [dotnet 4.5.1+, and dotnet Core 5+](https://github.com/dotnet/corefx) (this includes [ASP.NET 5+](https://github.com/aspnet)).

![](https://mseng.visualstudio.com/_apis/public/build/definitions/d09b7a4d-0a51-4c0e-a15a-07921d5b558f/3249/badge)

### Usage

To install this package, ensure your project.json contains the following lines:

```JSON
{
    "dependencies": {
        "xunit": "2.1.0-*",
        "dotnet-test-xunit": "2.1.0-*"
    },
    "testRunner": "dotnet-test-xunit"
}
```

To run tests from the command line, use the following.

```Shell
# Restore NuGet packages
dotnet restore

# Run tests in current directory
dotnet test

# Run tests if tests are not in the current directory
dotnet -p path/to/project test // not yet implemented
```

### More Information

For more complete example usage, please see [Getting Started with xUnit.net and DNX / ASP.NET 5](https://xunit.github.io/docs/getting-started-dnx.html).
