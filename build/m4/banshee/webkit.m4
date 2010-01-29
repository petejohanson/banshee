AC_DEFUN([BANSHEE_CHECK_WEBKIT],
[
	AC_ARG_ENABLE(webkit, AC_HELP_STRING([--disable-webkit], [Disable Wikipedia extension that requires webkit-sharp]), , enable_webkit="yes")

	if test "x$enable_webkit" = "xyes"; then
		PKG_CHECK_MODULES(WEBKIT, webkit-sharp-1.0 >= 0.2)
		AC_SUBST(WEBKIT_LIBS)
		AM_CONDITIONAL(HAVE_WEBKIT, true)
	else
		AM_CONDITIONAL(HAVE_WEBKIT, false)
	fi
])

