$toolsPath = split-path $MyInvocation.MyCommand.Definition
$dnvm = join-path $toolsPath "dnvm.ps1"
$solutionPath = [System.IO.Path]::GetFullPath($(join-path $toolsPath ".."))
$globalJson = join-path $solutionPath "global.json"
$dnxVersion = (ConvertFrom-JSON ([System.IO.File]::ReadAllText($globalJson))).sdk.version

& $dnvm install $dnxVersion -runtime CoreCLR -arch x86 -u
& $dnvm install $dnxVersion -runtime CLR     -arch x86 -u
& $dnvm install $dnxVersion -runtime CoreCLR -arch x64 -u
& $dnvm install $dnxVersion -runtime CLR     -arch x64 -u

& $dnvm use $dnxVersion -runtime CLR -arch x86

# Update build number during CI
if ($env:BuildSemanticVersion -ne $null) {
  $projectJson = join-path $solutionPath "src\xunit.runner.dnx\project.json"
  $content = get-content $projectJson
  $content = $content.Replace("99.99.99-dev", $env:BuildSemanticVersion)
  set-content $projectJson $content -encoding UTF8
}

# Restore packages and build
& dnu restore $solutionPath
& dnu pack $(join-path $solutionPath "src\xunit.runner.dnx") --configuration Release
