//
// DefaultPreferenceWidgets.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Widgets;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Library;
using Banshee.Preferences;
using Banshee.Collection;
using Banshee.ServiceStack;

using Banshee.Widgets;
using Banshee.Gui.Widgets;

namespace Banshee.Preferences.Gui
{
    public static class DefaultPreferenceWidgets
    {
        public static void Load (PreferenceService service)
        {
            foreach (var library in ServiceManager.SourceManager.FindSources<LibrarySource> ()) {
                new LibraryLocationButton (library);

                if (library.PathPattern != null) {
                    var library_page = library.PreferencesPage;
                    var pattern = library.PathPattern;

                    var folder_pattern = library_page["file-system"][pattern.FolderSchema.Key];
                    folder_pattern.DisplayWidget = new PatternComboBox (library, folder_pattern, pattern.SuggestedFolders);

                    var file_pattern = library_page["file-system"][pattern.FileSchema.Key];
                    file_pattern.DisplayWidget = new PatternComboBox (library, file_pattern, pattern.SuggestedFiles);

                    var pattern_display = library_page["file-system"].FindOrAdd (new VoidPreference ("file_folder_pattern"));
                    pattern_display.DisplayWidget = new PatternDisplay (library, folder_pattern.DisplayWidget, file_pattern.DisplayWidget);
                }
            }

            service["extensions"].DisplayWidget = new Banshee.Addins.Gui.AddinView ();
        }

        private class LibraryLocationButton : HBox
        {
            private LibrarySource source;
            private SchemaPreference<string> preference;
            private FileChooserButton chooser;
            private Button reset;
            private string created_directory;

            public LibraryLocationButton (LibrarySource source)
            {
                this.source = source;
                preference = source.PreferencesPage["library-location"]["library-location"] as SchemaPreference<string>;
                preference.ShowLabel = false;
                preference.DisplayWidget = this;

                string dir = preference.Value ?? source.DefaultBaseDirectory;

                Spacing = 5;

                // FileChooserButton wigs out if the directory does not exist,
                // so create it if it doesn't and store the fact that we did
                // in case it ends up not being used, we can remove it
                try {
                    if (!Banshee.IO.Directory.Exists (dir)) {
                        Banshee.IO.Directory.Create (dir);
                        created_directory = dir;
                        Log.DebugFormat ("Created library directory: {0}", created_directory);
                    }
                } catch {
                }

                chooser = new FileChooserButton (Catalog.GetString ("Select library location"),
                    FileChooserAction.SelectFolder);
                // Only set the LocalOnly property if false; setting it when true
                // causes the "Other..." entry to be hidden in older Gtk+
                if (!Banshee.IO.Provider.LocalOnly) {
                    chooser.LocalOnly = Banshee.IO.Provider.LocalOnly;
                }
                chooser.SetCurrentFolder (dir);
                chooser.SelectionChanged += OnChooserChanged;

                HBox box = new HBox ();
                box.Spacing = 2;
                box.PackStart (new Image (Stock.Undo, IconSize.Button), false, false, 0);
                box.PackStart (new Label (Catalog.GetString ("Reset")), false, false, 0);
                reset = new Button () {
                    Sensitive = dir != source.DefaultBaseDirectory,
                    TooltipText = String.Format (Catalog.GetString ("Reset location to default ({0})"), source.DefaultBaseDirectory)
                };
                reset.Clicked += OnReset;
                reset.Add (box);

                //Button open = new Button ();
                //open.PackStart (new Image (Stock.Open, IconSize.Button), false, false, 0);
                //open.Clicked += OnOpen;

                PackStart (chooser, true, true, 0);
                PackStart (reset, false, false, 0);
                //PackStart (open, false, false, 0);

                chooser.Show ();
                reset.ShowAll ();
            }

            private void OnReset (object o, EventArgs args)
            {
                chooser.SetFilename (source.DefaultBaseDirectory);
            }

            //private void OnOpen (object o, EventArgs args)
            //{
                //open chooser.Filename
            //}

            private void OnChooserChanged (object o, EventArgs args)
            {
                preference.Value = chooser.Filename;
                reset.Sensitive = chooser.Filename != source.DefaultBaseDirectory;
            }

            protected override void OnUnrealized ()
            {
                // If the directory we had to create to appease FileSystemChooser exists
                // and ended up not being the one selected by the user we clean it up
                if (created_directory != null && chooser.Filename != created_directory) {
                    try {
                        Banshee.IO.Directory.Delete (created_directory);
                        if (!Banshee.IO.Directory.Exists (created_directory)) {
                            Log.DebugFormat ("Deleted unused and empty previous library directory: {0}", created_directory);
                            created_directory = null;
                        }
                    } catch {
                    }
                }

                base.OnUnrealized ();
            }
        }

        private class PatternComboBox : DictionaryComboBox<string>
        {
            private Preference<string> preference;

            public PatternComboBox (PrimarySource source, PreferenceBase pref, string [] patterns)
            {
                preference = (Preference<string>)pref;

                bool already_added = false;
                string conf_pattern = preference.Value;

                foreach (string pattern in patterns) {
                    if (!already_added && pattern.Equals (conf_pattern)) {
                        already_added = true;
                    }

                    Add (source.PathPattern.CreatePatternDescription (pattern), pattern);
                }

                if (!already_added) {
                    Add (source.PathPattern.CreatePatternDescription (conf_pattern), conf_pattern);
                }

                ActiveValue = conf_pattern;
            }

            protected override void OnChanged ()
            {
                preference.Value = ActiveValue;
                base.OnChanged ();
            }
        }

        private class PatternDisplay : WrapLabel
        {
            private PatternComboBox folder;
            private PatternComboBox file;
            private PrimarySource source;

            public PatternDisplay (PrimarySource source, object a, object b)
            {
                this.source = source;
                folder= (PatternComboBox)a;
                file = (PatternComboBox)b;

                folder.Changed += OnChanged;
                file.Changed += OnChanged;

                OnChanged (null, null);
            }

            private void OnChanged (object o, EventArgs args)
            {
                var pattern = source.PathPattern.CreateFolderFilePattern (folder.ActiveValue, file.ActiveValue);

                var sb = new System.Text.StringBuilder ();
                foreach (var track in source.PathPattern.SampleTracks) {
                    string display = source.PathPattern.CreateFromTrackInfo (pattern, track);
                    if (!String.IsNullOrEmpty (display)) {
                        sb.AppendFormat ("<small>{0}.ogg</small>", GLib.Markup.EscapeText (display));
                    }
                }

                Markup = sb.ToString ();
            }
        }
    }
}
