ASSEMBLY = Banshee.Dap.AppleDevice
TARGET = library
LINK = $(REF_DAP_APPLEDEVICE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Dap.AppleDevice/AppleDeviceSource.cs \
	Banshee.Dap.AppleDevice/AppleDeviceTrackInfo.cs

RESOURCES = Banshee.Dap.AppleDevice.addin.xml

EXTRA_BUNDLE = $(LIBGPODSHARP_ASSEMBLIES)

if ENABLE_APPLEDEVICE
include $(top_srcdir)/build/build.mk

define EXTRA_INSTALL_DATA_HOOK
	for ASM in $(LIBGPODSHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;
endef

define EXTRA_UNINSTALL_HOOK
	for ASM in $(LIBGPODSHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;
endef

else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

