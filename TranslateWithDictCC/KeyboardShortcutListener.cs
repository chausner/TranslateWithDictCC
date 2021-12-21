using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;

namespace TranslateWithDictCC
{
    class KeyboardShortcutListener
    {
        bool controlPressed;
        bool menuPressed;
        bool shiftPressed;

        List<Tuple<VirtualKeyModifiers, VirtualKey, EventHandler>> shortcutHandlers = new List<Tuple<VirtualKeyModifiers, VirtualKey, EventHandler>>();

        public KeyboardShortcutListener()
        {
            // Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            // Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Control:
                    controlPressed = true;
                    return;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    menuPressed = true;
                    return;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    shiftPressed = true;
                    return;
            }

            VirtualKeyModifiers modifiers = 0;

            if (controlPressed)
                modifiers |= VirtualKeyModifiers.Control;
            if (menuPressed)
                modifiers |= VirtualKeyModifiers.Menu;
            if (shiftPressed)
                modifiers |= VirtualKeyModifiers.Shift;

            foreach (var shortcutHandler in shortcutHandlers)
                if (shortcutHandler.Item1 == modifiers &&
                    shortcutHandler.Item2 == args.VirtualKey)
                    shortcutHandler.Item3(this, EventArgs.Empty);
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Control:
                    controlPressed = false;
                    break;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    menuPressed = false;
                    break;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    shiftPressed = false;
                    break;
            }
        }

        public void RegisterShortcutHandler(VirtualKeyModifiers modifiers, VirtualKey key, EventHandler handler)
        {
            shortcutHandlers.Add(Tuple.Create(modifiers, key, handler));
        }
    }
}
