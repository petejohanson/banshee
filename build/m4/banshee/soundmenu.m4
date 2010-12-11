AC_DEFUN([BANSHEE_CHECK_SOUNDMENU],
[
	LIBINDICATESHARP_REQUIRED=0.4.1

	AC_ARG_ENABLE([soundmenu],
		AS_HELP_STRING([--enable-soundmenu], [Enable sound menu support]),
		enable_soundmenu=$enableval, enable_soundmenu=no
	)

	if test "x$enable_soundmenu" = "xyes"; then
		has_indicatesharp=no
		PKG_CHECK_MODULES(INDICATESHARP,
			indicate-sharp-0.1 >= $LIBINDICATESHARP_REQUIRED,
			has_indicatesharp=yes, has_indicatesharp=no)
		if test "x$has_indicatesharp" = "xno"; then
			AC_MSG_ERROR([indicate-sharp was not found or is not up to date. Please install indicate-sharp of at least version $LIBINDICATESHARP_REQUIRED, or disable sound menu support by passing --disable-soundmenu])
		fi
	fi

	AM_CONDITIONAL(ENABLE_SOUNDMENU, test "x$enable_soundmenu" = "xyes")
])

