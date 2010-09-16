AC_DEFUN([BANSHEE_CHECK_YOUTUBE],
[
	GDATASHARP_REQUIRED_VERSION=1.4

	AC_ARG_ENABLE(youtube, AC_HELP_STRING([--disable-youtube], [Disable Youtube extension]), , enable_youtube="yes")

	if test "x$enable_youtube" = "xyes"; then
		PKG_CHECK_MODULES(GDATASHARP,
			gdata-sharp-youtube >= 1.5,
			[AM_CONDITIONAL(HAVE_GDATASHARP_1_5, true)],
			[PKG_CHECK_MODULES(GDATASHARP, gdata-sharp-youtube >= $GDATASHARP_REQUIRED_VERSION)
			 AM_CONDITIONAL(HAVE_GDATASHARP_1_5, false)]
		)
		AC_SUBST(GDATASHARP_LIBS)
		AM_CONDITIONAL(HAVE_GDATA, true)
	else
		AM_CONDITIONAL(HAVE_GDATASHARP_1_5, false)
		AM_CONDITIONAL(HAVE_GDATA, false)
	fi
])
