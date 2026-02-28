using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using Windows.System;

namespace TranslateWithDictCC;

class KeyboardShortcutListener
{
    private record ShortcutHandler(VirtualKeyModifiers VirtualKeyModifiers, VirtualKey VirtualKey, KeyEventHandler Handler);

    bool controlPressed;
    bool menuPressed;
    bool shiftPressed;

    List<ShortcutHandler> shortcutHandlers = new List<ShortcutHandler>();

    public KeyboardShortcutListener(UIElement element)
    {
        element.KeyDown += Element_KeyDown;
        element.KeyUp += Element_KeyUp;
    }

    private void Element_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
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
                shortcutHandler.VirtualKey == e.Key)                
                shortcutHandler.Handler(this, e);
    }

    private void Element_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
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

    public void RegisterShortcutHandler(VirtualKeyModifiers modifiers, VirtualKey key, KeyEventHandler handler)
    {
        shortcutHandlers.Add(new ShortcutHandler(modifiers, key, handler));
    }
}
