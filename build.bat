@echo off
setlocal EnableDelayedExpansion

set ScriptRoot=%~dp0
set VisualStudioInstallation=%ProgramFiles(x86)%\Microsoft Visual Studio\2019

echo Searching for Visual Studio in: "%VisualStudioInstallation%"

if defined DevEnvDir (
    echo Visual Studio 2019 development environment has already been set.
) else (
    echo Setting Visual Studio 2019 environment...

    for /F %%G in ('dir /b "%VisualStudioInstallation%"') do (

        set VisualStudioCommonTools=!VisualStudioInstallation!\%%G\Common7\Tools\VsDevCmd.bat

        if exist "!VisualStudioCommonTools!" (
            call "!VisualStudioCommonTools!"

            goto :RunBuild
        ) else (
            echo ERROR: Could not locate "!VisualStudioCommonTools!".  Trying next installation...
        )
    )
)

:RunBuild

if not defined DevEnvDir (
    echo Visual Studio 2019 environment should have been set.
    exit /b 
)

msbuild %ScriptRoot%\Build\EasyNetQ.proj

pause
