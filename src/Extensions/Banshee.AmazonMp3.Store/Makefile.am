ASSEMBLY = Banshee.AmazonMp3.Store
TARGET = library
LINK = $(REF_EXTENSION_AMAZONMP3_STORE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.AmazonMp3.Store/StoreSource.cs \
	Banshee.AmazonMp3.Store/StoreSourcePreferences.cs \
	Banshee.AmazonMp3.Store/StoreView.cs \
	Banshee.AmazonMp3.Store/StoreWebBrowserShell.cs

RESOURCES = Banshee.AmazonMp3.Store.addin.xml

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

EXTRA_DIST += ThemeIcons/amazon-mp3-store-source.svg
