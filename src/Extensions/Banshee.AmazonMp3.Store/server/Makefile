CFLAGS_AND_LIBS := $(shell pkg-config --cflags --libs glib-2.0) -lGeoIP

all: redirect.do

check-deps:
	@test -f /usr/include/GeoIP.h || \
			{ echo "You must provide /usr/include/GeoIP.h"; exit 1; }
	@pkg-config --exists glib-2.0 || \
			{ echo "You must provide pkgconfig(glib-2.0)"; exit 1; }

redirect.do: check-deps redirect.c
	$(CC) -Wall -o $@ $(CFLAGS_AND_LIBS) redirect.c

clean:
	rm -f redirect.do

push:
	scp * banshee@banshee.fm:web/integrated-services.banshee.fm/amz/
	ssh banshee@banshee.fm 'cd web/integrated-services.banshee.fm/amz/; make'
