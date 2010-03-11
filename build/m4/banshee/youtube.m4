AC_DEFUN([BANSHEE_CHECK_YOUTUBE],
[
	GDATASHARP_REQUIRED_VERSION=1.4
	AC_SUBST(GDATASHARP_REQUIRED_VERSION)

	AC_ARG_ENABLE(youtube, AC_HELP_STRING([--disable-youtube], [Disable Youtube extension]), , enable_youtube="yes")

	if test "x$enable_youtube" = "xyes"; then
		PKG_CHECK_MODULES(GDATASHARP,
			gdata-sharp-core >= $GDATASHARP_REQUIRED_VERSION
			gdata-sharp-youtube >= $GDATASHARP_REQUIRED_VERSION)
		AC_SUBST(GDATASHARP_LIBS)
		AM_CONDITIONAL(HAVE_GDATA, true)
	else
		AM_CONDITIONAL(HAVE_GDATA, false)
	fi
])
