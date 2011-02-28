using System;
using System.Linq;
using System.Xml.XPath;
using System.Collections.Generic;

public static class AddinXmlStringExtract
{
    public static void Main (string [] args)
    {
        var queries = new [] {
            "/Addin/@name",
            "/Addin/@description",
            "/Addin/@category"
        };

        Console.WriteLine (@"// Generated - Do Not Edit!

internal static class AddinXmlStringCatalog
{
    private static void Strings ()
    {");

        var paths = new List<string> (args);
        paths.Sort ();

        var blacklist = new string [] { "GStreamer", "Gnome", "Osx", "Unix", "MeeGo", "Gio", "NowPlaying",
            "Hal", "src/Core", "Banshee.Dap/", "RemoteAudio", "Sample", "SqlDebugConsole", "Template", "Windows" };

        foreach (var path in paths) {
            if (blacklist.Any (path.Contains))
                continue;

            Console.WriteLine ("        // {0}", path);
            var xpath = new XPathDocument (path);
            var nav = xpath.CreateNavigator ();
            foreach (var query in queries) {
                var iter = nav.Select (query);
                while (iter.MoveNext ()) {
                    var value = iter.Current.Value.Trim ();
                    if (String.IsNullOrEmpty (value) ||
                        value[0] == '@' ||
                        (iter.Current.Name == "category" && value.StartsWith ("required:"))) {
                        continue;
                    }
                    Console.WriteLine (@"        Catalog.GetString (@""{0}"");",
                        value.Replace (@"""", @""""""));
                }
            }
            Console.WriteLine ();
        }

        Console.WriteLine ("    }\n}");
    }
}
