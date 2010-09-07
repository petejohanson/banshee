AC_DEFUN([BANSHEE_CHECK_DAP_APPLEDEVICE],
[
	LIBGPODSHARP_REQUIRED=0.1

	AC_ARG_ENABLE(appledevice, AC_HELP_STRING([--disable-appledevice], [Disable Apple device (iPhone, iPod, iPad) DAP support]), , enable_appledevice="yes")

	if test "x$enable_appledevice" = "xyes"; then
		has_libgpod=no
		PKG_CHECK_MODULES(LIBGPODSHARP,
			libgpod-sharp >= $LIBGPODSHARP_REQUIRED,
			has_libgpod=yes, has_libgpod=no)
		if test "x$has_libgpod" = "xno"; then
			AC_MSG_ERROR([libgpod-sharp was not found or is not up to date. Please install libgpod-sharp of at least version $LIBGPODSHARP_REQUIRED, or disable Apple device support by passing --disable-appledevice])
		fi
	fi

	if test "x$enable_appledevice" = "xyes"; then
		asm="`$PKG_CONFIG --variable=Libraries libgpod-sharp`"
		LIBGPODSHARP_ASSEMBLIES="$LIBGPODSHARP_ASSEMBLIES $asm"
		[[ -r "$asm.config" ]] && LIBGPODSHARP_ASSEMBLIES="$LIBGPODSHARP_ASSEMBLIES $asm.config"
		[[ -r "$asm.mdb" ]] && LIBGPODSHARP_ASSEMBLIES="$LIBGPODSHARP_ASSEMBLIES $asm.mdb"
		AC_SUBST(LIBGPODSHARP_ASSEMBLIES)
	fi
	
	AM_CONDITIONAL(ENABLE_APPLEDEVICE, test "x$enable_appledevice" = "xyes")
])

