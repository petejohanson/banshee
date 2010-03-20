AC_DEFUN([BANSHEE_CHECK_MEEGO],
[
	AC_ARG_ENABLE(meego, AC_HELP_STRING([--enable-meego], [Enable Moblin integration]), , enable_meego="no")

	if test "x$enable_meego" = "xyes"; then
		AM_CONDITIONAL(HAVE_MEEGO, true)
		MEEGO_PANELS_DIR=`$PKG_CONFIG --variable=moblin_panel_panels_dir moblin-panel`
		AC_SUBST(MEEGO_PANELS_DIR)
	else
		AM_CONDITIONAL(HAVE_MEEGO, false)
	fi
])

