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

install-data-hook:
	for ASM in $(LIBGPODSHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(LIBGPODSHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

