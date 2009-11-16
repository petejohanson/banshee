ASSEMBLY = Banshee.LibraryWatcher
TARGET = library
LINK = $(REF_EXTENSION_LIBRARYWATCHER)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.LibraryWatcher/FileSystemWatcherProxy.cs \
	Banshee.LibraryWatcher/IO/FileAction.cs \
	Banshee.LibraryWatcher/IO/FileSystemWatcher.cs \
	Banshee.LibraryWatcher/IO/IFileWatcher.cs \
	Banshee.LibraryWatcher/IO/InotifyWatcher.cs \
	Banshee.LibraryWatcher/IO/SearchPattern.cs \
	Banshee.LibraryWatcher/LibraryWatcherService.cs \
	Banshee.LibraryWatcher/SourceWatcher.cs

RESOURCES = Banshee.LibraryWatcher.addin.xml

include $(top_srcdir)/build/build.mk

