ASSEMBLY = Banshee.YouTube
TARGET = library
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS) $(LINK_GDATA)

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
