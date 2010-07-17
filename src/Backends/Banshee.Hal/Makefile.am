ASSEMBLY = Banshee.Hal
TARGET = library
LINK = $(REF_BACKEND_HAL)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.HalBackend/BlockDevice.cs \
	Banshee.HalBackend/CdromDevice.cs \
	Banshee.HalBackend/Device.cs \
	Banshee.HalBackend/DeviceMediaCapabilities.cs \
	Banshee.HalBackend/DiscVolume.cs \
	Banshee.HalBackend/DiskDevice.cs \
	Banshee.HalBackend/DkDisk.cs \
	Banshee.HalBackend/HardwareManager.cs \
	Banshee.HalBackend/UsbDevice.cs \
	Banshee.HalBackend/Volume.cs \
	Hal/Device.cs \
	Hal/Manager.cs \
	Hal/Volume.cs

RESOURCES = Banshee.Hal.addin.xml

if ENABLE_HAL
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif


