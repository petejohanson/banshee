ASSEMBLY = Banshee
TARGET = exe
LINK = $(REF_BOOTER)
SOURCES = Booter/Entry.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = banshee-1
EXTRA_DIST += Banshee.exe.config
module_SCRIPTS += Banshee.exe.config

if PLATFORM_DARWIN
banshee-1: banshee-1.darwin
else
banshee-1: banshee-1.linux
endif
	cp $< $@

install-exec-hook:
	$(LN_S) -f banshee-1 $(DESTDIR)$(bindir)/muinshee

CLEANFILES = banshee-1
