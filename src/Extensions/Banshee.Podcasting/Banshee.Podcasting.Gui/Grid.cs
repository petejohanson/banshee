//
// Grid.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Data;
using Hyena.Gui;
using Hyena.Gui.Canvas;
using Hyena.Data.Gui;

using Banshee.Collection;
using Banshee.Collection.Gui;

using Banshee.Podcasting;
using Banshee.Podcasting.Data;

namespace Banshee.Podcasting.Gui
{
    public class Grid : BaseTrackListView
    {
        public Grid ()
        {
            var layout = new DataViewLayoutGrid () {
                Fill = true,
                ChildAllocator = () => {
                    DataViewChildImage img = new DataViewChildImage ();
                    return new StackPanel () {
                        Margin = new Thickness (5),
                        Width = 300,
                        Height = 125,
                        Spacing = 5,
                        Children = {
                            new StackPanel () {
                                Orientation = Orientation.Vertical,
                                Width = 90,
                                Spacing = 5,
                                Children = {
                                    img,
                                    new ColumnCellPodcastStatusIndicator (null)
                                }
                            },
                            new TextBlock () {
                                UseMarkup = true,
                                TextWrap = TextWrap.WordChar,
                                TextGenerator = o => {
                                    var track = o as TrackInfo;
                                    if (track != null) {
                                        var episode = PodcastTrackInfo.From (track);
                                        if (episode != null) {
                                            return "<b>{0}</b>\n<small>{1}\n{2}</small>".FormatEscaped (
                                                track.DisplayTrackTitle, episode.PublishedDate.ToShortDateString (), episode.Description
                                            );
                                        }
                                    }
                                    return "";
                                }
                            }
                        },
                        // Render the prelight just on the cover art, but triggered by being anywhere over the album
                        PrelightRenderer = (cr, theme, size, o) => {
                            Prelight.Gradient (cr, theme, new Rect (img.ContentAllocation.X, img.ContentAllocation.Y, img.ImageSize, img.ImageSize), o);
                        }
                    };
                },
                View = this
            };

            ViewLayout = layout;
        }

        public void SetLibrary (PodcastSource library)
        {
            SetModel (library.TrackModel);
        }

        public override bool SelectOnRowFound {
            get { return true; }
        }
    }
}
