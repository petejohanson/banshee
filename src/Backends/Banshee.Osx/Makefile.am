ASSEMBLY = Banshee.Osx
ASSEMBLY_BUILD_FLAGS = -unsafe
TARGET = library
LINK = $(REF_BACKEND_OSX)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.OsxBackend/HardwareManager.cs \
	Banshee.OsxBackend/OsxService.cs \
	OsxIntegration.Framework/AppleEvent.cs \
	OsxIntegration.Framework/ApplicationEvents.cs \
	OsxIntegration.Framework/Carbon.cs \
	OsxIntegration.Framework/CoreFoundation.cs \
	OsxIntegration.Framework/HIToolbox.cs \
	OsxIntegration.Framework/NavDialog.cs \
	OsxIntegration.Ige/IgeMacMenu.cs \
	OsxIntegration.Ige/IgeMacMenuGroup.cs 

RESOURCES = Banshee.Osx.addin.xml

if ENABLE_OSX
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

MAINTAINERCLEANFILES = Makefile.in
