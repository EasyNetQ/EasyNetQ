$repositoryRoot = [IO.Path]::GetFullPath($(Join-Path $PSScriptRoot "\..\"))
$sourceFolder = Join-Path $repositoryRoot "Source"

Write-Host "Repository: $repositoryRoot"
Write-Host "Source: $sourceFolder"

if (!(Test-Path $sourceFolder)) {
    Write-Error "Cannot find $sourceFolder"
}

$projects = Get-ChildItem $sourceFolder | ? { $_.PSIsContainer -and $_.Name.Contains("Tests") -and !$_.Name.Contains("Tests.") }

# upload results to AppVeyor
$wc = New-Object 'System.Net.WebClient'

foreach ($projectJson in $projects) {

    $directory = $projectJson.FullName

    $wc.UploadFile("https://ci.appveyor.com/api/testresults/nunit/$($env:APPVEYOR_JOB_ID)", (Join-Path $directory "\TestResult.xml"))

}

