ASSEMBLY = Banshee.GStreamerSharp
TARGET = library
LINK = $(REF_BACKEND_GSTREAMER) $(GST_SHARP_LIBS)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.GStreamerSharp/PlayerEngine.cs

RESOURCES = Banshee.GStreamerSharp.addin.xml

if ENABLE_GST_SHARP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

if ENABLE_GAPLESS
BUILD_DEFINES="-define:ENABLE_GAPLESS"
endif
