using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GW2AddonManager.UI.Controls
{
    [TemplatePart(Name = "PART_Dropdown", Type = typeof(Button))]
    [DefaultProperty("MenuItems")]
    public class SplitButton : Button
    {   
        public static readonly DependencyProperty IsContextMenuOpenProperty;

        static SplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));

            IsContextMenuOpenProperty =
                DependencyProperty.Register(
                    nameof(IsContextMenuOpen),
                    typeof(bool),
                    typeof(SplitButton),
                    new FrameworkPropertyMetadata(
                        false,
                        new PropertyChangedCallback(OnIsContextMenuOpenChanged))
                    );
        }

        public SplitButton()
        {

        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.Template.FindName("PART_Dropdown", this) is ButtonBase dropDown)
            {
                dropDown.Click += Dropdown_Click;
            }
        }

        public bool IsContextMenuOpen
        {
            get => (bool)GetValue(IsContextMenuOpenProperty);
            set => SetValue(IsContextMenuOpenProperty, value);
        }

        public ItemCollection MenuItems
        {
            get
            {
                EnsureContextMenuIsValid();
                return this.ContextMenu.Items;
            }
        }

        private void EnsureContextMenuIsValid()
        {
            if (this.ContextMenu == null)
            {
                this.ContextMenu = new ContextMenu();
                this.ContextMenu.PlacementTarget = this;
                this.ContextMenu.Placement = PlacementMode.Bottom;

                this.ContextMenu.Opened += ((sender, routedEventArgs) => IsContextMenuOpen = true);
                this.ContextMenu.Closed += ((sender, routedEventArgs) => IsContextMenuOpen = false);
            }
        }

        private void OnDropdown()
        {
            EnsureContextMenuIsValid();
            if (!this.ContextMenu.HasItems)
                return;

            this.ContextMenu.IsOpen = !IsContextMenuOpen; // open it if closed, close it if open
        }

        private void Dropdown_Click(object sender, RoutedEventArgs e)
        {
            OnDropdown();
            e.Handled = true;
        }

        private static void OnIsContextMenuOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SplitButton btn)
            {
                btn.EnsureContextMenuIsValid();

                if (!btn.ContextMenu.HasItems)
                {
                    return;
                }

                if (e.NewValue is bool value)
                {
                    if (value && !btn.ContextMenu.IsOpen)
                    {
                        btn.ContextMenu.IsOpen = true;
                    }
                    else if (!value && btn.ContextMenu.IsOpen)
                    {
                        btn.ContextMenu.IsOpen = false;
                    }
                }
            }
        }
    }

}
