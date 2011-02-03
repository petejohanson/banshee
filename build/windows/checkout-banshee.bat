REM This script will checkout Banshee from git (and a few submodules that are needed).
REM It will result in a banshee/ directory under where it's launched from.
REM See banshee\build\windows\README.txt for more information
REM (this file is tracked in version control at: http://git.gnome.org/browse/banshee/plain/build/windows/checkout-banshee.bat )

REM for some reason need to put these all on one line w/ && between; as separate commands on separate lines it stops after a given git cmd
git clone git://git.gnome.org/banshee && cd banshee && git submodule update --init && git clone git://gitorious.org/banshee/windows-binaries.git bin && echo "Checkout script finished. Banshee is now checked out into the banshee folder.  Build it with build\windows\build-banshee.bat or your favorite IDE using Banshee.sln" && pause
