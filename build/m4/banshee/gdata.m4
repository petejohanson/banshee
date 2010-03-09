AC_DEFUN([BANSHEE_CHECK_GDATA],
[
	GDATASHARP_REQUIRED_VERSION=1.4
	AC_SUBST(GDATASHARP_REQUIRED_VERSION)

	AC_ARG_ENABLE(gdata, AC_HELP_STRING([--disable-gdata], [Disable Youtube extension]), , enable_gdata="yes")

	if test "x$enable_gdata" = "xyes"; then
		PKG_CHECK_MODULES(GDATASHARP,
			gdata-sharp-core >= $GDATASHARP_REQUIRED_VERSION
			gdata-sharp-youtube >= $GDATASHARP_REQUIRED_VERSION)
		AC_SUBST(GDATASHARP_LIBS)
		AM_CONDITIONAL(HAVE_GDATA, true)
	else
		AM_CONDITIONAL(HAVE_GDATA, false)
	fi
])
