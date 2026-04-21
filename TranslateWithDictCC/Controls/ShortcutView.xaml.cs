using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace TranslateWithDictCC.Controls;

public partial class ShortcutView : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ShortcutView),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ShortcutView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        ((ShortcutView)dependencyObject).RefreshShortcut();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshShortcut();
    }

    private void RefreshShortcut()
    {
        shortcutPanel.Children.Clear();

        string[] keyNames = Text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < keyNames.Length; i++)
        {
            if (i > 0)
            {
                shortcutPanel.Children.Add(new TextBlock
                {
                    Text = "+",
                    Style = (Style)Resources["ShortcutSeparatorTextStyle"]
                });
            }

            shortcutPanel.Children.Add(new Border
            {
                Style = (Style)Resources["ShortcutKeyBorderStyle"],
                Child = new TextBlock
                {
                    Text = keyNames[i],
                    Style = (Style)Resources["ShortcutKeyTextStyle"]
                }
            });
        }

        AutomationProperties.SetName(this, Text);
    }
}
