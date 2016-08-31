using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace TranslateWithDictCC.Controls
{
    [TemplatePart(Name = "PART_TogglingButton", Type = typeof(Button))]
    public sealed class HamburgerMenuItem : ToggleButton
    {
        public static DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(HamburgerMenuItem), null);

        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public event EventHandler CheckRequested;

        public HamburgerMenuItem()
        {
            DefaultStyleKey = typeof(HamburgerMenuItem);

            RegisterPropertyChangedCallback(ToggleButton.IsCheckedProperty, IsCheckedChanged);
        }

        private void IsCheckedChanged(DependencyObject sender, DependencyProperty property)
        {
            VisualStateManager.GoToState(this, IsChecked.Value ? "Checked" : "Unchecked", false);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            if (Template != null)
            {
                Button togglingButton = GetTemplateChild("PART_TogglingButton") as Button;
                
                if (togglingButton != null)
                    togglingButton.Click += TogglingButton_Click;
            }
        }

        private void TogglingButton_Click(object sender, RoutedEventArgs e)
        {
            CheckRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
