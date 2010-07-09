AC_DEFUN([BANSHEE_CHECK_LIBWEBKIT],
[
	have_libwebkit=no
	PKG_CHECK_MODULES(LIBWEBKIT, webkit-1.0 >= 1.2.0 libsoup-2.4 >= 2.26,
		have_libwebkit=yes, have_libwebkit=no)
	AC_SUBST(LIBWEBKIT_LIBS)
	AC_SUBST(LIBWEBKIT_CFLAGS)
	AM_CONDITIONAL(HAVE_LIBWEBKIT, [test x$have_libwebkit = xyes])

	have_libsoup_gnome=no
	PKG_CHECK_MODULES(LIBSOUP_GNOME, libsoup-gnome-2.4 >= 2.26,
		have_libsoup_gnome=$have_libwebkit, have_libsoup_gnome=no)
	AC_SUBST(LIBSOUP_GNOME_LIBS)
	AC_SUBST(LIBSOUP_GNOME_CFLAGS)
	AM_CONDITIONAL(HAVE_LIBSOUP_GNOME, [test x$have_libsoup_gnome = xyes])
	if test x$have_libsoup_gnome = xyes; then
		AC_DEFINE(HAVE_LIBSOUP_GNOME, 1, [libsoup-gnome-2.4 detected])
	fi
])

