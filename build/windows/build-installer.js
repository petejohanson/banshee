// This script was started by copying MonoDevelop's, available at 
// https://github.com/mono/monodevelop/tree/master/setup/WixSetup

// HEAT manual: http://wix.sourceforge.net/manual-wix3/heat.htm

var version = "1.9.3";
var bin = '..\\..\\bin';

var sh = new ActiveXObject("WScript.Shell");
var fs = new ActiveXObject("Scripting.FileSystemObject");
var env = sh.Environment("Process");

var heat = "\"" + env("WIX") + "bin\\heat.exe\"";

// Build Banshee
//build ("..\\..\\Banshee.sln");

// Delete some files that might be created by running uninstalled
if (fs.FileExists ("registry.bin")) fs.DeleteFile ("registry.bin");
if (fs.FolderExists ("addin-db-001")) fs.DeleteFolder ("addin-db-001");

// We can't just heat the entire dir b/c it would include the .git/ directory
heatDir ("bin");
heatDir ("etc");
heatDir ("lib");
heatDir ("share");

// Create the installer, will be outputted to Banshee-1.9.3.msi in build/windows/
build ("Installer.wixproj")

WScript.Echo ("Setup successfully generated");

function heatDir (dir)
{
  // Generate the list of binary files (managed and native .dlls and .pdb and .config files)
  run (heat + ' dir ..\\..\\bin\\' + dir + ' -cg ' + dir + ' -scom -sreg -ag -sfrag -indent 2 -var var.' + dir + 'Dir -dr INSTALLLOCATION -out obj\\generated_'+dir+'.wxi');

  // Heat has no option to output Include (wxi) files instead of Wix (wxs) ones, so do a little regex
  regexreplace ('obj\\generated_'+dir+'.wxi', /Wix xmlns/, 'Include xmlns');
  regexreplace ('obj\\generated_'+dir+'.wxi', /Wix>/, 'Include>');
}

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
