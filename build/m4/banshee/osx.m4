AC_DEFUN([BANSHEE_CHECK_OSX],
[
	IGE_MAC_INTEGRATION_REQUIRED=0.8.6

	AC_ARG_ENABLE(osx, AC_HELP_STRING([--enable-osx], [Enable OSX support]), enable_osx=$enableval, enable_osx="no")

	if test "x$enable_osx" = "xyes"; then
		dnl FIXME: detect osx
		have_osx="yes"

		PKG_CHECK_MODULES(IGE_MAC_INTEGRATION, 
			ige-mac-integration >= $IGE_MAC_INTEGRATION_REQUIRED,
			have_ige_mac_integration=yes, have_ige_mac_integration=no)
	
		if test "x$have_ige_mac_integration" = "xno"; then
			AC_MSG_ERROR([ige-mac-integration was not found or is not up to date. Please install ige-mac-integration of at least version $IGE_MAC_INTEGRATION_REQUIRED])
		fi
		AC_SUBST(IGE_MAC_INTEGRATION_LIBS)
	fi

	AM_CONDITIONAL(ENABLE_OSX, test "x$have_osx" = "xyes")
])
