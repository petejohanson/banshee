if HAVE_GNOME_DOC_UTILS
include $(top_srcdir)/gnome-doc-utils.make

DOC_ID = banshee
DOC_INCLUDES = legal.xml
DOC_FIGURES = 

DOC_PAGES = \
	add-podcast.page \
	add-radio.page \
	advanced.page \
	import.page \
	index.page \
	manage-coverart.page \
	manage-playlists.page \
	manage-tags.page \
	play-queue.page \
	play.page \
	search.page \
	sort.page \
	sync.page

DOC_LINGUAS = de
dist-hook: doc-dist-hook

all:
	mkdir -p $(top_builddir)/bin/share/gnome/help/$(DOC_ID) && cp -R C/ $(DOC_LINGUAS) $(top_builddir)/bin/share/gnome/help/$(DOC_ID)/

endif
