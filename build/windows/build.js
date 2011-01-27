// This script was started by copying MonoDevelop's, available at 
// https://github.com/mono/monodevelop/tree/master/setup/WixSetup

var version = "1.9.3";

var sh = new ActiveXObject("WScript.Shell");
var fs = new ActiveXObject("Scripting.FileSystemObject");

// Build Banshee
//build ("..\\..\\Banshee.sln");

// Copy binaries into a new folder so we can generate a file list w/o .git/* being included
if (fs.FolderExists ("dlls")) fs.DeleteFolder ("dlls");
fs.CreateFolder ("dlls");
run ('xcopy /D ..\\..\\bin\\*.dll dlls\\');
run ('xcopy /D ..\\..\\bin\\*.config dlls\\');
run ('xcopy /D ..\\..\\bin\\*.pdb dlls\\');
run ('xcopy /D ..\\..\\bin\\*.exe dlls\\');
fs.CreateFolder ("dlls\\gst-plugins");
run ('xcopy /D ..\\..\\bin\\gst-plugins\\* dlls\\gst-plugins\\');

// We already mention Nereid.exe specifically in Product.wxs so we can make a shortcut based off it
if (fs.FileExists ("dlls\\Nereid.exe")) fs.DeleteFile ("dlls\\Nereid.exe");

// Generate the list of binary files (managed and native .dlls and .pdb and .config files)
run ('heat dir dlls -cg binaries -srd -scom -sreg -ag -sfrag -suid -indent 2 -var var.BinDir -dr INSTALLLOCATION -out binaries.wxi');

// Heat has no option to output Include (wxi) files instead of Wix (wxs) ones, so do a little regex
regexreplace ('binaries.wxi', /Wix xmlns/, 'Include xmlns');
regexreplace ('binaries.wxi', /Wix>/, 'Include>');

// Generate the list of files in share/ (icons, i18n, and GStreamer audio-profiles)
run ('heat dir ..\\..\\bin\\share -cg share -scom -ag -sfrag -suid -indent 2 -var var.ShareDir -dr INSTALLLOCATION -out share.wxi');
regexreplace ('share.wxi', /Wix xmlns/, 'Include xmlns');
regexreplace ('share.wxi', /Wix>/, 'Include>');

// Create the installer, will be outputted to bin/Debug/
build ("WixSetup.sln")

WScript.Echo ("Setup successfully generated");

function run (cmd)
{
	if (sh.run (cmd, 5, true) != 0) {
		WScript.Echo ("Failed to run cmd:\n" + cmd);
	  WScript.Quit (1);
  }
}

function build (file)
{
	if (sh.run ("C:\\Windows\\Microsoft.NET\\Framework\\v3.5\\msbuild.exe " + file, 5, true) != 0) {
		WScript.Echo ("Build failed");
	  WScript.Quit (1);
	}
}

function regexreplace (file, regex, replacement)
{
   var f = fs.OpenTextFile (file, 1);
   var content = f.ReadAll ();
   f.Close ();
   content = content.replace (regex, replacement);
   f = fs.CreateTextFile (file, true);
   f.Write (content);
   f.Close ();
}

function format (num, len)
{
	var res = num.toString ();
	while (res.length < len)
		res = "0" + res;
	return res;
}
