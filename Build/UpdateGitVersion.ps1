$repositoryRoot = [IO.Path]::GetFullPath($(Join-Path $PSScriptRoot "\..\"))
$sourceFolder = Join-Path $repositoryRoot "Source"

Write-Host "Repository: $repositoryRoot"
Write-Host "Source: $sourceFolder"

if (!(Test-Path $sourceFolder)) {
    Write-Error "Cannot find $sourceFolder"
}

$projects = Get-ChildItem $sourceFolder | ? { $_.PSIsContainer -and !$_.Name.Contains("Tests") -and !$_.Name.Contains("Hosepipe.Setup") }

foreach ($projectJson in $projects) {

    $directory = $projectJson.FullName
        
    pushd $directory

    if (!(Test-Path $(Join-Path $directory "project.lock.json"))) {
        & dotnet restore
    }
    
    & dotnet gitversion

    popd
}