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
	Banshee.WebSource/WebView.cs \
	Ossifer.JavaScriptCore/JSClass.cs \
	Ossifer.JavaScriptCore/JSClassAttribute.cs \
	Ossifer.JavaScriptCore/JSClassDefinition.cs \
	Ossifer.JavaScriptCore/JSContext.cs \
	Ossifer.JavaScriptCore/JSException.cs \
	Ossifer.JavaScriptCore/JSFunction.cs \
	Ossifer.JavaScriptCore/JSObject.cs \
	Ossifer.JavaScriptCore/JSPropertyAttribute.cs \
	Ossifer.JavaScriptCore/JSPropertyNameAccumulator.cs \
	Ossifer.JavaScriptCore/JSPropertyNameArray.cs \
	Ossifer.JavaScriptCore/JSString.cs \
	Ossifer.JavaScriptCore/JSType.cs \
	Ossifer.JavaScriptCore/JSValue.cs \
	Ossifer.JavaScriptCore/ManagedPropertyBagClass.cs \
	Ossifer.JavaScriptCore/Tests/JSFunctionTests.cs \
	Ossifer.JavaScriptCore/Tests/JSObjectTests.cs \
	Ossifer.JavaScriptCore/Tests/JSStringTests.cs \
	Ossifer.JavaScriptCore/Tests/JSValueTests.cs

RESOURCES =  \
	Resources/loading.html

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
