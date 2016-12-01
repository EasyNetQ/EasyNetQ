$repositoryRoot = [IO.Path]::GetFullPath($(Join-Path $PSScriptRoot "\..\"))
$sourceFolder = Join-Path $repositoryRoot "Source"

Write-Host "Repository: $repositoryRoot"
Write-Host "Source: $sourceFolder"

if (!(Test-Path $sourceFolder)) {
    Write-Error "Cannot find $sourceFolder"
}

$projects = Get-ChildItem $sourceFolder\project.json -Recurse | ? { !$_.Directory.Name.Contains("Tests") }

foreach ($projectJson in $projects) {

    $directory = $projectJson.DirectoryName
        
    pushd $directory

    if (!(Test-Path $(Join-Path $directory "project.lock.json"))) {
        & dotnet restore
    }
    
    & dotnet gitversion

    popd
}