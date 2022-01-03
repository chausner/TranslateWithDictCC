﻿using System;
using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;

namespace TranslateWithDictCC
{
    class KeyboardShortcutListener
    {
        private record ShortcutHandler(VirtualKeyModifiers VirtualKeyModifiers, VirtualKey VirtualKey, EventHandler Handler);

        bool controlPressed;
        bool menuPressed;
        bool shiftPressed;

        List<ShortcutHandler> shortcutHandlers = new List<ShortcutHandler>();

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
                if (shortcutHandler.VirtualKeyModifiers == modifiers &&
                    shortcutHandler.VirtualKey == args.VirtualKey)
                    shortcutHandler.Handler(this, EventArgs.Empty);
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
            shortcutHandlers.Add(new ShortcutHandler(modifiers, key, handler));
        }
    }
}
