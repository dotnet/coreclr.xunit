$toolsPath = split-path $MyInvocation.MyCommand.Definition
$dnvm = join-path $toolsPath "dnvm.ps1"
$solutionPath = [System.IO.Path]::GetFullPath($(join-path $toolsPath ".."))
$globalJson = join-path $solutionPath "global.json"
$dnxVersion = (ConvertFrom-JSON ([System.IO.File]::ReadAllText($globalJson))).sdk.version

& $dnvm use $dnxVersion -runtime CLR -arch x86
& dnx -p $(join-path $solutionPath "test\test.xunit.runner.dnx") test
if ($LastExitCode -ne 0) { exit 1 }

& $dnvm use $dnxVersion -runtime CoreCLR -arch x86
& dnx -p $(join-path $solutionPath "test\test.xunit.runner.dnx") test
if ($LastExitCode -ne 0) { exit 1 }

& $dnvm use $dnxVersion -runtime CLR -arch x64
& dnx -p $(join-path $solutionPath "test\test.xunit.runner.dnx") test
if ($LastExitCode -ne 0) { exit 1 }

& $dnvm use $dnxVersion -runtime CoreCLR -arch x64
& dnx -p $(join-path $solutionPath "test\test.xunit.runner.dnx") test
if ($LastExitCode -ne 0) { exit 1 }
