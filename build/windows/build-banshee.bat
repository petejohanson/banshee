@ECHO OFF
SET v40-30319="%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild"
SET v35="%WINDIR%\Microsoft.NET\Framework\v3.5\msbuild"
CD ..\..
ECHO "Looking for Microsoft.NET MSBuild..."
IF EXIST %v40-30319% (
ECHO "Building with Microsoft.NET v4.0 MSBuild"
%v40-30319% Banshee.sln /p:Configuration=Windows /p:Platform="Any CPU"
) ELSE IF EXIST "%v35%" (
ECHO "Building with Microsoft.NET v3.5 MSBuild"
%v35% Banshee.sln /p:Configuration=Windows /p:Platform="Any CPU"
) ELSE (
ECHO "Build failed: Microsoft.NET MSBuild (msbuild.exe) not found"
)
ECHO 'Running "post-build.bat"'
build\windows\post-build.bat
CD build\windows
