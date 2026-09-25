using FinalStatsPlugin.Settings;
using Hearthstone_Deck_Tracker.API;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace FinalStatsPlugin.UI.Settings
{
    internal static class PluginSettingsFlyout
    {
        private static object _flyout;
        private static FinalStatsSettings _settings;
        private static ComboBox _finalScreenshotFilter;
        private static bool _updatingControls;

        public static void Show(FinalStatsSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
            EnsureFlyout();
            SyncControls();

            PropertyInfo isOpenProperty =
                _flyout?.GetType().GetProperty("IsOpen");

            if (isOpenProperty == null)
            {
                throw new InvalidOperationException(
                    "MahApps Flyout.IsOpen was not found."
                );
            }

            isOpenProperty.SetValue(_flyout, true, null);
        }

        public static void Detach()
        {
            try
            {
                if (_flyout == null)
                    return;

                PropertyInfo isOpenProperty =
                    _flyout.GetType().GetProperty("IsOpen");
                isOpenProperty?.SetValue(_flyout, false, null);

                ItemsControl flyoutsControl =
                    TryGetHdtFlyoutsControl();
                flyoutsControl?.Items.Remove(_flyout);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "HDT-FinalStatsPlugin settings flyout detach failed: "
                    + ex
                );
            }
            finally
            {
                _flyout = null;
                _settings = null;
                _finalScreenshotFilter = null;
                _updatingControls = false;
            }
        }

        private static void EnsureFlyout()
        {
            if (_flyout != null)
                return;

            Type flyoutType = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(
                    assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "MahApps.Metro",
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                ?.GetType("MahApps.Metro.Controls.Flyout");

            if (flyoutType == null)
            {
                throw new InvalidOperationException(
                    "MahApps.Metro.Controls.Flyout is not available in HDT."
                );
            }

            object flyout = Activator.CreateInstance(flyoutType);

            if (!(flyout is HeaderedContentControl flyoutControl))
            {
                throw new InvalidOperationException(
                    "The MahApps Flyout is not a HeaderedContentControl."
                );
            }

            flyoutControl.Header =
                "Battlegrounds Final Stats Options";

            if (flyout is FrameworkElement flyoutElement)
                flyoutElement.Width = 360;

            if (flyout is UIElement flyoutUiElement)
                Panel.SetZIndex(flyoutUiElement, 100);

            PropertyInfo positionProperty =
                flyoutType.GetProperty("Position");

            if (
                positionProperty != null
                && positionProperty.PropertyType.IsEnum
            )
            {
                object leftPosition = Enum.Parse(
                    positionProperty.PropertyType,
                    "Left",
                    true
                );
                positionProperty.SetValue(
                    flyout,
                    leftPosition,
                    null
                );
            }

            StackPanel root = new StackPanel
            {
                Margin = new Thickness(16)
            };

            root.Children.Add(
                new TextBlock
                {
                    Text = "Final Screenshot only on :",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                }
            );

            _finalScreenshotFilter = new ComboBox
            {
                Width = 160,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 8)
            };

            AddOption(
                "All",
                FinalScreenshotPlacementFilter.All
            );
            AddOption(
                "Top 4",
                FinalScreenshotPlacementFilter.Top4
            );
            AddOption(
                "Top 3",
                FinalScreenshotPlacementFilter.Top3
            );
            AddOption(
                "Top 2",
                FinalScreenshotPlacementFilter.Top2
            );
            AddOption(
                "Top 1",
                FinalScreenshotPlacementFilter.Top1
            );

            _finalScreenshotFilter.SelectionChanged +=
                HandleFinalScreenshotFilterChanged;

            root.Children.Add(_finalScreenshotFilter);
            flyoutControl.Content = root;

            ItemsControl flyoutsControl =
                TryGetHdtFlyoutsControl();

            if (flyoutsControl == null)
            {
                throw new InvalidOperationException(
                    "HDT MainWindow.Flyouts was not found."
                );
            }

            flyoutsControl.Items.Add(flyout);
            _flyout = flyout;
        }

        private static void AddOption(
            string label,
            FinalScreenshotPlacementFilter value)
        {
            _finalScreenshotFilter.Items.Add(
                new ComboBoxItem
                {
                    Content = label,
                    Tag = value
                }
            );
        }

        private static void SyncControls()
        {
            if (
                _settings == null
                || _finalScreenshotFilter == null
            )
            {
                return;
            }

            _updatingControls = true;

            try
            {
                foreach (
                    ComboBoxItem item
                    in _finalScreenshotFilter.Items
                )
                {
                    if (
                        item.Tag
                            is FinalScreenshotPlacementFilter value
                        && value
                            == _settings.FinalScreenshotOnlyOn
                    )
                    {
                        _finalScreenshotFilter.SelectedItem =
                            item;
                        break;
                    }
                }
            }
            finally
            {
                _updatingControls = false;
            }
        }

        private static void HandleFinalScreenshotFilterChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (
                _updatingControls
                || _settings == null
                || !(
                    _finalScreenshotFilter?.SelectedItem
                        is ComboBoxItem selectedItem
                )
                || !(
                    selectedItem.Tag
                        is FinalScreenshotPlacementFilter filter
                )
            )
            {
                return;
            }

            _settings.FinalScreenshotOnlyOn = filter;
            SettingsService.Save(_settings);
        }

        private static ItemsControl TryGetHdtFlyoutsControl()
        {
            object mainWindow = Core.MainWindow;
            if (mainWindow == null)
                return null;

            PropertyInfo flyoutsProperty =
                mainWindow.GetType().GetProperty("Flyouts");

            return flyoutsProperty?.GetValue(
                mainWindow,
                null
            ) as ItemsControl;
        }
    }
}
