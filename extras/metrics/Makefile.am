ASSEMBLY = Metrics
TARGET = exe
LINK = $(LINK_HYENA_DEPS) $(LINK_ICSHARP_ZIP_LIB)
SOURCES =  \
	Database.cs \
	Main.cs \
	Metric.cs \
	MetaMetrics.cs \
	User.cs \
	MultiUserSample.cs

RESOURCES =

copy:
	cp $(top_srcdir)/bin/Metrics.exe* .

include $(top_srcdir)/build/build.mk
