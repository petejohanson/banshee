ASSEMBLY = Banshee.LibraryWatcher
TARGET = library
LINK = $(REF_EXTENSION_LIBRARYWATCHER)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.LibraryWatcher/LibraryWatcherService.cs \
	Banshee.LibraryWatcher/SourceWatcher.cs

RESOURCES = Banshee.LibraryWatcher.addin.xml

if ENABLE_LIBRARY_WATCHER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
