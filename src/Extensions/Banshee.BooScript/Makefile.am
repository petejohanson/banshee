ASSEMBLY = Banshee.BooScript
TARGET = library
LINK = $(REF_EXTENSION_BOOSCRIPT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.BooScript/BooScriptService.cs

DIST_RESOURCES =  \
	Resources/duration-status-formats.boo \
	Resources/filename-transform.boo \
	Resources/vorbis-oga-extension.boo

RESOURCES =  \
	Banshee.BooScript.addin.xml \
	$(DIST_RESOURCES)

if HAVE_BOO
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(DIST_RESOURCES) Banshee.BooScript.addin.xml.in
endif

