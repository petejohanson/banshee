ASSEMBLY = Banshee.MiroGuide
TARGET = library
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS) $(LINK_BANSHEE_WEBBROWSER_DEPS) 

SOURCES = \
	Banshee.MiroGuide/MiroGuideSource.cs \
	Banshee.MiroGuide/MiroGuideView.cs

RESOURCES = \
	Banshee.MiroGuide.addin.xml \
	Resources/loading.html

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
