using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Controls;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private TextBlock _heroValue;
        private TextBlock _placementValue;
        private TextBlock _mmrValue;
        private TextBlock _turnValue;
        private TextBlock _highestCreatureValue;
        private TextBlock _durationValue;

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

            Grid root = new Grid
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };
            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                }
            );
            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                }
            );
            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            Grid header = CreateHeader();
            Border separator = new Border
            {
                Height = 1,
                Background = CreateFrozenBrush(
                    Color.FromArgb(55, 255, 255, 255)
                ),
                Margin = new Thickness(4, 5, 4, 5),
                IsHitTestVisible = false
            };

            Grid.SetRow(header, 0);
            Grid.SetRow(separator, 1);
            Grid.SetRow(_board, 2);
            root.Children.Add(header);
            root.Children.Add(separator);
            root.Children.Add(_board);

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
                Padding = new Thickness(20, 8, 20, 10),
                Child = root,
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
            _heroValue = null;
            _placementValue = null;
            _mmrValue = null;
            _turnValue = null;
            _highestCreatureValue = null;
            _durationValue = null;
        }

        public void UpdateSummary(
            IReadOnlyList<Entity> entities,
            FinalBoardSummaryData data)
        {
            EnsureCreated();
            UpdateHeader(data);
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

        private Grid CreateHeader()
        {
            Grid header = new Grid
            {
                Width = 830,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            AddHeaderColumn(header, 210);
            AddHeaderColumn(header, 75);
            AddHeaderColumn(header, 115);
            AddHeaderColumn(header, 90);
            AddHeaderColumn(header, 220);
            AddHeaderColumn(header, 120);

            AddHeaderCell(
                header,
                0,
                "HERO",
                out _heroValue,
                18
            );
            AddHeaderCell(
                header,
                1,
                "PLACE",
                out _placementValue,
                17
            );
            AddHeaderCell(
                header,
                2,
                "MMR",
                out _mmrValue,
                17
            );
            AddHeaderCell(
                header,
                3,
                "TURN",
                out _turnValue,
                17
            );
            AddHeaderCell(
                header,
                4,
                "HIGHEST CREATURE",
                out _highestCreatureValue,
                17
            );
            AddHeaderCell(
                header,
                5,
                "DURATION",
                out _durationValue,
                17
            );

            _heroValue.Foreground = CreateFrozenBrush(
                Color.FromRgb(218, 184, 108)
            );

            return header;
        }

        private static void AddHeaderColumn(
            Grid header,
            double width)
        {
            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(width)
                }
            );
        }

        private static void AddHeaderCell(
            Grid header,
            int column,
            string label,
            out TextBlock value,
            double valueFontSize)
        {
            StackPanel cell = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0),
                IsHitTestVisible = false
            };

            TextBlock labelText = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateFrozenBrush(
                    Color.FromRgb(154, 161, 169)
                ),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsHitTestVisible = false
            };

            value = new TextBlock
            {
                Text = "—",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = valueFontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateFrozenBrush(
                    Color.FromRgb(238, 241, 244)
                ),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis,
                IsHitTestVisible = false
            };

            cell.Children.Add(labelText);
            cell.Children.Add(value);
            Grid.SetColumn(cell, column);
            header.Children.Add(cell);
        }

        private void UpdateHeader(FinalBoardSummaryData data)
        {
            data = data ?? new FinalBoardSummaryData();

            _heroValue.Text =
                string.IsNullOrWhiteSpace(data.HeroName)
                    ? "Unknown hero"
                    : data.HeroName;
            _placementValue.Text =
                FormatPlacement(data.Placement);
            _turnValue.Text =
                data.Turn > 0
                    ? data.Turn.ToString(
                        CultureInfo.InvariantCulture
                    )
                    : "—";
            _highestCreatureValue.Text =
                data.HighestCreatureAttack > 0
                || data.HighestCreatureHealth > 0
                    ? data.HighestCreatureAttack.ToString(
                        CultureInfo.InvariantCulture
                    )
                        + " / "
                        + data.HighestCreatureHealth.ToString(
                            CultureInfo.InvariantCulture
                        )
                    : "—";
            _durationValue.Text =
                FormatDuration(data.Duration);

            if (data.MmrDelta.HasValue)
            {
                int delta = data.MmrDelta.Value;
                _mmrValue.Text =
                    delta > 0
                        ? "+"
                            + delta.ToString(
                                CultureInfo.InvariantCulture
                            )
                        : delta.ToString(
                            CultureInfo.InvariantCulture
                        );
                _mmrValue.Foreground = CreateFrozenBrush(
                    delta > 0
                        ? Color.FromRgb(91, 203, 154)
                        : delta < 0
                            ? Color.FromRgb(240, 123, 123)
                            : Color.FromRgb(154, 161, 169)
                );
            }
            else
            {
                _mmrValue.Text = "—";
                _mmrValue.Foreground = CreateFrozenBrush(
                    Color.FromRgb(154, 161, 169)
                );
            }
        }

        private static string FormatPlacement(int placement)
        {
            if (placement <= 0)
                return "—";

            int lastTwoDigits = placement % 100;
            string suffix;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            {
                suffix = "th";
            }
            else
            {
                switch (placement % 10)
                {
                    case 1:
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;
                    case 3:
                        suffix = "rd";
                        break;
                    default:
                        suffix = "th";
                        break;
                }
            }

            return placement.ToString(
                CultureInfo.InvariantCulture
            )
                + suffix;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;

            long totalHours = (long)duration.TotalHours;

            if (totalHours > 0)
            {
                return totalHours.ToString(
                    CultureInfo.InvariantCulture
                )
                    + ":"
                    + duration.Minutes.ToString(
                        "00",
                        CultureInfo.InvariantCulture
                    )
                    + ":"
                    + duration.Seconds.ToString(
                        "00",
                        CultureInfo.InvariantCulture
                    );
            }

            return ((int)duration.TotalMinutes).ToString(
                "00",
                CultureInfo.InvariantCulture
            )
                + ":"
                + duration.Seconds.ToString(
                    "00",
                    CultureInfo.InvariantCulture
                );
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

        public void SavePng(string filePath)
        {
            EnsureCreated();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "A screenshot path is required.",
                    nameof(filePath)
                );
            }

            string temporaryPath =
                filePath
                + ".tmp-"
                + Guid.NewGuid().ToString("N");
            Visibility previousVisibility = _panel.Visibility;

            try
            {
                _panel.Visibility = Visibility.Visible;
                _panel.UpdateLayout();

                RenderTargetBitmap bitmap =
                    new RenderTargetBitmap(
                        (int)PanelWidth,
                        (int)PanelHeight,
                        96,
                        96,
                        PixelFormats.Pbgra32
                    );

                // Rendering the panel directly also applies its Canvas
                // position. The content then lands outside a 900 x 220
                // bitmap and produces a fully transparent PNG. A
                // VisualBrush renders the panel in local coordinates.
                DrawingVisual localVisual = new DrawingVisual();

                using (
                    DrawingContext drawingContext =
                        localVisual.RenderOpen()
                )
                {
                    drawingContext.DrawRectangle(
                        new VisualBrush(_panel)
                        {
                            Stretch = Stretch.Fill
                        },
                        null,
                        new Rect(
                            0,
                            0,
                            PanelWidth,
                            PanelHeight
                        )
                    );
                }

                bitmap.Render(localVisual);
                EnsureBitmapContainsVisiblePixels(bitmap);

                PngBitmapEncoder encoder =
                    new PngBitmapEncoder();
                encoder.Frames.Add(
                    BitmapFrame.Create(bitmap)
                );

                using (
                    FileStream stream = new FileStream(
                        temporaryPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None
                    )
                )
                {
                    encoder.Save(stream);
                    stream.Flush();
                }

                File.Move(temporaryPath, filePath);
            }
            finally
            {
                _panel.Visibility = previousVisibility;

                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void EnsureBitmapContainsVisiblePixels(
            BitmapSource bitmap)
        {
            int stride =
                bitmap.PixelWidth
                * (bitmap.Format.BitsPerPixel / 8);
            byte[] pixels =
                new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(pixels, stride, 0);

            for (int index = 3; index < pixels.Length; index += 4)
            {
                if (pixels[index] != 0)
                    return;
            }

            throw new InvalidOperationException(
                "The rendered final-board screenshot is fully transparent."
            );
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    internal sealed class FinalBoardSummaryData
    {
        public string HeroName { get; set; }
        public int Placement { get; set; }
        public int? MmrDelta { get; set; }
        public int Turn { get; set; }
        public int HighestCreatureAttack { get; set; }
        public int HighestCreatureHealth { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
