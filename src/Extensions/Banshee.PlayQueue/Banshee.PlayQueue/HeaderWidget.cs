//
// HeaderWidget.cs
//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
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

using Gtk;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Linq;

using Hyena;
using Banshee.PlaybackController;
using Banshee.Sources;
using Banshee.Collection.Database;
using Banshee.Widgets;

namespace Banshee.PlayQueue
{
    public class HeaderWidget : Alignment
    {
        public event EventHandler<EventArgs<RandomBy>> ModeChanged;
        public event EventHandler<EventArgs<DatabaseSource>> SourceChanged;

        private readonly List<Widget> sensitive_widgets = new List<Widget> ();

        private DictionaryComboBox<RandomBy> mode_combo;

        public string ShuffleModeId { get { return mode_combo.ActiveValue.Id; } }

        public HeaderWidget (Shuffler shuffler, string shuffle_mode_id, string source_name) : base (0, 0, 0, 0)
        {
            ThreadAssist.AssertInMainThread ();

            var box = new HBox ();
            box.Spacing = 6;

            var fill_label = new Label (Catalog.GetString ("_Fill"));
            mode_combo = new DictionaryComboBox<RandomBy> ();
            foreach (var random_by in shuffler.RandomModes.OrderBy (r => r.Adverb)) {
                mode_combo.Add (random_by.Adverb, random_by);
                if (random_by.Id == "off") {
                    mode_combo.Default = random_by;
                }
            }

            fill_label.MnemonicWidget = mode_combo;
            mode_combo.Changed += OnModeComboChanged;

            var from_label = new Label (Catalog.GetString ("f_rom"));
            var source_combo_box = new QueueableSourceComboBox (source_name);
            from_label.MnemonicWidget = source_combo_box;

            sensitive_widgets.Add (source_combo_box);
            sensitive_widgets.Add (from_label);

            source_combo_box.Changed += delegate {
                var handler = SourceChanged;
                if (handler != null) {
                    handler (this, new EventArgs<DatabaseSource> (source_combo_box.Source));
                }
            };

            box.PackStart (fill_label, false, false, 0);
            box.PackStart (mode_combo, false, false, 0);
            box.PackStart (from_label, false, false, 0);
            box.PackStart (source_combo_box, false, false, 0);
            this.SetPadding (0, 0, 6, 6);
            this.Add (box);

            // Select the saved population mode.
            var default_randomby = shuffler.RandomModes.FirstOrDefault (r => r.Id == shuffle_mode_id);
            if (default_randomby != null) {
                mode_combo.ActiveValue = default_randomby;
            } else if (mode_combo.Default != null) {
                mode_combo.ActiveValue = mode_combo.Default;
            }

            shuffler.RandomModeAdded   += (r) => mode_combo.Add (r.Adverb, r);
            shuffler.RandomModeRemoved += (r) => mode_combo.Remove (r);
        }

        public void SetManual () {
            ThreadAssist.AssertInMainThread ();
            mode_combo.ActiveValue = mode_combo.Default;
        }

        private void OnModeComboChanged (object o, EventArgs args)
        {
            var random_by = mode_combo.ActiveValue;
            foreach (var widget in sensitive_widgets) {
                widget.Sensitive = random_by.Id != "off";
            }

            var handler = ModeChanged;
            if (handler != null) {
                handler (this, new EventArgs<RandomBy> (random_by));
            }
        }
    }
}
