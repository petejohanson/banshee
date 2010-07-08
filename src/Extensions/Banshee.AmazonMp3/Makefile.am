ASSEMBLY = Banshee.AmazonMp3
TARGET = exe
LINK = $(REF_EXTENSION_AMAZONMP3)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.AmazonMp3/AmazonMp3DownloaderService.cs \
	Banshee.AmazonMp3/AmzMp3Downloader.cs \
	Banshee.AmazonMp3/AmzXspfPlaylist.cs \
	Banshee.AmazonMp3/ConsoleTool.cs \
	Banshee.AmazonMp3/ImportSource.cs \
	Banshee.AmazonMp3/UserJobDownloadManager.cs 

RESOURCES = Banshee.AmazonMp3.addin.xml

bamzdir = $(bindir)
bamz_SCRIPTS = bamz

mimedir = $(datadir)/mime/packages
mime_DATA = amazonmp3.xml

bamz: bamz.in
	@echo '  GEN   $@'; sed 's,@bansheedir@,$(pkglibdir),g' < $< > $@

define EXTRA_INSTALL_DATA_HOOK
	test "$(top_srcdir)" = "$(top_builddir)" && update-mime-database -V $(DESTDIR)$(datadir)/mime || :
endef

include $(top_srcdir)/build/build.mk

CLEANFILES += bamz
EXTRA_DIST += bamz.in amazonmp3.xml
