== Building ==

See http://banshee.fm/download/development/#windows for instructions for building
Banshee on Windows.

== Creating the Banshee.msi installer ==

You need
- WIX 3.5 installed
- Banshee built

With that, you should be able to run build-installer.js and have it produce the
installer.

== Updating Bundled Deps ==

The bundle-deps.bat script will copy Gtk# and GStreamer into Banshee's bin/
directory.  It only needs to be run by maintainers updating the bundled deps.
See the script for which packages you need to have installed.
