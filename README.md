## <a href="https://github.com/xunit/xunit"><img src="https://raw.github.com/xunit/media/master/full-logo.png" title="xUnit.net DNX Runner" /></a>

This runner supports [xUnit.net](https://github.com/xunit/xunit) tests for [DNX 4.5.1+, and DNX Core 5+](https://github.com/aspnet/dnx) (this includes [ASP.NET 5+](https://github.com/aspnet)).

### Usage

To install this package, ensure your project.json contains the following lines:

```JSON
{
    "dependencies": {
        "xunit": "2.1.0-*",
        "xunit.runner.dnx": "2.1.0-*"
    },
    "commands": {
        "test": "xunit.runner.dnx"
    }
}
```

To run tests from the command line, use the following.

```Shell
# Restore NuGet packages
dnu restore

# Run tests in current directory
dnx test

# Run tests if tests are not in the current directory
dnx -p path/to/project test
```

### More Information

For more complete example usage, please see [Getting Started with xUnit.net and DNX / ASP.NET 5](http://xunit.github.io/docs/getting-started-dnx.html).
