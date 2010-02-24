//
// OsxService.cs
//
// Author:
//   Eoin Hennessy <eoin@randomrules.org>
//
// Copyright (C) 2008 Eoin Hennessy
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
using Gtk;
using Mono.Unix;

using Banshee.ServiceStack;
using Banshee.Gui;

using OsxIntegration.Ige;
using OsxIntegration.Framework;

namespace Banshee.OsxBackend
{
    public class OsxService : IExtensionService, IDisposable
    {
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private uint ui_manager_id;
        private bool disposed;

        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();

            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            }

            ServiceStartup ();
        }

        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null) {
                return false;
            }

            Initialize ();
            ServiceManager.ServiceStarted -= OnServiceStarted;

            return true;
        }

        private void Initialize ()
        {
            elements_service.PrimaryWindow.WindowStateEvent += OnWindowStateEvent;

            // add close action
            interface_action_service.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("CloseAction", Stock.Close,
                    Catalog.GetString ("_Close"), "<Control>W",
                    Catalog.GetString ("Close"), CloseWindow)
            });

            // merge close menu item
            ui_manager_id = interface_action_service.UIManager.AddUiFromString (@"
              <ui>
                <menubar name=""MainMenu"">
                  <menu name=""MediaMenu"" action=""MediaMenuAction"">
                    <placeholder name=""ClosePlaceholder"">
                    <menuitem name=""Close"" action=""CloseAction""/>
                    </placeholder>
                  </menu>
                </menubar>
              </ui>
            ");

            RegisterCloseHandler ();
            ConfigureOsxMainMenu ();

            IgeMacMenu.GlobalKeyHandlerEnabled = false;

            ApplicationEvents.Quit += (o, e) => {
                Banshee.ServiceStack.Application.Shutdown ();
                e.Handled = true;
            };

            ApplicationEvents.Reopen += (o, e) => {
                SetWindowVisibility (true);
                e.Handled = true;
            };
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            elements_service.PrimaryWindowClose = null;

            interface_action_service.GlobalActions.Remove ("CloseAction");
            interface_action_service.UIManager.RemoveUi (ui_manager_id);

            disposed = true;
        }

        private void ConfigureOsxMainMenu ()
        {
            IgeMacMenu.MenuBar = (MenuShell)interface_action_service.UIManager.GetWidget ("/MainMenu");

            var ui = interface_action_service.UIManager;

            IgeMacMenu.QuitMenuItem = ui.GetWidget ("/MainMenu/MediaMenu/Quit") as MenuItem;

            var group = IgeMacMenu.AddAppMenuGroup ();
            group.AddMenuItem (ui.GetWidget ("/MainMenu/HelpMenu/About") as MenuItem, null);
            group.AddMenuItem (ui.GetWidget ("/MainMenu/EditMenu/Preferences") as MenuItem, null);
        }

        private void RegisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose == null) {
                elements_service.PrimaryWindowClose = () => {
                    CloseWindow (null, null);
                    return true;
                };
            }
        }

        private void CloseWindow (object o, EventArgs args)
        {
            SetWindowVisibility (false);
        }

        private void SetCloseMenuItemSensitivity (bool sensitivity)
        {
            ((MenuItem)interface_action_service.UIManager.GetWidget (
                "/MainMenu/MediaMenu/ClosePlaceholder/Close")).Sensitive = sensitivity;
        }

        private void SetWindowVisibility (bool visible)
        {
            SetCloseMenuItemSensitivity (visible);
            if (elements_service.PrimaryWindow.Visible != visible) {
                elements_service.PrimaryWindow.ToggleVisibility ();
            }
        }

        private void OnWindowStateEvent (object obj, WindowStateEventArgs args)
        {
            switch (args.Event.NewWindowState) {
                case Gdk.WindowState.Iconified:
                    SetCloseMenuItemSensitivity (false);
                    break;
                case (Gdk.WindowState)0:
                    SetCloseMenuItemSensitivity (true);
                    break;
            }
        }

        string IService.ServiceName {
            get { return "OsxService"; }
        }
    }
}
