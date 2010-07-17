AC_DEFUN([BANSHEE_CHECK_HAL],
[
	AC_ARG_ENABLE(hal, AC_HELP_STRING([--disable-hal], [Disable Hal hardware backend]), ,enable_hal="yes")

	AM_CONDITIONAL(ENABLE_HAL, test "x$enable_hal" = "xyes")
])