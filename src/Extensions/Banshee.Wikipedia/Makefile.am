ASSEMBLY = Banshee.Wikipedia
TARGET = library
LINK = $(REF_EXTENSION_WIKIPEDIA)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Wikipedia/ContextPage.cs \
	Banshee.Wikipedia/WikipediaView.cs

RESOURCES = Banshee.Wikipedia.addin.xml

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES) $(shell find $(srcdir)/ThemeIcons -type f)
endif

