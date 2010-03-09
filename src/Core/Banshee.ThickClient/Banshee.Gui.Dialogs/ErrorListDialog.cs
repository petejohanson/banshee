//
// ErrorListDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2006-2010 Novell, Inc.
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
using System.Collections;
using Mono.Unix;
using Gtk;

namespace Banshee.Gui.Dialogs
{
    public class ErrorListDialog : BansheeDialog
    {
        private Label header_label;
        private Hyena.Widgets.WrapLabel message_label;
        private TreeView list_view;
        private Image icon_image;
        private Expander details_expander;

        private ListStore simple_model;

        public ErrorListDialog () : base (Catalog.GetString ("Error"))
        {
            var table = new Table (3, 2, false) {
                RowSpacing = 12,
                ColumnSpacing = 16
            };

            table.Attach (icon_image = new Image () {
                    IconName = "dialog-error",
                    IconSize = (int)IconSize.Dialog,
                    Yalign = 0.0f
                }, 0, 1, 0, 3, AttachOptions.Shrink, AttachOptions.Fill | AttachOptions.Expand, 0, 0);

            table.Attach (header_label = new Label () { Xalign = 0.0f }, 1, 2, 0, 1,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink, 0, 0);

            table.Attach (message_label = new Hyena.Widgets.WrapLabel (), 1, 2, 1, 2,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink, 0, 0);

            var scrolled_window = new ScrolledWindow () {
                HscrollbarPolicy = PolicyType.Automatic,
                VscrollbarPolicy = PolicyType.Automatic,
                ShadowType = ShadowType.In
            };

            list_view = new TreeView () {
                HeightRequest = 120,
                WidthRequest = 200
            };
            scrolled_window.Add (list_view);

            table.Attach (details_expander = new Expander (Catalog.GetString ("Details")),
                1, 2, 2, 3,
                AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Fill | AttachOptions.Expand,
                0, 0);
            details_expander.Add (scrolled_window);

            VBox.PackStart (table, true, true, 0);
            VBox.Spacing = 12;
            VBox.ShowAll ();

            details_expander.Activated += OnConfigureGeometry;
            Realized += OnConfigureGeometry;
        }

        private void OnConfigureGeometry (object o, EventArgs args)
        {
            var limits = new Gdk.Geometry () {
                MinWidth = SizeRequest ().Width,
                MaxWidth = Gdk.Screen.Default.Width
            };

            if (details_expander.Expanded) {
                limits.MinHeight = SizeRequest ().Height + list_view.SizeRequest ().Height;
                limits.MaxHeight = Gdk.Screen.Default.Height;
            } else {
                limits.MinHeight = -1;
                limits.MaxHeight = -1;
            }

            SetGeometryHints (this, limits,
                Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }

        public string Header {
            set {
                Title = value;
                header_label.Markup = String.Format("<b><big>{0}</big></b>",
                    GLib.Markup.EscapeText(value));
            }
        }

        public void AppendString(string item)
        {
            if(list_view.Model == null) {
                CreateSimpleModel();
            }

            if(list_view.Model != simple_model) {
                throw new ApplicationException("A custom model is in use");
            }

            simple_model.AppendValues(item);
        }

        private void CreateSimpleModel()
        {
            simple_model = new ListStore(typeof(string));
            list_view.Model = simple_model;
            list_view.AppendColumn("Error", new CellRendererText(), "text", 0);
            list_view.HeadersVisible = false;
        }

        public string Message {
            set { message_label.Text = value; }
        }

        public string DialogIconName {
            set { icon_image.SetFromIconName(value, IconSize.Dialog); }
        }

        public string DialogIconNameStock {
            set { icon_image.SetFromStock(value, IconSize.Dialog); }
        }

        public TreeView ListView {
            get { return list_view; }
        }
    }
}
