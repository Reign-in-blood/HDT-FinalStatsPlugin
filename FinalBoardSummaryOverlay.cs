using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Controls;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FinalStatsPlugin
{
    internal sealed class FinalBoardSummaryOverlay
    {
        private const double PanelWidth = 900;
        private const double PanelHeight = 220;
        private const double PanelLeft = 305;
        private const double PanelBottom = 100;
        private const double BottomCornerRadius = 36;
        private const double MinionSize = 120;

        private Border _panel;
        private StackPanel _board;

        public void EnsureCreated()
        {
            if (_panel != null)
                return;

            _board = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            _panel = new Border
            {
                Width = PanelWidth,
                Height = PanelHeight,
                Background = CreateFrozenBrush(
                    Color.FromArgb(250, 8, 9, 11)
                ),
                BorderBrush = CreateFrozenBrush(
                    Color.FromArgb(70, 255, 255, 255)
                ),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(
                    0,
                    0,
                    BottomCornerRadius,
                    BottomCornerRadius
                ),
                Padding = new Thickness(20, 18, 20, 24),
                Child = _board,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            Panel.SetZIndex(_panel, 995);
            Core.OverlayCanvas.Children.Add(_panel);
        }

        public void Remove()
        {
            if (_panel != null)
                Core.OverlayCanvas.Children.Remove(_panel);

            _panel = null;
            _board = null;
        }

        public void UpdateBoard(IReadOnlyList<Entity> entities)
        {
            EnsureCreated();
            _board.Children.Clear();

            if (entities == null || entities.Count == 0)
            {
                _board.Children.Add(
                    new TextBlock
                    {
                        Text = "No minions on the final board",
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 18,
                        Foreground = CreateFrozenBrush(
                            Color.FromArgb(170, 255, 255, 255)
                        ),
                        HorizontalAlignment =
                            HorizontalAlignment.Center,
                        VerticalAlignment =
                            VerticalAlignment.Center,
                        IsHitTestVisible = false
                    }
                );
                return;
            }

            foreach (Entity entity in entities)
            {
                _board.Children.Add(
                    new BattlegroundsMinion(entity)
                    {
                        Width = MinionSize,
                        Height = MinionSize,
                        Margin = new Thickness(1),
                        IsHitTestVisible = false
                    }
                );
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureCreated();
            _panel.Visibility =
                visible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        public void Position()
        {
            if (_panel == null)
                return;

            double canvasHeight = Core.OverlayCanvas.ActualHeight;
            double top = System.Math.Max(
                0,
                canvasHeight - PanelBottom - PanelHeight
            );

            Canvas.SetLeft(_panel, PanelLeft);
            Canvas.SetTop(_panel, top);
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
