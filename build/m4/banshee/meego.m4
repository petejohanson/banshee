AC_DEFUN([BANSHEE_CHECK_MEEGO],
[
	AC_ARG_ENABLE(meego, AC_HELP_STRING([--enable-meego], [Enable MeeGo integration]), , enable_meego="no")

	if test "x$enable_meego" = "xyes"; then
		AM_CONDITIONAL(HAVE_MEEGO, true)
	else
		AM_CONDITIONAL(HAVE_MEEGO, false)
	fi
])

