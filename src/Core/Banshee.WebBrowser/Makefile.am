SUBDIRS = libossifer

ASSEMBLY = Banshee.WebBrowser
TARGET = library
LINK = $(REF_BANSHEE_WEBBROWSER)
INSTALL_DIR = $(DEFAULT_INSTALL_DIR)

SOURCES =  \
	Banshee.WebBrowser/OssiferDownloadStatus.cs \
	Banshee.WebBrowser/OssiferNavigationResponse.cs \
	Banshee.WebBrowser/OssiferWebView.cs

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES)
endif
