AC_DEFUN([BANSHEE_CHECK_UBUNTUONE],
[
	AC_ARG_ENABLE([ubuntuone],
		AS_HELP_STRING([--enable-ubuntuone], [Enable Ubuntu One Music Store support]),
		enable_ubuntuone=$enableval, enable_ubuntuone=no
	)

	if test "x$enable_ubuntuone" = "xyes"; then
		has_ubuntuonesharp=no
		PKG_CHECK_MODULES(UBUNTUONESHARP,
			[ubuntuone-sharp-1.0],
			has_ubuntuonesharp=yes, has_ubuntuonesharp=no)
		if test "x$has_ubuntuonesharp" = "xno"; then
			AC_MSG_ERROR([ubuntuone-sharp was not found. Please install ubuntuone-sharp, or disable Ubuntu One support by passing --disable-ubuntuone])
		fi
	fi

	AM_CONDITIONAL(ENABLE_UBUNTUONE, test "x$enable_ubuntuone" = "xyes")
])

