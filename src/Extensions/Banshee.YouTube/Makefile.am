ASSEMBLY = Banshee.YouTube
TARGET = library
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS) $(LINK_GDATA)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.YouTube.Data/DataFetch.cs \
	Banshee.YouTube.Data/YouTubeData.cs \
	Banshee.YouTube.Gui/VideoStreamTile.cs \
	Banshee.YouTube.Gui/YouTubeTile.cs \
	Banshee.YouTube/ContextPage.cs \
	Banshee.YouTube/YouTubePane.cs

RESOURCES = Banshee.YouTube.addin.xml

if HAVE_GDATA
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

if HAVE_GDATASHARP_1_5
GMCS_FLAGS += "-define:HAVE_GDATASHARP_1_5"
endif

