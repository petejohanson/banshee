ASSEMBLY = Banshee.MiroGuide
TARGET = library
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS) $(LINK_BANSHEE_WEBBROWSER_DEPS) 

SOURCES =  \
	Banshee.MiroGuide/MiroGuideSource.cs \
	Banshee.MiroGuide/View.cs

RESOURCES = Banshee.MiroGuide.addin.xml

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
