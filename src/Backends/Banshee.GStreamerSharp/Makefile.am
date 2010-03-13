ASSEMBLY = Banshee.GStreamerSharp
TARGET = library
LINK = $(REF_BACKEND_GSTREAMER)
SOURCES =  \
	Banshee.GStreamerSharp/PlayerEngine.cs

RESOURCES = Banshee.GStreamerSharp.addin.xml
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

include $(top_srcdir)/build/build.mk

if ENABLE_GAPLESS
BUILD_DEFINES="-define:ENABLE_GAPLESS"
endif
