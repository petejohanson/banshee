REM This script only needs to be run by maintainers updating the bundled
REM dependencies.  It assumes you have these packages installed:
REM http://ftp.novell.com/pub/mono/gtk-sharp/gtk-sharp-2.12.10.win32.msi
REM http://ossbuild.googlecode.com/files/GStreamer-WinBuilds-GPL-x86.msi
REM http://ossbuild.googlecode.com/files/GStreamer-WinBuilds-LGPL-x86.msi
REM http://ossbuild.googlecode.com/files/GStreamer-WinBuilds-SDK-LGPL-x86.msi

REM ==================================
REM Copy Gtk+ and Gtk#
REM ==================================
xcopy /SYQ "C:\Program Files\GtkSharp\2.12\*" ..\..\bin\

REM ==================================
REM Copy GStreamer and gstreamer-sharp
REM ==================================
xcopy /SYQ "C:\Program Files\OSSBuild\GStreamer\v0.10.6\bin\*.dll" ..\..\bin\bin\
xcopy /SYQ "C:\Program Files\OSSBuild\GStreamer\v0.10.6\lib\gstreamer-0.10\*.dll" ..\..\bin\lib\gstreamer-0.10\
xcopy /SYQ "C:\Program Files\OSSBuild\GStreamer\v0.10.6\sdk\bindings\dotnet\*.dll" ..\..\bin\bin\
mkdir ..\..\bin\share\licenses
xcopy /SYQ "C:\Program Files\OSSBuild\GStreamer\v0.10.6\share\licenses" ..\..\bin\share\licenses

REM ==================================
REM Move some files around
REM ==================================
move ..\..\bin\lib\Mono.Cairo\* ..\..\bin\bin\
move ..\..\bin\lib\Mono.Posix\* ..\..\bin\bin\
move ..\..\bin\lib\gtk-sharp-2.0\* ..\..\bin\bin\
xcopy /SYQ ..\..\COPYING ..\..\bin\share\licenses\banshee.txt

REM ==================================
REM Delete files we don't need
REM ==================================
rmdir ..\..\bin\lib\Mono.Cairo
rmdir ..\..\bin\lib\Mono.Posix
rmdir ..\..\bin\lib\gtk-sharp-2.0
rmdir /SQ ..\..\bin\lib1
del ..\..\bin\bin\*.exe
del ..\..\bin\lib\gstreamer-0.10\libgstpython-v2.6.dll

REM ==================================
REM OTHER DEPS, manually copied
REM ==================================
REM sqlite: http://sqlite.org/sqlite-dll-win32-x86-3070500.zip
REM Mono.ZeroConf: http://download.banshee-project.org/mono-zeroconf/mono-zeroconf-0.9.0-binary.zip
REM ICSharpCode.SharpZipLib, Mono.Addins.*: from MonoDevelop's bin/ dir

REM taglib-sharp
REM Google.GData

REM TODO NDesk.DBus won't be needed with latexer's remoting patch
REM NDesk.DBus

REM TODO not yet used in the Windows build
REM gio-sharp
REM gtk-sharp-beans
REM libwebkit
