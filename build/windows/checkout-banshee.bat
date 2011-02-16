REM This script will checkout Banshee from git (and a few submodules that are needed).
REM It will result in a banshee/ directory under where it's launched from.
REM See banshee\build\windows\README.txt for more information
REM (this file is tracked in version control at: http://git.gnome.org/browse/banshee/plain/build/windows/checkout-banshee.bat )

call git clone git://git.gnome.org/banshee
if not exist banshee goto failure

cd banshee
call git submodule update --init
call git clone git://gitorious.org/banshee/windows-binaries.git bin
if not exist bin goto failure

echo "Checkout script finished. Banshee is now checked out into the banshee folder.  Build it with build\windows\build-banshee.bat or your favorite IDE using Banshee.sln"
pause

:failure
echo "There was a problem during the checkout. Please see the message above."
