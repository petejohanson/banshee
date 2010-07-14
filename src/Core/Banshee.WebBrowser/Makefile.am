SUBDIRS = libossifer

ASSEMBLY = Banshee.WebBrowser
TARGET = library
LINK = $(REF_BANSHEE_WEBBROWSER)
INSTALL_DIR = $(DEFAULT_INSTALL_DIR)

SOURCES =  \
	Banshee.WebBrowser/NavigationControl.cs \
	Banshee.WebBrowser/OssiferCookie.cs \
	Banshee.WebBrowser/OssiferDownloadStatus.cs \
	Banshee.WebBrowser/OssiferLoadStatus.cs \
	Banshee.WebBrowser/OssiferNavigationResponse.cs \
	Banshee.WebBrowser/OssiferSession.cs \
	Banshee.WebBrowser/OssiferWebView.cs \
	Banshee.WebSource/WebBrowserShell.cs \
	Banshee.WebSource/WebSource.cs \
	Banshee.WebSource/WebView.cs

RESOURCES =  \
	Resources/loading.html

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
