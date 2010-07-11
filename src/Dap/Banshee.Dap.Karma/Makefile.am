ASSEMBLY = Banshee.Dap.Karma
TARGET = library
LINK = $(REF_DAP_KARMA)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Dap.Karma/KarmaSource.cs \
	Banshee.Dap.Karma/KarmaTrackInfo.cs

RESOURCES = Banshee.Dap.Karma.addin.xml

EXTRA_BUNDLE = $(KARMASHARP_ASSEMBLIES)

if ENABLE_KARMA
include $(top_srcdir)/build/build.mk

define EXTRA_INSTALL_DATA_HOOK
	for ASM in $(KARMASHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;
endef

define EXTRA_UNINSTALL_HOOK
	for ASM in $(KARMASHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;
endef

else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
