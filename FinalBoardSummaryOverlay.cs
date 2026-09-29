using FinalStatsPlugin.Settings;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Controls;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Utility.Assets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FinalStatsPlugin
{
    internal sealed class FinalBoardSummaryOverlay
    {
        private const double ReferenceWidth = 1920;
        private const double ReferenceHeight = 1080;
        private const double PanelWidth = 920;
        private const double PanelHeight = 410;
        private const double DetailedExtraHeight = PanelHeight;
        private const double DuoExtraHeight = 180;
        private const double PanelTop = 85;
        private const double PanelLeft = 304;
        private const double MinionSize = 134; //Taille des Minions
        private const string PluginDisplayName =
            "Battlegrounds Final Stats";

        private static readonly Brush DuoPanelBrush =
            CreateFrozenBrush(Color.FromRgb(0, 0, 0));

        private static readonly Brush DetailedDividerBrush =
            CreateFrozenBrush(Color.FromArgb(28, 255, 255, 255));

        private static readonly Brush DetailedTitleBrush =
            CreateFrozenBrush(Color.FromRgb(218, 184, 108));

        private static readonly Brush DetailedCategoryBrush =
            CreateFrozenBrush(Color.FromRgb(184, 157, 99));

        private static readonly Brush DetailedLabelBrush =
            CreateFrozenBrush(Color.FromRgb(178, 184, 191));

        private static readonly Brush DetailedValueBrush =
            CreateFrozenBrush(Color.FromRgb(238, 241, 244));

        private static readonly Brush DetailedPositiveBrush =
            CreateFrozenBrush(Color.FromRgb(91, 203, 154));

        private static readonly Brush DetailedNegativeBrush =
            CreateFrozenBrush(Color.FromRgb(240, 123, 123));

        private static readonly Brush DetailedNeutralBrush =
            CreateFrozenBrush(Color.FromRgb(154, 161, 169));

        private Border _panel;
        private Grid _displayHost;
        private ScaleTransform _displayScale;
        private Brush _soloPanelBackground;
        private StackPanel _board;
        private Grid _footer;
        private RowDefinition _partnerRow;
        private Grid _partnerArea;
        private StackPanel _partnerBoard;
        private TextBlock _partnerNameValue;
        private RowDefinition _detailedStatsRow;
        private Border _detailedStatsArea;
        private TextBlock _detailedGoldSpentValue;
        private TextBlock _detailedTavernRollsValue;
        private TextBlock _detailedFreeRollsValue;
        private TextBlock _detailedCardsBoughtValue;
        private TextBlock _detailedMinionsBoughtValue;
        private TextBlock _detailedSpellsBoughtValue;
        private TextBlock _detailedCardsPlayedValue;
        private TextBlock _detailedMinionsPlayedValue;
        private TextBlock _detailedSpellsPlayedValue;
        private TextBlock _detailedCombatWinsValue;
        private TextBlock _detailedCombatLossesValue;
        private TextBlock _detailedCombatDrawsValue;
        private CardImage _heroPortrait;
        private Border _heroPowerContainer;
        private HeroPower _heroPower;
        private int _heroPowerEntityId;
        private string _heroPowerCardId;
        private Border _deityContainer;
        private BattlegroundsMinion _deityMinion;
        private string _deityCardId;
        private int _deityAttack;
        private int _deityHealth;
        private bool _deityIsGolden;
        private StackPanel _trinketPanel;
        private readonly List<Trinket> _trinkets =
            new List<Trinket>();
        private StackPanel _anomalySection;
        private Grid _anomalyVisualContainer;
        private CardImage _anomalyImage;
        private CardImage _anomalyPortrait;
        private HeroPower _anomalyHeroPower;
        private string _anomalyCardId;
        private int _anomalyHeroPowerEntityId;
        private string _anomalyHeroPowerCardId;
        private string _anomalyVisualMode = "none";
        private TextBlock _heroValue;
        private TextBlock _placementValue;
        private TextBlock _mmrValue;
        private TextBlock _turnValue;
        private TextBlock _highestCreatureValue;
        private TextBlock _durationValue;
        private TextBlock _playerNameValue;
        private TextBlock _pluginNameValue;

        public void EnsureCreated()
        {
            if (_panel != null)
                return;

            _board = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = new TranslateTransform(0, -35), //hauteur des minions
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
                    Height = new GridLength(204) //212
                }
            );
            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(58)
                }
            );
            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        PanelHeight - 204 - 58
                    )
                }
            );

            _partnerRow = new RowDefinition
            {
                Height = new GridLength(0)
            };
            root.RowDefinitions.Add(_partnerRow);

            _detailedStatsRow = new RowDefinition
            {
                Height = new GridLength(0)
            };
            root.RowDefinitions.Add(_detailedStatsRow);

            Grid header = CreateHeader();
            FrameworkElement heroDetails = CreateHeroDetails();
            FrameworkElement boardArea = CreateBoardArea();
            _partnerArea = CreatePartnerArea();
            _detailedStatsArea = CreateDetailedStatsArea();
            Border headerBar = new Border
            {
                Padding = new Thickness(16, 4, 16, 4),
                RenderTransform = new TranslateTransform(0, -45),
                Child = header,
                IsHitTestVisible = false
            };

            Grid.SetRow(heroDetails, 0);
            Grid.SetRow(headerBar, 1);
            Grid.SetRow(boardArea, 2);
            Grid.SetRow(_partnerArea, 3);
            Grid.SetRow(_detailedStatsArea, 4);
            Panel.SetZIndex(headerBar, 0);
            Panel.SetZIndex(boardArea, 5);
            Panel.SetZIndex(heroDetails, 10);
            Panel.SetZIndex(_partnerArea, 5);
            Panel.SetZIndex(_detailedStatsArea, 5);
            root.Children.Add(heroDetails);
            root.Children.Add(headerBar);
            root.Children.Add(boardArea);
            root.Children.Add(_partnerArea);
            root.Children.Add(_detailedStatsArea);

            _soloPanelBackground = CreatePanelBackground();

            _panel = new Border
            {
                Width = PanelWidth,
                Height = PanelHeight,
                Background = _soloPanelBackground,
                BorderBrush = CreateFrozenBrush(
                    Color.FromArgb(70, 255, 255, 255)
                ),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(0),
                Child = root,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            _displayScale = new ScaleTransform(1.0, 1.0);

            _displayHost = new Grid
            {
                Width = PanelWidth,
                Height = PanelHeight,
                RenderTransform = _displayScale,
                RenderTransformOrigin = new Point(0, 0),
                IsHitTestVisible = false
            };
            _displayHost.Children.Add(_panel);

            Panel.SetZIndex(_displayHost, 995);
            Core.OverlayCanvas.Children.Add(_displayHost);
        }

        public void Remove()
        {
            if (_displayHost != null)
                Core.OverlayCanvas.Children.Remove(_displayHost);

            _panel = null;
            _displayHost = null;
            _displayScale = null;
            _soloPanelBackground = null;
            _board = null;
            _footer = null;
            _partnerRow = null;
            _partnerArea = null;
            _partnerBoard = null;
            _partnerNameValue = null;
            _detailedStatsRow = null;
            _detailedStatsArea = null;
            _detailedGoldSpentValue = null;
            _detailedTavernRollsValue = null;
            _detailedFreeRollsValue = null;
            _detailedCardsBoughtValue = null;
            _detailedMinionsBoughtValue = null;
            _detailedSpellsBoughtValue = null;
            _detailedCardsPlayedValue = null;
            _detailedMinionsPlayedValue = null;
            _detailedSpellsPlayedValue = null;
            _detailedCombatWinsValue = null;
            _detailedCombatLossesValue = null;
            _detailedCombatDrawsValue = null;
            _heroPortrait = null;
            _heroPowerContainer = null;
            _heroPower = null;
            _heroPowerEntityId = 0;
            _heroPowerCardId = null;
            _deityContainer = null;
            _deityMinion = null;
            _deityCardId = null;
            _deityAttack = 0;
            _deityHealth = 0;
            _deityIsGolden = false;
            _trinketPanel = null;
            _trinkets.Clear();
            _anomalySection = null;
            _anomalyVisualContainer = null;
            _anomalyImage = null;
            _anomalyPortrait = null;
            _anomalyHeroPower = null;
            _anomalyCardId = null;
            _anomalyHeroPowerEntityId = 0;
            _anomalyHeroPowerCardId = null;
            _anomalyVisualMode = "none";
            _heroValue = null;
            _placementValue = null;
            _mmrValue = null;
            _turnValue = null;
            _highestCreatureValue = null;
            _durationValue = null;
            _playerNameValue = null;
            _pluginNameValue = null;
        }

        public void UpdateSummary(
            IReadOnlyList<Entity> entities,
            FinalBoardSummaryData data)
        {
            EnsureCreated();
            UpdateDuosPresentation(data);
            UpdateDetailedStats(data);
            UpdateHeader(data);
            UpdateFooter(data);
            UpdateHeroPortrait(data);
            UpdateHeroPower(data);
            UpdateDeity(data);
            UpdateTrinkets(data);
            UpdateAnomaly(data);
            UpdatePartnerBoard(data);
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
                        Margin = new Thickness(-5, 0, -5, 0), //espacement des cartes
                        IsHitTestVisible = false
                    }
                );
            }
        }

        private Grid CreateHeader()
        {
            Grid header = new Grid
            {
                Width = 740,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            AddHeaderColumn(header, 80);
            AddHeaderColumn(header, 110);
            AddHeaderColumn(header, 90);
            AddHeaderColumn(header, 180);
            AddHeaderColumn(header, 170);
            AddHeaderColumn(header, 110);

            AddHeaderCell(
                header,
                0,
                "RANK",
                out _placementValue,
                17
            );
            AddHeaderCell(
                header,
                1,
                "MMR",
                out _mmrValue,
                17
            );
            AddHeaderCell(
                header,
                2,
                "TURN",
                out _turnValue,
                17
            );
            AddHeaderCell(
                header,
                3,
                "HERO",
                out _heroValue,
                18
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

        private FrameworkElement CreateHeroDetails()
        {
            Grid details = new Grid
            {
                Width = 840,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = new TranslateTransform(0, -15),
                IsHitTestVisible = false
            };
            details.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(320)
                }
            );
            details.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(200)
                }
            );
            details.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(320)
                }
            );

            _heroPortrait = new CardImage
            {
                Width = 190,
                Height = 268,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            Grid.SetColumn(_heroPortrait, 1);
            details.Children.Add(_heroPortrait);

            StackPanel trinketSection = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            _trinketPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            trinketSection.Children.Add(_trinketPanel);
            Grid.SetColumn(trinketSection, 0);
            details.Children.Add(trinketSection);

            StackPanel rightSection = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            _heroPowerContainer = new Border
            {
                Width = 130,
                Height = 130,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            rightSection.Children.Add(_heroPowerContainer);

            _deityContainer = new Border
            {
                Width = 100,
                Height = 100,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            rightSection.Children.Add(_deityContainer);

            _anomalySection = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };

            _anomalyImage = new CardImage
            {
                Width = 90,
                Height = 130,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            _anomalyPortrait = new CardImage
            {
                Width = 90,
                Height = 130,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };

            _anomalyVisualContainer = new Grid
            {
                Width = 90,
                Height = 130,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            _anomalyVisualContainer.Children.Add(_anomalyImage);
            _anomalyVisualContainer.Children.Add(_anomalyPortrait);
            _anomalySection.Children.Add(
                _anomalyVisualContainer
            );
            rightSection.Children.Add(_anomalySection);

            Grid.SetColumn(rightSection, 2);
            details.Children.Add(rightSection);
            return details;
        }

        private FrameworkElement CreateBoardArea()
        {
            Grid boardArea = new Grid
            {
                ClipToBounds = false, //add
                IsHitTestVisible = false
            };
            boardArea.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            _footer = new Grid
            {
                RenderTransform = new TranslateTransform(0, 7),
                IsHitTestVisible = false
            };

            _playerNameValue = new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateFrozenBrush(
                    Color.FromRgb(104, 109, 116)
                ),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(20, 0, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 620,
                IsHitTestVisible = false
            };

            _pluginNameValue = new TextBlock
            {
                Text = PluginDisplayName,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateFrozenBrush(
                    Color.FromRgb(132, 115, 78)
                ),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 20, 0),
                IsHitTestVisible = false
            };

            _footer.Children.Add(_playerNameValue);
            _footer.Children.Add(_pluginNameValue);

            Panel.SetZIndex(_board, 10);
            Panel.SetZIndex(_footer, 20);

            boardArea.Children.Add(_board);
            boardArea.Children.Add(_footer);
            return boardArea;
        }

        private Grid CreatePartnerArea()
        {
            Grid area = new Grid
            {
                Visibility = Visibility.Collapsed,
                ClipToBounds = false,
                RenderTransform = new TranslateTransform(0, -15),
                IsHitTestVisible = false
            };

            area.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(30)
                }
            );
            area.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            _partnerNameValue = new TextBlock
            {
                Text = "Partner : —",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = CreateFrozenBrush(
                    Color.FromRgb(178, 184, 191)
                ),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 440,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };

            _partnerBoard = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = new TranslateTransform(0, -23),
                IsHitTestVisible = false
            };

            Grid.SetRow(_partnerNameValue, 0);
            Grid.SetRow(_partnerBoard, 1);
            area.Children.Add(_partnerNameValue);
            area.Children.Add(_partnerBoard);
            return area;
        }

        private void UpdateDuosPresentation(
            FinalBoardSummaryData data)
        {
            bool isDuos = data?.IsDuosMatch == true;
            bool isDetailed =
                data?.DisplayMode
                    == FinalBoardDisplayMode.Detailed;

            _panel.Height = PanelHeight
                + (isDuos ? DuoExtraHeight : 0)
                + (isDetailed ? DetailedExtraHeight : 0);

            if (_displayHost != null)
                _displayHost.Height = _panel.Height;

            _partnerRow.Height = new GridLength(
                isDuos ? DuoExtraHeight : 0
            );
            _partnerArea.Visibility =
                isDuos
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            _detailedStatsRow.Height = new GridLength(
                isDetailed ? DetailedExtraHeight : 0
            );
            _detailedStatsArea.Visibility =
                isDetailed
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            _panel.Background =
                isDuos || isDetailed
                    ? DuoPanelBrush
                    : _soloPanelBackground;

            if (_footer != null)
            {
                _footer.RenderTransform = new TranslateTransform(
                    0,
                    7
                    + (isDuos ? DuoExtraHeight : -15)
                    + (isDetailed ? DetailedExtraHeight : 0)
                );
            }
        }

        private void UpdatePartnerBoard(
            FinalBoardSummaryData data)
        {
            if (_partnerBoard == null)
                return;

            _partnerBoard.Children.Clear();

            if (data?.IsDuosMatch != true)
                return;

            IReadOnlyList<Entity> partnerEntities =
                data.PartnerBoardEntities;

            if (
                partnerEntities == null
                || partnerEntities.Count == 0
            )
            {
                _partnerBoard.Children.Add(
                    new TextBlock
                    {
                        Text = "Partner board unavailable",
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 14,
                        Foreground = CreateFrozenBrush(
                            Color.FromArgb(
                                170,
                                255,
                                255,
                                255
                            )
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

            foreach (Entity entity in partnerEntities)
            {
                if (entity == null)
                    continue;

                _partnerBoard.Children.Add(
                    new BattlegroundsMinion(entity)
                    {
                        Width = MinionSize,
                        Height = MinionSize,
                        Margin = new Thickness(-5, 0, -5, 0),
                        IsHitTestVisible = false
                    }
                );
            }
        }

        private Border CreateDetailedStatsArea()
        {
            Grid content = new Grid
            {
                Margin = new Thickness(48, 28, 48, 28),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };

            content.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                }
            );
            content.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                }
            );
            content.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            TextBlock title = new TextBlock
            {
                Text = "FINAL STATS",
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = DetailedTitleBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 0, 0, 4),
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Border separator = new Border
            {
                Height = 1,
                Background = DetailedDividerBrush,
                Margin = new Thickness(0, 0, 0, 8),
                IsHitTestVisible = false
            };

            Grid statsGrid = new Grid
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };
            statsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );
            statsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(36)
                }
            );
            statsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            StackPanel leftColumn = new StackPanel
            {
                IsHitTestVisible = false
            };

            leftColumn.Children.Add(
                CreateDetailedCategoryHeader(
                    "GLOBAL STATS",
                    0
                )
            );
            AddDetailedStatRow(
                leftColumn,
                "Gold spent",
                out _detailedGoldSpentValue
            );
            AddDetailedStatRow(
                leftColumn,
                "Tavern rolls",
                out _detailedTavernRollsValue
            );
            AddDetailedStatRow(
                leftColumn,
                "Free rolls gained",
                out _detailedFreeRollsValue
            );

            leftColumn.Children.Add(
                CreateDetailedCategoryHeader(
                    "BOUGHT CARDS",
                    10
                )
            );
            AddDetailedStatRow(
                leftColumn,
                "Cards bought",
                out _detailedCardsBoughtValue
            );
            AddDetailedStatRow(
                leftColumn,
                "Minions bought",
                out _detailedMinionsBoughtValue
            );
            AddDetailedStatRow(
                leftColumn,
                "Spells bought",
                out _detailedSpellsBoughtValue
            );

            StackPanel rightColumn = new StackPanel
            {
                IsHitTestVisible = false
            };

            rightColumn.Children.Add(
                CreateDetailedCategoryHeader(
                    "PLAYED CARDS",
                    0
                )
            );
            AddDetailedStatRow(
                rightColumn,
                "Played cards",
                out _detailedCardsPlayedValue
            );
            AddDetailedStatRow(
                rightColumn,
                "Played minions",
                out _detailedMinionsPlayedValue
            );
            AddDetailedStatRow(
                rightColumn,
                "Played spells",
                out _detailedSpellsPlayedValue
            );

            rightColumn.Children.Add(
                CreateDetailedCategoryHeader(
                    "COMBATS",
                    10
                )
            );
            AddDetailedStatRow(
                rightColumn,
                "Combat wins",
                out _detailedCombatWinsValue
            );
            AddDetailedStatRow(
                rightColumn,
                "Combat losses",
                out _detailedCombatLossesValue
            );
            AddDetailedStatRow(
                rightColumn,
                "Combat draws",
                out _detailedCombatDrawsValue
            );

            _detailedCombatWinsValue.Foreground =
                DetailedPositiveBrush;
            _detailedCombatLossesValue.Foreground =
                DetailedNegativeBrush;
            _detailedCombatDrawsValue.Foreground =
                DetailedNeutralBrush;

            Grid.SetColumn(leftColumn, 0);
            Grid.SetColumn(rightColumn, 2);
            statsGrid.Children.Add(leftColumn);
            statsGrid.Children.Add(rightColumn);

            Grid.SetRow(title, 0);
            Grid.SetRow(separator, 1);
            Grid.SetRow(statsGrid, 2);
            content.Children.Add(title);
            content.Children.Add(separator);
            content.Children.Add(statsGrid);

            return new Border
            {
                Background = DuoPanelBrush,
                Child = content,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
        }

        private static Grid CreateDetailedCategoryHeader(
            string title,
            double topMargin)
        {
            Grid header = new Grid
            {
                Height = 20,
                Margin = new Thickness(0, topMargin, 0, 0),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };

            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            );
            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );

            TextBlock headerText = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = DetailedCategoryBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Border line = new Border
            {
                Height = 1,
                Background = DetailedDividerBrush,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            Grid.SetColumn(headerText, 0);
            Grid.SetColumn(line, 1);
            header.Children.Add(headerText);
            header.Children.Add(line);
            return header;
        }

        private static void AddDetailedStatRow(
            Panel parent,
            string label,
            out TextBlock value)
        {
            Grid row = new Grid
            {
                Height = 23,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };

            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        1,
                        GridUnitType.Star
                    )
                }
            );
            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(92)
                }
            );

            TextBlock labelText = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = DetailedLabelBrush,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            value = new TextBlock
            {
                Text = "0",
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = DetailedValueBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Typography.SetNumeralAlignment(
                value,
                FontNumeralAlignment.Tabular
            );
            Typography.SetNumeralStyle(
                value,
                FontNumeralStyle.Lining
            );

            Grid.SetColumn(labelText, 0);
            Grid.SetColumn(value, 1);
            row.Children.Add(labelText);
            row.Children.Add(value);
            parent.Children.Add(row);
        }

        private void UpdateDetailedStats(
            FinalBoardSummaryData data)
        {
            if (_detailedGoldSpentValue == null)
                return;

            SetDetailedValue(
                _detailedGoldSpentValue,
                data?.GoldSpent ?? 0
            );
            SetDetailedValue(
                _detailedTavernRollsValue,
                data?.TavernRolls ?? 0
            );
            SetDetailedValue(
                _detailedFreeRollsValue,
                data?.FreeRollsGained ?? 0
            );
            SetDetailedValue(
                _detailedCardsBoughtValue,
                data?.CardsBought ?? 0
            );
            SetDetailedValue(
                _detailedMinionsBoughtValue,
                data?.MinionsBought ?? 0
            );
            SetDetailedValue(
                _detailedSpellsBoughtValue,
                data?.SpellsBought ?? 0
            );
            SetDetailedValue(
                _detailedCardsPlayedValue,
                data?.CardsPlayed ?? 0
            );
            SetDetailedValue(
                _detailedMinionsPlayedValue,
                data?.MinionsPlayed ?? 0
            );
            SetDetailedValue(
                _detailedSpellsPlayedValue,
                data?.SpellsPlayed ?? 0
            );
            SetDetailedValue(
                _detailedCombatWinsValue,
                data?.CombatWins ?? 0
            );
            SetDetailedValue(
                _detailedCombatLossesValue,
                data?.CombatLosses ?? 0
            );
            SetDetailedValue(
                _detailedCombatDrawsValue,
                data?.CombatDraws ?? 0
            );
        }

        private static void SetDetailedValue(
            TextBlock target,
            int value)
        {
            if (target == null)
                return;

            target.Text = value.ToString(
                CultureInfo.InvariantCulture
            );
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

        private void UpdateFooter(FinalBoardSummaryData data)
        {
            string playerName = data?.PlayerName;
            string displayPlayerName =
                string.IsNullOrWhiteSpace(playerName)
                    ? string.Empty
                    : RemoveBattleTagCode(playerName);

            if (data?.IsDuosMatch == true)
            {
                string partnerName = data.PartnerName;
                string displayPartnerName =
                    string.IsNullOrWhiteSpace(partnerName)
                        ? "—"
                        : RemoveBattleTagCode(partnerName);

                _playerNameValue.Text =
                    "player : "
                    + (
                        string.IsNullOrWhiteSpace(displayPlayerName)
                            ? "—"
                            : displayPlayerName
                    )
                    + " - Partner : "
                    + displayPartnerName;
            }
            else
            {
                _playerNameValue.Text = displayPlayerName;
            }

            _pluginNameValue.Text = PluginDisplayName;
        }

        private static string RemoveBattleTagCode(string playerName)
        {
            int separatorIndex = playerName.LastIndexOf('#');

            if (
                separatorIndex <= 0
                || separatorIndex == playerName.Length - 1
            )
            {
                return playerName;
            }

            string code = playerName.Substring(separatorIndex + 1);
            return code.All(char.IsDigit)
                ? playerName.Substring(0, separatorIndex)
                : playerName;
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
            if (_panel == null || _displayHost == null)
                return;

            double overlayWidth =
                Core.OverlayCanvas?.ActualWidth ?? 0;
            double overlayHeight =
                Core.OverlayCanvas?.ActualHeight ?? 0;

            double scale =
                overlayHeight > 0
                    ? overlayHeight / ReferenceHeight
                    : 1.0;

            double centerX =
                overlayWidth > 0
                    ? overlayWidth / 2.0
                    : ReferenceWidth / 2.0;

            double left =
                centerX
                + (
                    PanelLeft
                    - ReferenceWidth / 2.0
                )
                * scale;

            double top = PanelTop * scale;

            if (_displayScale != null)
            {
                _displayScale.ScaleX = scale;
                _displayScale.ScaleY = scale;
            }

            Canvas.SetLeft(
                _displayHost,
                System.Math.Max(0, left)
            );
            Canvas.SetTop(
                _displayHost,
                System.Math.Max(0, top)
            );
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

                double renderHeight =
                    _panel.ActualHeight > 0
                        ? _panel.ActualHeight
                        : _panel.Height;

                if (
                    double.IsNaN(renderHeight)
                    || renderHeight <= 0
                )
                {
                    renderHeight = PanelHeight;
                }

                RenderTargetBitmap bitmap =
                    new RenderTargetBitmap(
                        (int)PanelWidth,
                        (int)Math.Ceiling(renderHeight),
                        96,
                        96,
                        PixelFormats.Pbgra32
                    );

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
                            renderHeight
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

        public bool AreScreenshotAssetsReady()
        {
            RefreshAnomalyVisual();

            bool heroPortraitExpected =
                _heroPortrait != null
                && !string.IsNullOrWhiteSpace(
                    _heroPortrait.CardId
                );
            bool heroPortraitReady =
                !heroPortraitExpected
                || _heroPortrait.CardAsset != null;

            bool heroPowerExpected = _heroPower != null;
            HeroPowerViewModel heroPowerViewModel =
                _heroPower?.DataContext
                    as HeroPowerViewModel;
            bool heroPowerReady =
                !heroPowerExpected
                || heroPowerViewModel?.CardPortrait?.Asset
                    != null;

            bool deityExpected = _deityMinion != null;
            BattlegroundsMinionViewModel deityViewModel =
                _deityMinion?.DataContext
                    as BattlegroundsMinionViewModel;
            bool deityReady =
                !deityExpected
                || deityViewModel?.CardPortrait?.Asset
                    != null;

            bool trinketsReady = true;

            foreach (Trinket trinket in _trinkets)
            {
                TrinketViewModel trinketViewModel =
                    trinket.DataContext as TrinketViewModel;

                if (
                    trinketViewModel?.CardPortrait?.Asset
                        == null
                )
                {
                    trinketsReady = false;
                    break;
                }
            }

            bool anomalyExpected =
                !string.IsNullOrWhiteSpace(
                    _anomalyCardId
                );
            bool anomalyReady =
                !anomalyExpected
                || !string.Equals(
                    _anomalyVisualMode,
                    "loading",
                    StringComparison.Ordinal
                );

            return heroPortraitReady
                && heroPowerReady
                && deityReady
                && trinketsReady
                && anomalyReady;
        }

        public void RefreshAnomalyVisual()
        {
            if (
                string.IsNullOrWhiteSpace(_anomalyCardId)
                || _anomalyVisualContainer == null
            )
            {
                _anomalyVisualMode = "none";
                return;
            }

            if (_anomalyImage?.CardAsset != null)
            {
                ShowAnomalyVisual(
                    _anomalyImage,
                    "full-image"
                );
                return;
            }

            if (_anomalyPortrait?.CardAsset != null)
            {
                ShowAnomalyVisual(
                    _anomalyPortrait,
                    "portrait"
                );
                return;
            }

            HeroPowerViewModel anomalyHeroPowerViewModel =
                _anomalyHeroPower?.DataContext
                    as HeroPowerViewModel;

            if (
                anomalyHeroPowerViewModel
                    ?.CardPortrait
                    ?.Asset
                    != null
            )
            {
                ShowAnomalyVisual(
                    _anomalyHeroPower,
                    "hero-power"
                );
                return;
            }

            ShowAnomalyVisual(null, "loading");
        }

        public string GetScreenshotAssetStatus()
        {
            bool heroPortraitExpected =
                _heroPortrait != null
                && !string.IsNullOrWhiteSpace(
                    _heroPortrait.CardId
                );
            HeroPowerViewModel heroPowerViewModel =
                _heroPower?.DataContext
                    as HeroPowerViewModel;
            HeroPowerViewModel anomalyHeroPowerViewModel =
                _anomalyHeroPower?.DataContext
                    as HeroPowerViewModel;
            BattlegroundsMinionViewModel deityViewModel =
                _deityMinion?.DataContext
                    as BattlegroundsMinionViewModel;
            int readyTrinkets = _trinkets.Count(
                trinket =>
                    (trinket.DataContext as TrinketViewModel)
                        ?.CardPortrait
                        ?.Asset
                    != null
            );

            return "hero="
                + (!heroPortraitExpected
                    ? "not-expected"
                    : _heroPortrait.CardAsset != null
                        ? "ready"
                        : "missing")
                + ",heroPower="
                + (_heroPower == null
                    ? "not-expected"
                    : heroPowerViewModel
                            ?.CardPortrait
                            ?.Asset
                        != null
                        ? "ready"
                        : "missing")
                + ",deity="
                + (_deityMinion == null
                    ? "not-expected"
                    : deityViewModel?.CardPortrait?.Asset
                        != null
                        ? "ready"
                        : "missing")
                + ",trinkets=" + readyTrinkets
                + "/" + _trinkets.Count
                + ",anomalyCard="
                + (_anomalyCardId ?? "none")
                + ",anomalyMode=" + _anomalyVisualMode
                + ",anomalyFull="
                + (_anomalyImage?.CardAsset != null
                    ? "ready"
                    : "missing")
                + ",anomalyPortrait="
                + (_anomalyPortrait?.CardAsset != null
                    ? "ready"
                    : "missing")
                + ",anomalyHeroPower="
                + (anomalyHeroPowerViewModel
                        ?.CardPortrait
                        ?.Asset
                    != null
                    ? "ready"
                    : "missing")
                + ",partnerBoard="
                + (_partnerBoard?.Children.Count ?? 0);
        }

        private void ShowAnomalyVisual(
            UIElement visual,
            string mode)
        {
            foreach (
                UIElement child
                in _anomalyVisualContainer.Children
            )
            {
                child.Visibility =
                    ReferenceEquals(child, visual)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }

            _anomalyVisualMode = mode;
        }

        private void UpdateHeroPortrait(FinalBoardSummaryData data)
        {
            Hearthstone_Deck_Tracker.Hearthstone.Card heroCard =
                data?.HeroCard;
            string heroCardId = heroCard?.Id ?? string.Empty;

            if (
                !string.Equals(
                    _heroPortrait.CardId,
                    heroCardId,
                    StringComparison.Ordinal
                )
            )
            {
                if (heroCard == null)
                {
                    _heroPortrait.CardAsset = null;
                    _heroPortrait.CardId = string.Empty;
                }
                else
                {
                    _heroPortrait.SetCardIdFromCard(
                        heroCard,
                        CardAssetType.Hero
                    );
                }
            }
        }

        private void UpdateHeroPower(FinalBoardSummaryData data)
        {
            Entity heroPowerEntity = data?.HeroPowerEntity;
            int entityId = heroPowerEntity?.Id ?? 0;
            string cardId =
                heroPowerEntity?.Info?.LatestCardId;

            if (string.IsNullOrWhiteSpace(cardId))
                cardId = heroPowerEntity?.CardId;

            cardId = cardId ?? string.Empty;

            if (
                entityId == _heroPowerEntityId
                && string.Equals(
                    cardId,
                    _heroPowerCardId,
                    StringComparison.Ordinal
                )
            )
            {
                return;
            }

            _heroPowerEntityId = entityId;
            _heroPowerCardId = cardId;
            _heroPowerContainer.Child = null;
            _heroPower = null;

            if (heroPowerEntity == null)
                return;

            _heroPower = new HeroPower(heroPowerEntity)
            {
                Width = 120,
                Height = 120,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            _heroPowerContainer.Child = _heroPower;
        }

        private void UpdateDeity(
            FinalBoardSummaryData data)
        {
            DeitySnapshot deity = data?.Deity;
            string cardId = deity?.Card?.Id ?? string.Empty;

            if (deity == null || deity.Card == null)
            {
                _deityContainer.Visibility =
                    Visibility.Collapsed;
                _deityContainer.Child = null;
                _deityMinion = null;
                _deityCardId = null;
                _deityAttack = 0;
                _deityHealth = 0;
                _deityIsGolden = false;
                return;
            }

            _deityContainer.Visibility = Visibility.Visible;

            if (
                _deityMinion != null
                && string.Equals(
                    _deityCardId,
                    cardId,
                    StringComparison.Ordinal
                )
                && _deityAttack == deity.Attack
                && _deityHealth == deity.Health
                && _deityIsGolden == deity.IsGolden
            )
            {
                return;
            }

            _deityCardId = cardId;
            _deityAttack = deity.Attack;
            _deityHealth = deity.Health;
            _deityIsGolden = deity.IsGolden;

            _deityMinion = new BattlegroundsMinion(
                new BattlegroundsMinionViewModel
                {
                    Card = deity.Card,
                    Attack = deity.Attack,
                    Health = deity.Health,
                    IsPremium = deity.IsGolden,
                    HighlightBuffedStats = false
                }
            )
            {
                Width = 100,
                Height = 100,
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                VerticalAlignment =
                    VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            _deityContainer.Child = _deityMinion;
        }

        private void UpdateTrinkets(
            FinalBoardSummaryData data)
        {
            _trinketPanel.Children.Clear();
            _trinkets.Clear();

            IReadOnlyList<Entity> trinketEntities =
                data?.TrinketEntities;

            if (trinketEntities == null)
                return;

            foreach (Entity trinketEntity in trinketEntities)
            {
                if (trinketEntity == null)
                    continue;

                Trinket trinket = new Trinket(trinketEntity)
                {
                    Width = 100,
                    Height = 100,
                    IsHitTestVisible = false
                };

                _trinkets.Add(trinket);
                _trinketPanel.Children.Add(trinket);
            }
        }

        private void UpdateAnomaly(
            FinalBoardSummaryData data)
        {
            Hearthstone_Deck_Tracker.Hearthstone.Card
                anomalyCard = data?.AnomalyCard;
            Entity anomalyHeroPowerEntity =
                data?.AnomalyHeroPowerEntity;
            string anomalyCardId =
                anomalyCard?.Id ?? string.Empty;
            int anomalyHeroPowerEntityId =
                anomalyHeroPowerEntity?.Id ?? 0;
            string anomalyHeroPowerCardId =
                anomalyHeroPowerEntity
                    ?.Info
                    ?.LatestCardId;

            if (
                string.IsNullOrWhiteSpace(
                    anomalyHeroPowerCardId
                )
            )
            {
                anomalyHeroPowerCardId =
                    anomalyHeroPowerEntity?.CardId;
            }

            anomalyHeroPowerCardId =
                anomalyHeroPowerCardId ?? string.Empty;

            if (anomalyCard == null)
            {
                _anomalySection.Visibility =
                    Visibility.Collapsed;
                _anomalyCardId = null;
                _anomalyHeroPowerEntityId = 0;
                _anomalyHeroPowerCardId = null;
                _anomalyHeroPower = null;
                _anomalyImage.SetCardIdFromCard(null);
                _anomalyPortrait.SetCardIdFromCard(null);
                _anomalyVisualContainer.Children.Clear();
                _anomalyVisualContainer.Children.Add(
                    _anomalyImage
                );
                _anomalyVisualContainer.Children.Add(
                    _anomalyPortrait
                );
                _anomalyVisualMode = "none";
                return;
            }

            _anomalySection.Visibility =
                Visibility.Visible;

            if (
                !string.Equals(
                    _anomalyCardId,
                    anomalyCardId,
                    StringComparison.Ordinal
                )
            )
            {
                _anomalyCardId = anomalyCardId;
                _anomalyImage.SetCardIdFromCard(
                    anomalyCard,
                    CardAssetType.FullImage
                );
                _anomalyPortrait.SetCardIdFromCard(
                    anomalyCard,
                    CardAssetType.Portrait
                );
            }

            if (
                anomalyHeroPowerEntityId
                    != _anomalyHeroPowerEntityId
                || !string.Equals(
                    anomalyHeroPowerCardId,
                    _anomalyHeroPowerCardId,
                    StringComparison.Ordinal
                )
            )
            {
                _anomalyHeroPowerEntityId =
                    anomalyHeroPowerEntityId;
                _anomalyHeroPowerCardId =
                    anomalyHeroPowerCardId;

                if (_anomalyHeroPower != null)
                {
                    _anomalyVisualContainer.Children.Remove(
                        _anomalyHeroPower
                    );
                    _anomalyHeroPower = null;
                }

                if (anomalyHeroPowerEntity != null)
                {
                    _anomalyHeroPower = new HeroPower(
                        anomalyHeroPowerEntity
                    )
                    {
                        Width = 90,
                        Height = 90,
                        HorizontalAlignment =
                            HorizontalAlignment.Center,
                        VerticalAlignment =
                            VerticalAlignment.Center,
                        Visibility = Visibility.Collapsed,
                        IsHitTestVisible = false
                    };
                    _anomalyVisualContainer.Children.Add(
                        _anomalyHeroPower
                    );
                }
            }

            RefreshAnomalyVisual();
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

        private static Brush CreatePanelBackground()
        {
            try
            {
                BitmapImage image = new BitmapImage();

                image.BeginInit();
                image.UriSource = new Uri(
                    "pack://application:,,,/HDT-FinalStatsPlugin;component/Images/FinalBoardBackground.png",
                    UriKind.Absolute
                );
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                ImageBrush brush = new ImageBrush(image)
                {
                    Stretch = Stretch.Fill,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };

                brush.Freeze();
                return brush;
            }
            catch
            {
                return CreateFrozenBrush(
                    Color.FromArgb(250, 8, 9, 11)
                );
            }
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
        public string PlayerName { get; set; }
        public string HeroName { get; set; }
        public Hearthstone_Deck_Tracker.Hearthstone.Card
            HeroCard
        { get; set; }
        public Entity HeroPowerEntity { get; set; }
        public Entity AnomalyHeroPowerEntity { get; set; }
        public IReadOnlyList<Entity> TrinketEntities
        { get; set; }
        public Hearthstone_Deck_Tracker.Hearthstone.Card
            AnomalyCard
        { get; set; }
        public DeitySnapshot Deity { get; set; }
        public int Placement { get; set; }
        public int? MmrDelta { get; set; }
        public int Turn { get; set; }
        public int HighestCreatureAttack { get; set; }
        public int HighestCreatureHealth { get; set; }
        public TimeSpan Duration { get; set; }
        public FinalBoardDisplayMode DisplayMode { get; set; }
        public int GoldSpent { get; set; }
        public int TavernRolls { get; set; }
        public int FreeRollsGained { get; set; }
        public int CardsBought { get; set; }
        public int MinionsBought { get; set; }
        public int SpellsBought { get; set; }
        public int CardsPlayed { get; set; }
        public int MinionsPlayed { get; set; }
        public int SpellsPlayed { get; set; }
        public int CombatWins { get; set; }
        public int CombatLosses { get; set; }
        public int CombatDraws { get; set; }
        public bool IsDuosMatch { get; set; }
        public string PartnerName { get; set; }
        public IReadOnlyList<Entity> PartnerBoardEntities
        { get; set; }
    }
}