[CmdletBinding()]
param(
    [string[]]$ExcludeUpdatingDependencies = @("EasyNetQ.Management.Client", "EasyNetQ.Tests.Common", "EasyNetQ.Tests.Tasks")
)

$repositoryRoot = [IO.Path]::GetFullPath($(Join-Path $PSScriptRoot "\..\"))
$sourceFolder = Join-Path $repositoryRoot "Source"

Write-Host "Repository: $repositoryRoot"
Write-Host "Source: $sourceFolder"

if (!(Test-Path $sourceFolder)) {
    Write-Error "Cannot find $sourceFolder"
}

$projects = Get-ChildItem $sourceFolder\project.json -Recurse
$projectsExcludingTests = $projects | ? { !$_.Directory.Name.Contains("Tests") }

# Update the version number in all of the project.jsons

foreach ($projectJson in $projectsExcludingTests) {

    $directory = $projectJson.DirectoryName

    pushd $directory

    Write-Host "Updating version for $($projectJson.Directory.Name)..."

    if (!(Test-Path $(Join-Path $directory "project.lock.json"))) {
        & dotnet restore
    }
    
    & dotnet gitversion

    popd
}

# Update the dependency in each of the project.jsons
# ie) "EasyNetQ" : "99.0.0-dev", we want updated to "EasyNetQ": "<new version>"
# Or else, when we do a dotnet-pack, it will specify 99.0.0-dev in the package
# for the dependency EasyNetQ.
$regex = "NuGetVersion: (?<version>.+)"
$gitVersionCache = Get-ChildItem $([IO.Path]::Combine($repositoryRoot, ".git", "gitversion_cache")) `
    | Sort-Object -Descending LastWriteTimeUtc `
    | Select -First 1 `
    | select-string $regex

if ($gitVersionCache -eq $null -or @($gitVersionCache).Count -eq 0) {
    Write-Warning "Could not find gitversion_cache and a match to $regex.  Please run dotnet gitversion."
    return
}

$version = $gitVersionCache.Matches[0].Groups["version"].Value

Write-Host "Getting version from: $($gitVersionCache.Path)"

foreach ($projectJson in $projects) {
    $json = ConvertFrom-Json "$(Get-Content $projectJson.FullName)"

    Write-Host "$($projectJson.Directory.Name): Updating dependencies..."

    $dependencies = $json.dependencies.PsObject.Members `
        | ? { $_.MemberType -eq "NoteProperty" -and $_.Name.StartsWith("EasyNetQ") -and !$ExcludeUpdatingDependencies.Contains($_.Name) } `
        | foreach {
                Write-Host "    - $($_.Name): Updating $($_.Value) -> $version"
                $_.Value = $version
            }

    $dependencies = $json.frameworks.PsObject.Members | foreach { $_.Value.dependencies.PsObject.Members } `
        | ? { $_.MemberType -eq "NoteProperty" -and $_.Name.StartsWith("EasyNetQ") -and !$ExcludeUpdatingDependencies.Contains($_.Name) } `
        | foreach {
                Write-Host "    - $($_.Name): Updating $($_.Value) -> $version"
                $_.Value = $version
            }
        
    ConvertTo-Json $json -Depth 99 | Set-Content $projectJson.FullName
}