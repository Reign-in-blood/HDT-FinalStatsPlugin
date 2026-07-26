using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FinalStatsPlugin
{
    public sealed class Plugin : IPlugin
    {
        // ------------------------------------------------------------
        // Plugin information
        // ------------------------------------------------------------

        public string Name => "Battlegrounds Final Stats";

        public string Description =>
            "Tracks live Battlegrounds match statistics and keeps the final summary visible after the game.";

        public string ButtonText => "Show / hide";
        public string Author => "Benito";
        public Version Version => new Version(0, 1, 27);
        public MenuItem MenuItem => null;

        // ------------------------------------------------------------
        // Overlay settings
        // ------------------------------------------------------------

        private const double PanelWidth = 250;
        private const double PanelHeight = 750;
        private const double PanelRight = 15;
        private const double PanelBottom = 50;
        private const double ToggleButtonHeight = 30;
        private const double ToggleButtonGap = 6;
        private const double StatRowHeight = 23;
        private const double CategoryHeaderHeight = 20;

        private static readonly Brush PanelBrush =
            CreateFrozenBrush(Color.FromArgb(248, 24, 26, 30));

        private static readonly Brush BorderBrush =
            CreateFrozenBrush(Color.FromArgb(42, 255, 255, 255));

        private static readonly Brush DividerBrush =
            CreateFrozenBrush(Color.FromArgb(28, 255, 255, 255));

        private static readonly Brush TitleBrush =
            CreateFrozenBrush(Color.FromRgb(218, 184, 108));

        private static readonly Brush CategoryBrush =
            CreateFrozenBrush(Color.FromRgb(184, 157, 99));

        private static readonly Brush LabelBrush =
            CreateFrozenBrush(Color.FromRgb(178, 184, 191));

        private static readonly Brush ValueBrush =
            CreateFrozenBrush(Color.FromRgb(238, 241, 244));

        private static readonly Brush PositiveBrush =
            CreateFrozenBrush(Color.FromRgb(91, 203, 154));

        private static readonly Brush NegativeBrush =
            CreateFrozenBrush(Color.FromRgb(240, 123, 123));

        private static readonly Brush NeutralBrush =
            CreateFrozenBrush(Color.FromRgb(154, 161, 169));

        private static readonly Brush ToggleButtonBrush =
            CreateFrozenBrush(Color.FromRgb(30, 33, 38));

        private static readonly Brush ToggleButtonHoverBrush =
            CreateFrozenBrush(Color.FromRgb(46, 50, 56));

        private static readonly Brush ToggleButtonPressedBrush =
            CreateFrozenBrush(Color.FromRgb(18, 20, 24));

        // ------------------------------------------------------------
        // Overlay controls
        // ------------------------------------------------------------

        private Border _panel;
        private Border _toggleButton;
        private TextBlock _toggleButtonText;
        private TextBlock _matchDurationValue;
        private TextBlock _goldSpentValue;
        private TextBlock _cardsBoughtValue;
        private TextBlock _minionsBoughtValue;
        private TextBlock _spellsBoughtValue;
        private TextBlock _freeRollsObtainedValue;
        private TextBlock _tavernRollsValue;
        private TextBlock _cardsPlayedValue;
        private TextBlock _minionsPlayedValue;
        private TextBlock _playedSpellsValue;
        private TextBlock _battlecriesValue;
        private TextBlock _ralliesValue;
        private TextBlock _highestAttackValue;
        private TextBlock _highestHealthValue;
        private TextBlock _highestCreatureValue;
        private TextBlock _highestTurnValue;
        private TextBlock _heroDamageDealtValue;
        private TextBlock _maxHeroDamageDealtValue;
        private TextBlock _heroDamageTakenValue;
        private TextBlock _maxHeroDamageTakenValue;
        private TextBlock _combatWinsValue;
        private TextBlock _combatLossesValue;
        private TextBlock _combatDrawsValue;
        private TextBlock _tavernSpellBuffValue;
        private TextBlock _tavernMinionBuffValue;

        // ------------------------------------------------------------
        // Match state
        // ------------------------------------------------------------

        private bool _loaded;
        private bool _pluginVisible = true;
        private bool _trackingMatch;
        private bool _hasMatchData;
        private bool _gameEndObserved;
        private bool _showingFinalSummary;
        private bool _newGameEventPending;
        private bool? _previousCombatPhase;
        private readonly Stopwatch _matchStopwatch = new Stopwatch();
        private TimeSpan _finalMatchDuration = TimeSpan.Zero;
        private long _lastDisplayedMatchDurationSecond = -1;
        private bool _matchDurationStarted;

        private int _goldSpent;
        private int _cardsBought;
        private int _minionsBought;
        private int _spellsBought;
        private int _freeRollsObtained;
        private int _tavernRolls;
        private int _cardsPlayed;
        private int _minionsPlayed;
        private int _playedSpells;
        private int _playedSpellsFromPlayerTag;
        private int _playedSpellsFromHandLog;
        private int _playedSpellsAutomatic;
        private int _battlecries;
        private int _rallies;
        private int _highestAttack;
        private int _highestHealth;
        private int _highestCreatureAttack;
        private int _highestCreatureHealth;
        private long _highestCreatureTotal;
        private int _highestTurn;
        private int _heroDamageDealt;
        private int _maxHeroDamageDealt;
        private int _heroDamageTaken;
        private int _maxHeroDamageTaken;
        private int _combatWins;
        private int _combatLosses;
        private int _combatDraws;
        private int _highestTavernSpellAttack;
        private int _highestTavernSpellHealth;
        private int _highestTavernMinionAttack;
        private int _highestTavernMinionHealth;

        private int _processedPowerLogLines;
        private int _fallbackResourcesUsed;
        private int _previousResourcesUsed;
        private int _previousFreeRollPotential;
        private bool _freeRollPotentialInitialized;
        private int _previousFreeRefreshesAvailable;
        private int _previousResourcesUsedForRolls;
        private bool _rerollTrackingInitialized;
        private int _pendingTavernRollActions;
        private DateTime _lastTavernRollActionQueuedUtc = DateTime.MinValue;
        private DateTime _lastTavernRollActionCountedUtc = DateTime.MinValue;
        private bool _shopSnapshotInitialized;

        private bool _heroCombatDamageTracking;
        private int _currentCombatDamageDealt;
        private int _currentCombatDamageTaken;

        private readonly Dictionary<int, EntityState> _entityStates =
            new Dictionary<int, EntityState>();

        private readonly HashSet<int> _countedPlayedSpellEntityIds =
            new HashSet<int>();

        private readonly HashSet<int> _handPlayedSpellEntityIds =
            new HashSet<int>();

        private readonly HashSet<int> _previousShopEntityIds =
            new HashSet<int>();

        private readonly HashSet<int> _knownShopEntityIds =
            new HashSet<int>();

        private readonly HashSet<int> _knownShopMinionEntityIds =
            new HashSet<int>();

        private readonly HashSet<int> _knownShopSpellEntityIds =
            new HashSet<int>();

        private readonly HashSet<int> _countedBoughtEntityIds =
            new HashSet<int>();

        private static readonly Regex EntityIdRegex = new Regex(
            @"(?:SourceEntityId=|Entity=)(\d+)|\bid=(\d+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private static readonly Regex PowerBlockPlayerIdRegex = new Regex(
            @"\bplayer=(\d+)",
            RegexOptions.Compiled
                | RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
        );

        private static readonly Regex EffectCardIdRegex = new Regex(
            @"\bEffectCardId=([^\s]+)",
            RegexOptions.Compiled
                | RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
        );

        private static readonly Regex BlockCardIdRegex = new Regex(
            @"\bcardId=([A-Za-z0-9_]+)",
            RegexOptions.Compiled
                | RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
        );

        private static readonly bool EnableDiagnosticLog = true;

        // ------------------------------------------------------------
        // HDT lifecycle
        // ------------------------------------------------------------

        public void OnLoad()
        {
            _loaded = true;

            GameEvents.OnGameStart.Add(HandleGameStart);
            GameEvents.OnGameEnd.Add(HandleGameEnd);
            GameEvents.OnInMenu.Add(HandleInMenu);
            GameEvents.OnEntityWillTakeDamage.Add(
                HandleEntityWillTakeDamage
            );

            Core.OverlayCanvas.Dispatcher.Invoke(() =>
            {
                CreateOverlay();
                UpdateOverlayValues();
                UpdateOverlayVisibility();
                PositionOverlay();
            });
        }

        public void OnUnload()
        {
            _loaded = false;

            Core.OverlayCanvas.Dispatcher.Invoke(() =>
            {
                if (_panel != null)
                    Core.OverlayCanvas.Children.Remove(_panel);

                if (_toggleButton != null)
                {
                    OverlayExtensions.SetIsOverlayHitTestVisible(
                        _toggleButton,
                        false
                    );
                    Core.OverlayCanvas.Children.Remove(_toggleButton);
                }

                _panel = null;
                _toggleButton = null;
                _toggleButtonText = null;
                _matchDurationValue = null;
                _goldSpentValue = null;
                _cardsBoughtValue = null;
                _minionsBoughtValue = null;
                _spellsBoughtValue = null;
                _freeRollsObtainedValue = null;
                _tavernRollsValue = null;
                _cardsPlayedValue = null;
                _minionsPlayedValue = null;
                _playedSpellsValue = null;
                _battlecriesValue = null;
                _ralliesValue = null;
                _highestAttackValue = null;
                _highestHealthValue = null;
                _highestCreatureValue = null;
                _highestTurnValue = null;
                _heroDamageDealtValue = null;
                _maxHeroDamageDealtValue = null;
                _heroDamageTakenValue = null;
                _maxHeroDamageTakenValue = null;
                _combatWinsValue = null;
                _combatLossesValue = null;
                _combatDrawsValue = null;
                _tavernSpellBuffValue = null;
                _tavernMinionBuffValue = null;
            });
        }

        public void OnButtonPress()
        {
            // The final summary must remain visible in the menu.
            if (_showingFinalSummary)
                return;

            _pluginVisible = !_pluginVisible;

            Core.OverlayCanvas.Dispatcher.Invoke(() =>
            {
                CreateOverlay();
                UpdateOverlayVisibility();
                PositionOverlay();
            });
        }

        // HDT calls this approximately every 100 ms.
        public void OnUpdate()
        {
            if (!_loaded)
                return;

            try
            {
                bool activeBattlegroundsMatch =
                    Core.Game.IsRunning
                    && !Core.Game.IsInMenu
                    && Core.Game.IsBattlegroundsMatch;

                if (
                    activeBattlegroundsMatch
                    && !_trackingMatch
                    && (!_gameEndObserved || _newGameEventPending)
                )
                {
                    BeginMatch();
                }

                if (_trackingMatch && activeBattlegroundsMatch)
                {
                    TrackMatch();
                }
                else if (_trackingMatch && !activeBattlegroundsMatch)
                {
                    FinishMatch();
                }

                Core.OverlayCanvas.Dispatcher.Invoke(() =>
                {
                    CreateOverlay();
                    UpdateOverlayValues();
                    UpdateOverlayVisibility();
                    PositionOverlay();
                });
            }
            catch (Exception ex)
            {
                WriteDiagnostic("UPDATE ERROR | " + ex);
            }
        }

        private void HandleGameStart()
        {
            if (!_loaded)
                return;

            _newGameEventPending = true;
            _gameEndObserved = false;
            _showingFinalSummary = false;

            Core.OverlayCanvas.Dispatcher.Invoke(() =>
            {
                UpdateOverlayVisibility();
                PositionOverlay();
            });
        }

        private void HandleGameEnd()
        {
            if (!_loaded)
                return;

            try
            {
                if (_trackingMatch)
                {
                    ProcessPowerLog();
                    TrackRerollStatistics();
                    TrackHighestStats();
                    TrackHighestTurnAndTavernBonuses();
                    FinalizeHeroCombatDamage();
                    TrackGoldSpent();
                    TrackPlayedSpellGameTag();
                    UpdatePlayedCardTotal();
                }

                FinishMatch();
            }
            catch (Exception ex)
            {
                WriteDiagnostic("GAME END ERROR | " + ex);
            }
        }

        private void HandleInMenu()
        {
            if (!_loaded)
                return;

            if (_trackingMatch)
                FinishMatch();

            if (!_hasMatchData)
                return;

            // In the menu, always show the final result and remove the
            // in-game toggle button. _pluginVisible is intentionally kept
            // unchanged so the previous in-game preference returns when
            // the next match starts.
            _showingFinalSummary = true;

            Core.OverlayCanvas.Dispatcher.Invoke(() =>
            {
                CreateOverlay();
                UpdateOverlayValues();
                UpdateOverlayVisibility();
                PositionOverlay();
            });
        }

        // ------------------------------------------------------------
        // Match tracking
        // ------------------------------------------------------------

        private void BeginMatch()
        {
            ResetStatistics();

            _trackingMatch = true;
            _hasMatchData = true;
            _gameEndObserved = false;
            _newGameEventPending = false;
            _showingFinalSummary = false;
            _previousCombatPhase = null;

            WriteDiagnostic("MATCH START");
        }

        private void FinishMatch()
        {
            if (!_trackingMatch)
                return;

            FinalizeHeroCombatDamage();
            _matchStopwatch.Stop();
            _finalMatchDuration = _matchStopwatch.Elapsed;
            _lastDisplayedMatchDurationSecond = -1;

            _trackingMatch = false;
            _gameEndObserved = true;
            _previousCombatPhase = null;

            WriteDiagnostic(
                "MATCH END | gold=" + _goldSpent
                + " | freeRollsGained=" + _freeRollsObtained
                + " | tavernRolls=" + _tavernRolls
                + " | cardsBought=" + _cardsBought
                + " | minionsBought=" + _minionsBought
                + " | spellsBought=" + _spellsBought
                + " | played=" + _cardsPlayed
                + " | minionsPlayed=" + _minionsPlayed
                + " | playedSpells=" + _playedSpells
                + " | battlecries=" + _battlecries
                + " | rallies=" + _rallies
                + " | highestAtk=" + _highestAttack
                + " | highestHp=" + _highestHealth
                + " | strongest=" + _highestCreatureAttack
                + "/" + _highestCreatureHealth
                + " | turn=" + _highestTurn
                + " | damageDealt=" + _heroDamageDealt
                + " | maxDamageDealt=" + _maxHeroDamageDealt
                + " | damageTaken=" + _heroDamageTaken
                + " | maxDamageTaken=" + _maxHeroDamageTaken
                + " | combatWins=" + _combatWins
                + " | combatLosses=" + _combatLosses
                + " | combatDraws=" + _combatDraws
                + " | durationSeconds="
                + ((long)_finalMatchDuration.TotalSeconds).ToString(
                    CultureInfo.InvariantCulture
                )
                + " | durationStarted=" + _matchDurationStarted
                + " | spellBuff=" + _highestTavernSpellAttack
                + "/" + _highestTavernSpellHealth
                + " | tavernBuff=" + _highestTavernMinionAttack
                + "/" + _highestTavernMinionHealth
            );
        }

        private void ResetStatistics()
        {
            _matchStopwatch.Reset();
            _finalMatchDuration = TimeSpan.Zero;
            _lastDisplayedMatchDurationSecond = -1;
            _matchDurationStarted = false;

            _goldSpent = 0;
            _cardsBought = 0;
            _minionsBought = 0;
            _spellsBought = 0;
            _freeRollsObtained = 0;
            _tavernRolls = 0;
            _cardsPlayed = 0;
            _minionsPlayed = 0;
            _playedSpells = 0;
            _playedSpellsFromPlayerTag = 0;
            _playedSpellsFromHandLog = 0;
            _playedSpellsAutomatic = 0;
            _battlecries = 0;
            _rallies = 0;
            _highestAttack = 0;
            _highestHealth = 0;
            _highestCreatureAttack = 0;
            _highestCreatureHealth = 0;
            _highestCreatureTotal = 0;
            _highestTurn = 0;
            _heroDamageDealt = 0;
            _maxHeroDamageDealt = 0;
            _heroDamageTaken = 0;
            _maxHeroDamageTaken = 0;
            _combatWins = 0;
            _combatLosses = 0;
            _combatDraws = 0;
            _highestTavernSpellAttack = 0;
            _highestTavernSpellHealth = 0;
            _highestTavernMinionAttack = 0;
            _highestTavernMinionHealth = 0;

            _processedPowerLogLines = 0;
            _fallbackResourcesUsed = 0;
            _previousResourcesUsed = 0;
            _previousFreeRollPotential = 0;
            _freeRollPotentialInitialized = false;
            _previousFreeRefreshesAvailable = 0;
            _previousResourcesUsedForRolls = 0;
            _rerollTrackingInitialized = false;
            _pendingTavernRollActions = 0;
            _lastTavernRollActionQueuedUtc = DateTime.MinValue;
            _lastTavernRollActionCountedUtc = DateTime.MinValue;
            _shopSnapshotInitialized = false;

            ResetHeroCombatDamageSnapshot();

            _entityStates.Clear();
            _countedPlayedSpellEntityIds.Clear();
            _handPlayedSpellEntityIds.Clear();
            _previousShopEntityIds.Clear();
            _knownShopEntityIds.Clear();
            _knownShopMinionEntityIds.Clear();
            _knownShopSpellEntityIds.Clear();
            _countedBoughtEntityIds.Clear();
        }

        private void TrackMatch()
        {
            bool isCombatPhase = Core.Game.IsBattlegroundsCombatPhase;

            if (!_previousCombatPhase.HasValue)
            {
                _previousCombatPhase = isCombatPhase;

                if (isCombatPhase)
                    StartCombatTracking();
            }
            else if (_previousCombatPhase.Value != isCombatPhase)
            {
                if (isCombatPhase)
                    StartCombatTracking();
                else
                    StopCombatTracking();

                _previousCombatPhase = isCombatPhase;
            }

            TrackGoldSpent();
            ProcessPowerLog();
            TrackRerollStatistics();
            TrackPlayedSpellGameTag();
            TrackHighestStats();
            TrackHighestTurnAndTavernBonuses();
            TrackEntityTransitions(isCombatPhase);
            UpdatePlayedCardTotal();
        }

        // ------------------------------------------------------------
        // Gold spent
        // ------------------------------------------------------------

        private void TrackGoldSpent()
        {
            Entity playerEntity = Core.Game.PlayerEntity;

            if (playerEntity == null)
                return;

            int resourcesUsed = Math.Max(
                0,
                playerEntity.GetTag(GameTag.RESOURCES_USED)
            );

            if (resourcesUsed > _previousResourcesUsed)
            {
                int spentSinceLastCheck =
                    resourcesUsed - _previousResourcesUsed;

                _fallbackResourcesUsed += spentSinceLastCheck;
                _goldSpent = _fallbackResourcesUsed;

                WriteDiagnostic(
                    "GOLD SPENT | delta="
                    + spentSinceLastCheck
                    + " | resourcesUsed="
                    + resourcesUsed
                    + " | total="
                    + _goldSpent
                );
            }
            else if (resourcesUsed < _previousResourcesUsed)
            {
                // A sale can reduce RESOURCES_USED because it restores gold.
                // A new recruitment turn can also reset it. Neither event is
                // spending, so the lower value only becomes the new baseline.
                WriteDiagnostic(
                    "GOLD BASELINE DECREASE | previous="
                    + _previousResourcesUsed
                    + " | current="
                    + resourcesUsed
                    + " | ignored"
                );
            }

            _previousResourcesUsed = resourcesUsed;
        }


        // ------------------------------------------------------------
        // Tavern refreshes
        // ------------------------------------------------------------

        private void TrackRerollStatistics()
        {
            if (Core.Game.IsBattlegroundsCombatPhase)
                return;

            int previousTotalRolls = _tavernRolls;
            int previousFreeRollsObtained = _freeRollsObtained;

            int freeRefreshesAvailable =
                GetCurrentFreeRefreshCount();

            // HDT exposes the number of free Tavern refreshes currently
            // available. An increase means new free refreshes were granted.
            if (!_freeRollPotentialInitialized)
            {
                _freeRollPotentialInitialized = true;
                _previousFreeRollPotential = freeRefreshesAvailable;
                _freeRollsObtained += freeRefreshesAvailable;
            }
            else
            {
                if (
                    freeRefreshesAvailable
                    > _previousFreeRollPotential
                )
                {
                    _freeRollsObtained +=
                        freeRefreshesAvailable
                        - _previousFreeRollPotential;
                }

                _previousFreeRollPotential =
                    freeRefreshesAvailable;
            }

            int resourcesUsed = GetCurrentResourcesUsed();
            HashSet<int> currentShopIds =
                CollectCurrentShopEntityIds();
            TryStartMatchDurationFromShop(currentShopIds);

            if (!_rerollTrackingInitialized)
            {
                _rerollTrackingInitialized = true;
                _previousFreeRefreshesAvailable =
                    freeRefreshesAvailable;
                _previousResourcesUsedForRolls = resourcesUsed;
                ReplaceShopSnapshot(currentShopIds);
            }
            else
            {
                int freeRefreshesConsumed = Math.Max(
                    0,
                    _previousFreeRefreshesAvailable
                    - freeRefreshesAvailable
                );

                int resourcesSpentSinceLastCheck =
                    resourcesUsed >= _previousResourcesUsedForRolls
                        ? resourcesUsed
                            - _previousResourcesUsedForRolls
                        : resourcesUsed;

                bool shopWasRefreshed =
                    HasShopBeenRefreshed(currentShopIds);

                DateTime now = DateTime.UtcNow;
                bool recentlyCountedFromAction =
                    _lastTavernRollActionCountedUtc
                        != DateTime.MinValue
                    && (
                        now - _lastTavernRollActionCountedUtc
                    ).TotalMilliseconds < 1500;

                if (_pendingTavernRollActions > 0)
                {
                    // Hearthstone writes several POWER blocks for one click.
                    // The pending value is deliberately treated as a boolean:
                    // one update containing three blocks is still one roll.
                    _tavernRolls++;
                    _pendingTavernRollActions = 0;
                    _lastTavernRollActionCountedUtc = now;

                    WriteDiagnostic(
                        "TAVERN ROLL FROM ACTION CARD | count=1"
                    );
                }
                else if (
                    recentlyCountedFromAction
                    && (shopWasRefreshed
                        || freeRefreshesConsumed > 0)
                )
                {
                    // The shop replacement and the free-refresh decrease can
                    // arrive one update after the POWER block. They confirm the
                    // same roll and must not add another count.
                    WriteDiagnostic(
                        "TAVERN ROLL SECONDARY SIGNAL IGNORED"
                        + " | shop=" + shopWasRefreshed
                        + " | freeConsumed="
                        + freeRefreshesConsumed
                    );
                }
                else if (
                    shopWasRefreshed
                    && (freeRefreshesConsumed > 0
                        || resourcesSpentSinceLastCheck > 0)
                )
                {
                    _tavernRolls++;

                    WriteDiagnostic(
                        "TAVERN ROLL FROM SHOP CHANGE"
                        + " | freeConsumed="
                        + freeRefreshesConsumed
                        + " | resourcesDelta="
                        + resourcesSpentSinceLastCheck
                    );
                }
                else if (freeRefreshesConsumed > 0)
                {
                    // Fallback when HDT misses the very short shop replacement.
                    _tavernRolls += freeRefreshesConsumed;

                    WriteDiagnostic(
                        "TAVERN ROLL FROM FREE REFRESH DECREASE | count="
                        + freeRefreshesConsumed
                    );
                }

                _previousFreeRefreshesAvailable =
                    freeRefreshesAvailable;
                _previousResourcesUsedForRolls = resourcesUsed;
                ReplaceShopSnapshot(currentShopIds);
            }

            if (
                previousTotalRolls != _tavernRolls
                || previousFreeRollsObtained
                    != _freeRollsObtained
            )
            {
                WriteDiagnostic(
                    "REROLLS | freeGained="
                    + _freeRollsObtained
                    + " | totalUsed=" + _tavernRolls
                    + " | freeAvailable="
                    + freeRefreshesAvailable
                    + " | pendingAction="
                    + (_pendingTavernRollActions > 0)
                );
            }
        }

        private static int GetCurrentResourcesUsed()
        {
            Entity playerEntity = Core.Game.PlayerEntity;

            if (playerEntity == null)
                return 0;

            return Math.Max(
                0,
                playerEntity.GetTag(GameTag.RESOURCES_USED)
            );
        }

        private HashSet<int> CollectCurrentShopEntityIds()
        {
            HashSet<int> ids = new HashSet<int>();
            int playerId = Core.Game.Player.Id;

            foreach (Entity entity in Core.Game.Entities.Values)
            {
                if (
                    entity == null
                    || entity.Id <= 0
                    || !entity.IsInPlay
                    || entity.IsControlledBy(playerId)
                    || (!IsMinionCard(entity)
                        && !IsTavernSpell(entity))
                )
                {
                    continue;
                }

                ids.Add(entity.Id);
                _knownShopEntityIds.Add(entity.Id);

                if (IsMinionCard(entity))
                    _knownShopMinionEntityIds.Add(entity.Id);

                if (IsTavernSpell(entity))
                    _knownShopSpellEntityIds.Add(entity.Id);
            }

            return ids;
        }

        private void TryStartMatchDurationFromShop(
            HashSet<int> currentShopIds)
        {
            if (
                _matchDurationStarted
                || currentShopIds == null
                || currentShopIds.Count == 0
            )
            {
                return;
            }

            _matchStopwatch.Restart();
            _matchDurationStarted = true;
            _lastDisplayedMatchDurationSecond = -1;

            WriteDiagnostic(
                "MATCH TIMER START"
                + " | source=first-shop"
                + " | shopEntities=" + currentShopIds.Count
            );
        }

        private bool HasShopBeenRefreshed(
            HashSet<int> currentShopIds)
        {
            if (
                !_shopSnapshotInitialized
                || _previousShopEntityIds.Count == 0
                || currentShopIds.Count == 0
            )
            {
                return false;
            }

            int retained = 0;

            foreach (int id in currentShopIds)
            {
                if (_previousShopEntityIds.Contains(id))
                    retained++;
            }

            int removed =
                _previousShopEntityIds.Count - retained;
            int added = currentShopIds.Count - retained;

            // Purchases remove one card and effects often add one card. A
            // refresh replaces the majority of the visible Tavern at once.
            bool majorityReplaced =
                removed >= 2
                && added >= 2
                && retained * 2
                    < Math.Max(
                        _previousShopEntityIds.Count,
                        currentShopIds.Count
                    );

            if (majorityReplaced)
            {
                WriteDiagnostic(
                    "SHOP REPLACED | previous="
                    + _previousShopEntityIds.Count
                    + " | current=" + currentShopIds.Count
                    + " | retained=" + retained
                    + " | removed=" + removed
                    + " | added=" + added
                );
            }

            return majorityReplaced;
        }

        private void ReplaceShopSnapshot(
            HashSet<int> currentShopIds)
        {
            _previousShopEntityIds.Clear();

            foreach (int id in currentShopIds)
                _previousShopEntityIds.Add(id);

            _shopSnapshotInitialized =
                currentShopIds.Count > 0;
        }

        private static int GetHighestPlayerTagValue(
            string tagName,
            bool excludeSetAside)
        {
            int playerId = Core.Game.Player.Id;
            int highestValue = 0;

            foreach (Entity entity in Core.Game.Entities.Values)
            {
                if (entity == null)
                    continue;

                if (
                    excludeSetAside
                    && entity.GetTag(GameTag.ZONE)
                        == (int)Zone.SETASIDE
                )
                {
                    continue;
                }

                bool belongsToPlayer =
                    entity == Core.Game.PlayerEntity
                    || entity.IsControlledBy(playerId);

                if (!belongsToPlayer)
                    continue;

                highestValue = Math.Max(
                    highestValue,
                    GetTagValueByName(entity, tagName)
                );
            }

            return Math.Max(0, highestValue);
        }

        private static int GetCurrentFreeRefreshCount()
        {
            try
            {
                if (Core.Game.CounterManager != null)
                {
                    foreach (
                        var counter
                        in Core.Game.CounterManager.PlayerCounters
                    )
                    {
                        if (
                            counter == null
                            || !string.Equals(
                                counter.GetType().Name,
                                "FreeRefreshCounter",
                                StringComparison.Ordinal
                            )
                        )
                        {
                            continue;
                        }

                        if (
                            int.TryParse(
                                counter.ValueToShow(),
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int counterValue
                            )
                        )
                        {
                            return Math.Max(0, counterValue);
                        }
                    }
                }
            }
            catch
            {
                // Fall back to the raw entity tag below.
            }

            return GetHighestPlayerTagValue(
                "BACON_FREE_REFRESH_COUNT",
                true
            );
        }

        // ------------------------------------------------------------
        // Tavern spells played - direct player counter
        // ------------------------------------------------------------

        private void TrackPlayedSpellGameTag()
        {
            Entity playerEntity = Core.Game.PlayerEntity;

            if (playerEntity == null)
                return;

            int gameTagCount = Math.Max(
                0,
                playerEntity.GetTag(
                    GameTag.NUM_SPELLS_PLAYED_THIS_GAME
                )
            );

            if (gameTagCount <= _playedSpellsFromPlayerTag)
                return;

            _playedSpellsFromPlayerTag = gameTagCount;
            UpdatePlayedSpellTotal();

            WriteDiagnostic(
                "SPELL TOTAL FROM PLAYER TAG | value="
                + gameTagCount
            );
        }

        private void UpdatePlayedSpellTotal()
        {
            _playedSpells = Math.Max(
                _playedSpellsFromPlayerTag,
                _playedSpellsFromHandLog
                    + _playedSpellsAutomatic
            );

            UpdatePlayedCardTotal();
        }

        private void UpdatePlayedCardTotal()
        {
            _cardsPlayed = _minionsPlayed + _playedSpells;
        }

        // ------------------------------------------------------------
        // Highest minion stats
        // ------------------------------------------------------------

        private void TrackHighestStats()
        {
            int playerId = Core.Game.Player.Id;

            foreach (Entity entity in Core.Game.Entities.Values)
            {
                if (
                    entity == null
                    || !entity.IsMinion
                    || !entity.IsInPlay
                    || !entity.IsControlledBy(playerId)
                )
                {
                    continue;
                }

                int attack = Math.Max(0, entity.Attack);
                int health = Math.Max(
                    0,
                    entity.GetTag(GameTag.HEALTH)
                );

                _highestAttack = Math.Max(
                    _highestAttack,
                    attack
                );

                _highestHealth = Math.Max(
                    _highestHealth,
                    health
                );

                long total = (long)attack + health;

                if (
                    total > _highestCreatureTotal
                    || (
                        total == _highestCreatureTotal
                        && attack > _highestCreatureAttack
                    )
                    || (
                        total == _highestCreatureTotal
                        && attack == _highestCreatureAttack
                        && health > _highestCreatureHealth
                    )
                )
                {
                    _highestCreatureTotal = total;
                    _highestCreatureAttack = attack;
                    _highestCreatureHealth = health;
                }
            }
        }

        // ------------------------------------------------------------
        // Turn and tavern bonuses
        // ------------------------------------------------------------

        private void TrackHighestTurnAndTavernBonuses()
        {
            _highestTurn = Math.Max(
                _highestTurn,
                Math.Max(0, Core.Game.GetTurnNumber())
            );

            Entity playerEntity = Core.Game.PlayerEntity;

            if (playerEntity == null)
                return;

            int tavernSpellAttack = Math.Max(
                0,
                GetTagValueByName(
                    playerEntity,
                    "TAVERN_SPELL_ATTACK_INCREASE"
                )
            );

            int tavernSpellHealth = Math.Max(
                0,
                GetTagValueByName(
                    playerEntity,
                    "TAVERN_SPELL_HEALTH_INCREASE"
                )
            );

            bool tavernSpellBuffChanged =
                tavernSpellAttack > _highestTavernSpellAttack
                || tavernSpellHealth > _highestTavernSpellHealth;

            _highestTavernSpellAttack = Math.Max(
                _highestTavernSpellAttack,
                tavernSpellAttack
            );

            _highestTavernSpellHealth = Math.Max(
                _highestTavernSpellHealth,
                tavernSpellHealth
            );

            if (tavernSpellBuffChanged)
            {
                WriteDiagnostic(
                    "TAVERN SPELL BUFF | atk="
                    + _highestTavernSpellAttack
                    + " | hp="
                    + _highestTavernSpellHealth
                );
            }

            TrackOfficialTavernMinionBuffCounter();
        }

        private void TrackOfficialTavernMinionBuffCounter()
        {
            int attack = 0;
            int health = 0;

            if (!TryGetPlayerStatsCounterValues(
                    "RandomTavernMinionBuffCounter",
                    out attack,
                    out health
                ))
            {
                return;
            }

            bool changed =
                attack > _highestTavernMinionAttack
                || health > _highestTavernMinionHealth;

            _highestTavernMinionAttack = Math.Max(
                _highestTavernMinionAttack,
                attack
            );

            _highestTavernMinionHealth = Math.Max(
                _highestTavernMinionHealth,
                health
            );

            if (changed)
            {
                WriteDiagnostic(
                    "TAVERN MINION BUFF FROM HDT COUNTER"
                    + " | current=" + attack + "/" + health
                    + " | max="
                    + _highestTavernMinionAttack
                    + "/"
                    + _highestTavernMinionHealth
                );
            }
        }

        private static bool TryGetPlayerStatsCounterValues(
            string counterTypeName,
            out int attack,
            out int health)
        {
            attack = 0;
            health = 0;

            try
            {
                if (Core.Game.CounterManager == null)
                    return false;

                foreach (
                    var counter
                    in Core.Game.CounterManager.PlayerCounters
                )
                {
                    if (
                        counter == null
                        || !string.Equals(
                            counter.GetType().Name,
                            counterTypeName,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        continue;
                    }

                    string displayedValue =
                        counter.ValueToShow() ?? string.Empty;

                    MatchCollection numbers = Regex.Matches(
                        displayedValue,
                        @"\d+"
                    );

                    if (numbers.Count < 2)
                        return false;

                    if (
                        !int.TryParse(
                            numbers[0].Value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out attack
                        )
                        || !int.TryParse(
                            numbers[1].Value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out health
                        )
                    )
                    {
                        attack = 0;
                        health = 0;
                        return false;
                    }

                    attack = Math.Max(0, attack);
                    health = Math.Max(0, health);
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteDiagnostic(
                    "TAVERN MINION BUFF COUNTER ERROR | "
                    + ex.Message
                );
            }

            return false;
        }

        private static int GetTagValueByName(
            Entity entity,
            string tagName)
        {
            if (
                entity == null
                || string.IsNullOrEmpty(tagName)
                || !Enum.TryParse(
                    tagName,
                    false,
                    out GameTag tag
                )
            )
            {
                return 0;
            }

            return entity.GetTag(tag);
        }

        // ------------------------------------------------------------
        // Tavern purchases
        // ------------------------------------------------------------

        private void TryCountTavernPurchaseFromPowerLog(string line)
        {
            if (
                string.IsNullOrEmpty(line)
                || line.IndexOf(
                    "tag=ZONE",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
                || line.IndexOf(
                    "value=HAND",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
            )
            {
                return;
            }

            int entityId = TryExtractEntityId(line);

            if (
                entityId <= 0
                || !_knownShopEntityIds.Contains(entityId)
                || !Core.Game.Entities.TryGetValue(
                    entityId,
                    out Entity entity
                )
            )
            {
                return;
            }

            CountBoughtEntity(
                entity,
                "POWER LOG PLAY TO HAND",
                line
            );
        }

        private void CountBoughtEntity(
            Entity entity,
            string source,
            string powerLogLine)
        {
            if (
                entity == null
                || entity.Id <= 0
                || !_knownShopEntityIds.Contains(entity.Id)
                || _countedBoughtEntityIds.Contains(entity.Id)
            )
            {
                return;
            }

            bool isMinion =
                _knownShopMinionEntityIds.Contains(entity.Id)
                || IsMinionCard(entity);

            bool isSpell =
                _knownShopSpellEntityIds.Contains(entity.Id)
                || IsTavernSpell(entity);

            if (!isMinion && !isSpell)
                return;

            _countedBoughtEntityIds.Add(entity.Id);
            _cardsBought++;

            if (isMinion)
                _minionsBought++;

            if (isSpell)
                _spellsBought++;

            WriteDiagnostic(
                "CARD BOUGHT | source=" + source
                + " | id=" + entity.Id
                + " | card=" + GetBestCardId(entity)
                + " | minion=" + isMinion
                + " | spell=" + isSpell
                + " | cards=" + _cardsBought
                + " | minions=" + _minionsBought
                + " | spells=" + _spellsBought
                + (
                    string.IsNullOrEmpty(powerLogLine)
                        ? string.Empty
                        : " | line="
                            + TrimLogLine(powerLogLine)
                )
            );
        }

        // ------------------------------------------------------------
        // Spell entity transitions
        // ------------------------------------------------------------

        private void TrackEntityTransitions(bool isCombatPhase)
        {
            int playerId = Core.Game.Player.Id;
            Dictionary<int, EntityState> currentStates =
                new Dictionary<int, EntityState>();

            foreach (Entity entity in Core.Game.Entities.Values)
            {
                if (entity == null || entity.Id <= 0)
                    continue;

                EntityState current = EntityState.FromEntity(entity);
                currentStates[entity.Id] = current;

                bool hadPreviousState =
                    _entityStates.TryGetValue(
                        entity.Id,
                        out EntityState previous
                    );

                if (
                    !isCombatPhase
                    && current.ControllerId == playerId
                    && current.Zone == Zone.HAND
                    && _knownShopEntityIds.Contains(entity.Id)
                )
                {
                    CountBoughtEntity(
                        entity,
                        hadPreviousState
                            ? "ENTITY TRANSITION "
                                + previous.Zone
                                + " TO HAND"
                            : "KNOWN SHOP ENTITY IN HAND",
                        null
                    );
                }

                if (
                    current.ControllerId == playerId
                    && IsTavernSpell(entity)
                )
                {
                    bool leftPlayerHand =
                        hadPreviousState
                        && previous.Zone == Zone.HAND
                        && current.Zone != Zone.HAND;

                    bool appearedAsCombatSpell =
                        isCombatPhase
                        && !hadPreviousState
                        && (
                            current.Zone == Zone.PLAY
                            || current.Zone == Zone.GRAVEYARD
                        );

                    if (leftPlayerHand || appearedAsCombatSpell)
                    {
                        CountPlayedSpellEntity(
                            entity,
                            !leftPlayerHand,
                            leftPlayerHand
                                ? "ENTITY LEFT HAND"
                                : "COMBAT SPELL ENTITY",
                            null
                        );
                    }
                }
            }

            _entityStates.Clear();

            foreach (KeyValuePair<int, EntityState> pair in currentStates)
                _entityStates[pair.Key] = pair.Value;
        }

        // ------------------------------------------------------------
        // Battlegrounds phase changes
        // ------------------------------------------------------------

        private void StartCombatTracking()
        {
            _shopSnapshotInitialized = false;
            _previousShopEntityIds.Clear();
            _rerollTrackingInitialized = false;
            StartHeroCombatDamageTracking();
            WriteDiagnostic("COMBAT START");
        }

        private void StopCombatTracking()
        {
            FinalizeHeroCombatDamage();
            _shopSnapshotInitialized = false;
            _previousShopEntityIds.Clear();
            _rerollTrackingInitialized = false;

            // Start the new recruitment phase from a clean resource baseline.
            // This also prevents a missed intermediate zero from hiding the
            // first purchase of the new turn.
            _previousResourcesUsed = 0;

            WriteDiagnostic("COMBAT END");
        }

        // ------------------------------------------------------------
        // Battlegrounds hero combat damage
        // ------------------------------------------------------------

        private void StartHeroCombatDamageTracking()
        {
            // OnUpdate can notice the combat phase shortly after the first
            // PREDAMAGE event. Do not clear values that were already received.
            if (_heroCombatDamageTracking)
                return;

            _heroCombatDamageTracking = true;
            _currentCombatDamageDealt = 0;
            _currentCombatDamageTaken = 0;

            WriteDiagnostic("HERO DAMAGE COMBAT START | source=PREDAMAGE");
        }

        private void HandleEntityWillTakeDamage(PredamageInfo info)
        {
            if (
                !_loaded
                || !_trackingMatch
                || info == null
                || info.Entity == null
                || info.Value <= 0
                || !Core.Game.IsBattlegroundsMatch
                || !Core.Game.IsBattlegroundsCombatPhase
                || !info.Entity.IsHero
            )
            {
                return;
            }

            if (!_heroCombatDamageTracking)
                StartHeroCombatDamageTracking();

            Entity target = info.Entity;
            int damage = info.Value;

            // Battlegrounds can expose more than one hero-shaped entity for a
            // player (leaderboard/combat representations), and PREDAMAGE may be
            // reported more than once for the same final impact. Only accept the
            // exact active hero entities referenced by the two player entities.
            int playerHeroEntityId = Core.Game.PlayerEntity != null
                ? Core.Game.PlayerEntity.GetTag(GameTag.HERO_ENTITY)
                : 0;
            int opponentHeroEntityId = Core.Game.OpponentEntity != null
                ? Core.Game.OpponentEntity.GetTag(GameTag.HERO_ENTITY)
                : 0;

            if (target.Id == opponentHeroEntityId && opponentHeroEntityId > 0)
            {
                // A Battlegrounds hero can only receive one final combat hit.
                // Keeping the maximum makes duplicate PREDAMAGE notifications
                // harmless instead of counting the same hit twice.
                _currentCombatDamageDealt = Math.Max(
                    _currentCombatDamageDealt,
                    damage
                );

                WriteDiagnostic(
                    "HERO PREDAMAGE DEALT"
                    + " | target=" + target.Id
                    + " | activeHero=" + opponentHeroEntityId
                    + " | value=" + damage
                    + " | combatMax=" + _currentCombatDamageDealt
                );
            }
            else if (target.Id == playerHeroEntityId && playerHeroEntityId > 0)
            {
                _currentCombatDamageTaken = Math.Max(
                    _currentCombatDamageTaken,
                    damage
                );

                WriteDiagnostic(
                    "HERO PREDAMAGE TAKEN"
                    + " | target=" + target.Id
                    + " | activeHero=" + playerHeroEntityId
                    + " | value=" + damage
                    + " | combatMax=" + _currentCombatDamageTaken
                );
            }
            else
            {
                WriteDiagnostic(
                    "HERO PREDAMAGE IGNORED"
                    + " | target=" + target.Id
                    + " | value=" + damage
                    + " | playerHero=" + playerHeroEntityId
                    + " | opponentHero=" + opponentHeroEntityId
                );
            }
        }

        private void FinalizeHeroCombatDamage()
        {
            if (!_heroCombatDamageTracking)
                return;

            int damageDealt = _currentCombatDamageDealt;
            int damageTaken = _currentCombatDamageTaken;

            if (damageDealt > 0)
            {
                _heroDamageDealt += damageDealt;
                _maxHeroDamageDealt = Math.Max(
                    _maxHeroDamageDealt,
                    damageDealt
                );
            }

            if (damageTaken > 0)
            {
                _heroDamageTaken += damageTaken;
                _maxHeroDamageTaken = Math.Max(
                    _maxHeroDamageTaken,
                    damageTaken
                );
            }

            string result;

            if (damageDealt > 0 && damageTaken == 0)
            {
                _combatWins++;
                result = "win";
            }
            else if (damageTaken > 0 && damageDealt == 0)
            {
                _combatLosses++;
                result = "loss";
            }
            else if (damageDealt == 0 && damageTaken == 0)
            {
                _combatDraws++;
                result = "draw";
            }
            else
            {
                // Both heroes taking damage in one Battlegrounds combat is
                // not a valid result. Keep the damage totals, but do not
                // invent a win, loss, or draw.
                result = "ambiguous";
            }

            WriteDiagnostic(
                "HERO DAMAGE COMBAT END"
                + " | source=PREDAMAGE"
                + " | dealt=" + damageDealt
                + " | taken=" + damageTaken
                + " | result=" + result
                + " | totalDealt=" + _heroDamageDealt
                + " | totalTaken=" + _heroDamageTaken
                + " | maxDealt=" + _maxHeroDamageDealt
                + " | maxTaken=" + _maxHeroDamageTaken
                + " | wins=" + _combatWins
                + " | losses=" + _combatLosses
                + " | draws=" + _combatDraws
            );

            ResetHeroCombatDamageSnapshot();
        }

        private void ResetHeroCombatDamageSnapshot()
        {
            _heroCombatDamageTracking = false;
            _currentCombatDamageDealt = 0;
            _currentCombatDamageTaken = 0;
        }

        // ------------------------------------------------------------
        // Power-log counters
        // ------------------------------------------------------------

        private void ProcessPowerLog()
        {
            var powerLog = Core.Game.PowerLog;

            if (powerLog == null)
                return;

            if (_processedPowerLogLines > powerLog.Count)
            {
                WriteDiagnostic(
                    "POWER LOG RESET | previous="
                    + _processedPowerLogLines
                    + " | current=" + powerLog.Count
                );

                _processedPowerLogLines = 0;
            }

            for (
                int index = _processedPowerLogLines;
                index < powerLog.Count;
                index++
            )
            {
                ProcessPowerLogLine(powerLog[index]);
            }

            _processedPowerLogLines = powerLog.Count;
        }

        private void ProcessPowerLogLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            TryQueueTavernRefreshAction(line);
            TryCountTavernPurchaseFromPowerLog(line);

            Entity playedEntity = null;
            bool isPlayerPlay =
                line.IndexOf(
                    "BLOCK_START BlockType=PLAY",
                    StringComparison.Ordinal
                ) >= 0
                && line.IndexOf(
                    "zone=HAND",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
                && TryGetPlayerSourceEntity(line, out playedEntity);

            if (isPlayerPlay)
            {
                bool isMinion = IsMinionCard(playedEntity);
                bool isSpell = IsTavernSpell(playedEntity);

                if (isMinion)
                    _minionsPlayed++;

                if (isSpell)
                {
                    CountPlayedSpellEntity(
                        playedEntity,
                        false,
                        "PLAY FROM HAND",
                        line
                    );
                }

                if (
                    isMinion
                    && EntityHasKeyword(playedEntity, "BATTLECRY")
                )
                {
                    _battlecries++;
                    WriteDiagnostic(
                        "BATTLECRY FROM PLAY | id="
                        + playedEntity.Id
                        + " | card=" + playedEntity.CardId
                    );
                }

                UpdatePlayedCardTotal();

                WriteDiagnostic(
                    "CARD PLAYED | minion=" + isMinion
                    + " | spell=" + isSpell
                    + " | total=" + _cardsPlayed
                    + " | " + TrimLogLine(line)
                );
                return;
            }

            Entity attackingEntity = null;
            bool isPlayerAttack =
                line.IndexOf(
                    "BLOCK_START BlockType=ATTACK",
                    StringComparison.Ordinal
                ) >= 0
                && TryGetPlayerSourceEntity(
                    line,
                    out attackingEntity
                );

            if (
                isPlayerAttack
                && EntityHasKeyword(attackingEntity, "RALLY")
            )
            {
                _rallies++;

                WriteDiagnostic(
                    "RALLY FROM ATTACK | id="
                    + attackingEntity.Id
                    + " | card=" + attackingEntity.CardId
                );
                return;
            }

            TryCountAutomaticTavernSpell(line);
        }

        private void TryQueueTavernRefreshAction(string line)
        {
            if (
                line.IndexOf(
                    "BLOCK_START BlockType=POWER",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
                || !IsPlayerOwnedPowerBlock(line)
                || !TryGetSourceEntity(line, out Entity entity)
                || !IsTavernRefreshActionEntity(entity)
            )
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            if (
                _lastTavernRollActionQueuedUtc != DateTime.MinValue
                && (
                    now - _lastTavernRollActionQueuedUtc
                ).TotalMilliseconds < 400
            )
            {
                WriteDiagnostic(
                    "DUPLICATE TAVERN REFRESH BLOCK IGNORED"
                    + " | id=" + entity.Id
                );
                return;
            }

            _lastTavernRollActionQueuedUtc = now;
            _pendingTavernRollActions = 1;

            WriteDiagnostic(
                "TAVERN REFRESH ACTION DETECTED | id="
                + entity.Id
                + " | card=" + entity.CardId
                + " | name=" + entity.Card.Name
                + " | " + TrimLogLine(line)
            );
        }

        private static bool IsTavernRefreshActionEntity(
            Entity entity)
        {
            if (entity == null)
                return false;

            bool isActionCard =
                GetTagValueByName(
                    entity,
                    "BACON_ACTION_CARD"
                ) > 0
                || GetCardTagValueByName(
                    entity,
                    "BACON_ACTION_CARD"
                ) > 0;

            if (!isActionCard)
                return false;

            bool hasRefreshTag =
                GetTagValueByName(
                    entity,
                    "BACON_REFRESH_TOOLTIP"
                ) > 0
                || GetCardTagValueByName(
                    entity,
                    "BACON_REFRESH_TOOLTIP"
                ) > 0;

            if (hasRefreshTag)
                return true;

            string cardId =
                !string.IsNullOrEmpty(entity.Info.LatestCardId)
                    ? entity.Info.LatestCardId
                    : entity.CardId;

            string cardName = entity.Card.Name;

            return (
                    !string.IsNullOrEmpty(cardId)
                    && cardId.IndexOf(
                        "refresh",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                || (
                    !string.IsNullOrEmpty(cardName)
                    && cardName.IndexOf(
                        "refresh",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                );
        }

        private static int GetCardTagValueByName(
            Entity entity,
            string tagName)
        {
            if (
                entity == null
                || string.IsNullOrEmpty(tagName)
                || !Enum.TryParse(
                    tagName,
                    false,
                    out GameTag tag
                )
            )
            {
                return 0;
            }

            string cardId =
                !string.IsNullOrEmpty(entity.Info.LatestCardId)
                    ? entity.Info.LatestCardId
                    : entity.CardId;

            if (
                string.IsNullOrEmpty(cardId)
                || !HearthDb.Cards.All.TryGetValue(
                    cardId,
                    out HearthDb.Card card
                )
                || card == null
            )
            {
                return 0;
            }

            return card.Entity.GetTag(tag);
        }

        private void CountPlayedSpellEntity(
            Entity spellEntity,
            bool automatic,
            string source,
            string line)
        {
            if (
                spellEntity == null
                || spellEntity.Id <= 0
                || !IsTavernSpell(spellEntity)
            )
            {
                return;
            }

            if (automatic)
            {
                if (
                    _handPlayedSpellEntityIds.Contains(
                        spellEntity.Id
                    )
                    || !_countedPlayedSpellEntityIds.Add(
                        spellEntity.Id
                    )
                )
                {
                    return;
                }

                _playedSpellsAutomatic++;
            }
            else
            {
                if (
                    !_handPlayedSpellEntityIds.Add(
                        spellEntity.Id
                    )
                )
                {
                    return;
                }

                _countedPlayedSpellEntityIds.Add(
                    spellEntity.Id
                );
                _playedSpellsFromHandLog++;
            }

            UpdatePlayedSpellTotal();

            WriteDiagnostic(
                "SPELL PLAYED | automatic=" + automatic
                + " | source=" + source
                + " | id=" + spellEntity.Id
                + " | card=" + spellEntity.CardId
                + " | tag=" + _playedSpellsFromPlayerTag
                + " | hand=" + _playedSpellsFromHandLog
                + " | automaticTotal="
                + _playedSpellsAutomatic
                + (
                    string.IsNullOrEmpty(line)
                        ? string.Empty
                        : " | " + TrimLogLine(line)
                )
            );
        }

        private void TryCountAutomaticTavernSpell(string line)
        {
            if (
                line.IndexOf(
                    "BLOCK_START BlockType=POWER",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
                || !IsPlayerOwnedPowerBlock(line)
            )
            {
                return;
            }

            Entity sourceEntity = null;

            if (TryGetPlayerSourceEntity(line, out sourceEntity))
            {
                string sourceCardId =
                    !string.IsNullOrEmpty(
                        sourceEntity.Info.LatestCardId
                    )
                        ? sourceEntity.Info.LatestCardId
                        : sourceEntity.CardId;

                if (
                    IsTavernSpell(sourceEntity)
                    || IsTavernSpellCardId(sourceCardId)
                )
                {
                    CountPlayedSpellEntity(
                        sourceEntity,
                        true,
                        "POWER BLOCK SOURCE",
                        line
                    );
                    return;
                }
            }

            string blockCardId = TryExtractBlockCardId(line);

            if (IsTavernSpellCardId(blockCardId))
            {
                int sourceEntityId = TryExtractEntityId(line);

                if (
                    sourceEntityId > 0
                    && !_countedPlayedSpellEntityIds.Add(
                        sourceEntityId
                    )
                )
                {
                    return;
                }

                if (
                    sourceEntityId > 0
                    && _handPlayedSpellEntityIds.Contains(
                        sourceEntityId
                    )
                )
                {
                    return;
                }

                _playedSpellsAutomatic++;
                UpdatePlayedSpellTotal();

                WriteDiagnostic(
                    "SPELL PLAYED | automatic=true"
                    + " | source=POWER BLOCK CARD ID"
                    + " | id=" + sourceEntityId
                    + " | card=" + blockCardId
                    + " | " + TrimLogLine(line)
                );
                return;
            }

            string effectCardId = TryExtractEffectCardId(line);

            if (!IsTavernSpellCardId(effectCardId))
                return;

            _playedSpellsAutomatic++;
            UpdatePlayedSpellTotal();

            WriteDiagnostic(
                "SPELL PLAYED | automatic=true"
                + " | source=EFFECT CARD"
                + " | card=" + effectCardId
                + " | " + TrimLogLine(line)
            );
        }

        private static string TryExtractBlockCardId(string line)
        {
            Match match = BlockCardIdRegex.Match(line);

            if (!match.Success)
                return string.Empty;

            return match.Groups[1].Value;
        }

        private static string TryExtractEffectCardId(string line)
        {
            Match match = EffectCardIdRegex.Match(line);

            if (!match.Success)
                return string.Empty;

            string cardId = match.Groups[1].Value;

            if (
                string.IsNullOrWhiteSpace(cardId)
                || string.Equals(
                    cardId,
                    "null",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return string.Empty;
            }

            return cardId;
        }

        private static string GetBestCardId(Entity entity)
        {
            if (entity == null)
                return string.Empty;

            return !string.IsNullOrEmpty(entity.Info.LatestCardId)
                ? entity.Info.LatestCardId
                : entity.CardId ?? string.Empty;
        }

        private static bool IsMinionCard(Entity entity)
        {
            if (entity == null)
                return false;

            if (entity.IsMinion)
                return true;

            string cardId =
                !string.IsNullOrEmpty(entity.Info.LatestCardId)
                    ? entity.Info.LatestCardId
                    : entity.CardId;

            if (
                string.IsNullOrEmpty(cardId)
                || !HearthDb.Cards.All.TryGetValue(
                    cardId,
                    out HearthDb.Card card
                )
                || card == null
            )
            {
                return false;
            }

            return card.Type == CardType.MINION;
        }

        private static bool IsTavernSpell(Entity entity)
        {
            if (entity == null)
                return false;

            if (
                entity.IsBattlegroundsSpell
                || entity.GetTag(GameTag.IS_BACON_POOL_SPELL) > 0
            )
            {
                return true;
            }

            string cardId =
                !string.IsNullOrEmpty(entity.Info.LatestCardId)
                    ? entity.Info.LatestCardId
                    : entity.CardId;

            return IsTavernSpellCardId(cardId);
        }

        private static bool IsTavernSpellCardId(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return false;

            if (
                !HearthDb.Cards.All.TryGetValue(
                    cardId,
                    out HearthDb.Card card
                )
                || card == null
            )
            {
                return false;
            }

            return card.Type == CardType.BATTLEGROUND_SPELL
                || card.Entity.GetTag(
                    GameTag.IS_BACON_POOL_SPELL
                ) > 0;
        }

        private static bool IsPlayerOwnedPowerBlock(string line)
        {
            Match playerMatch = PowerBlockPlayerIdRegex.Match(line);

            if (
                playerMatch.Success
                && int.TryParse(
                    playerMatch.Groups[1].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int playerId
                )
            )
            {
                return playerId == Core.Game.Player.Id;
            }

            return TryGetPlayerSourceEntity(line, out Entity _);
        }

        private static bool TryGetSourceEntity(
            string line,
            out Entity entity)
        {
            entity = null;
            int entityId = TryExtractEntityId(line);

            if (entityId <= 0)
                return false;

            return Core.Game.Entities.TryGetValue(entityId, out entity)
                && entity != null;
        }

        private static bool EntityHasKeyword(
            Entity entity,
            string keyword)
        {
            if (entity == null || string.IsNullOrEmpty(keyword))
                return false;

            if (
                Enum.TryParse(
                    keyword,
                    true,
                    out GameTag keywordTag
                )
                && entity.GetTag(keywordTag) > 0
            )
            {
                return true;
            }

            string[] mechanics = entity.Card.Mechanics;

            if (mechanics == null)
                return false;

            foreach (string mechanic in mechanics)
            {
                if (
                    string.Equals(
                        mechanic,
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPlayerSourceEntity(
            string line,
            out Entity entity)
        {
            entity = null;
            int entityId = TryExtractEntityId(line);

            if (entityId <= 0)
                return false;

            if (
                !Core.Game.Entities.TryGetValue(entityId, out entity)
                || entity == null
            )
            {
                return false;
            }

            return entity.IsControlledBy(Core.Game.Player.Id);
        }

        private static int TryExtractEntityId(string line)
        {
            Match match = EntityIdRegex.Match(line);

            if (!match.Success)
                return 0;

            string value =
                match.Groups[1].Success
                    ? match.Groups[1].Value
                    : match.Groups[2].Value;

            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int entityId
            )
                ? entityId
                : 0;
        }

        private static string TrimLogLine(string line)
        {
            const int MaxLength = 320;

            if (line.Length <= MaxLength)
                return line;

            return line.Substring(0, MaxLength) + "...";
        }

        // ------------------------------------------------------------
        // Overlay construction
        // ------------------------------------------------------------

        private void CreateOverlay()
        {
            if (_panel != null)
                return;

            Grid root = new Grid
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };

            TextOptions.SetTextFormattingMode(
                root,
                TextFormattingMode.Display
            );

            for (int i = 0; i < 32; i++)
            {
                root.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = GridLength.Auto
                    }
                );
            }

            Grid header = new Grid
            {
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };

            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                }
            );
            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            );

            TextBlock title = new TextBlock
            {
                Text = "FINAL STATS",
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = TitleBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 0, 0, 4),
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            _matchDurationValue = new TextBlock
            {
                Text = "00:00",
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = ValueBrush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(8, 0, 2, 4),
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Typography.SetNumeralAlignment(
                _matchDurationValue,
                FontNumeralAlignment.Tabular
            );
            Typography.SetNumeralStyle(
                _matchDurationValue,
                FontNumeralStyle.Lining
            );

            Grid.SetColumn(title, 0);
            Grid.SetColumn(_matchDurationValue, 1);

            header.Children.Add(title);
            header.Children.Add(_matchDurationValue);

            Border separator = new Border
            {
                Height = 1,
                Background = DividerBrush,
                Margin = new Thickness(0, 0, 0, 4),
                IsHitTestVisible = false
            };

            Grid.SetRow(header, 0);
            Grid.SetRow(separator, 1);

            root.Children.Add(header);
            root.Children.Add(separator);
            _lastDisplayedMatchDurationSecond = -1;

            AddCategoryHeader(root, 2, "GLOBAL STATS");
            AddStatRow(root, 3, "Highest turn", out _highestTurnValue);
            AddStatRow(root, 4, "Gold spent", out _goldSpentValue);
            AddStatRow(root, 5, "Tavern rolls", out _tavernRollsValue);
            AddStatRow(root, 6, "Free rolls gained", out _freeRollsObtainedValue);
            AddStatRow(root, 7, "Battlecries played", out _battlecriesValue);
            AddStatRow(root, 8, "Rally triggered", out _ralliesValue);

            AddCategoryHeader(root, 9, "BOUGHT CARDS");
            AddStatRow(root, 10, "Cards bought", out _cardsBoughtValue);
            AddStatRow(root, 11, "Minions bought", out _minionsBoughtValue);
            AddStatRow(root, 12, "Spells bought", out _spellsBoughtValue);

            AddCategoryHeader(root, 13, "PLAYED CARDS");
            AddStatRow(root, 14, "Played cards", out _cardsPlayedValue);
            AddStatRow(root, 15, "Played minions", out _minionsPlayedValue);
            AddStatRow(root, 16, "Played spells", out _playedSpellsValue);

            AddCategoryHeader(root, 17, "CREATURES");
            AddStatRow(root, 18, "Highest creature", out _highestCreatureValue);
            AddStatRow(root, 19, "Highest ATK", out _highestAttackValue);
            AddStatRow(root, 20, "Highest HP", out _highestHealthValue);

            AddCategoryHeader(root, 21, "BUFFS");
            AddStatRow(root, 22, "Tavern buff max", out _tavernMinionBuffValue);
            AddStatRow(root, 23, "Spell power buff", out _tavernSpellBuffValue);

            AddCategoryHeader(root, 24, "HERO");
            AddStatRow(root, 25, "Hero damage dealt", out _heroDamageDealtValue);
            AddStatRow(root, 26, "Max damage dealt", out _maxHeroDamageDealtValue);
            AddStatRow(root, 27, "Hero damage taken", out _heroDamageTakenValue);
            AddStatRow(root, 28, "Max damage taken", out _maxHeroDamageTakenValue);
            AddStatRow(root, 29, "Combat wins", out _combatWinsValue);
            AddStatRow(root, 30, "Combat losses", out _combatLossesValue);
            AddStatRow(root, 31, "Combat draws", out _combatDrawsValue);

            _combatWinsValue.Foreground = PositiveBrush;
            _combatLossesValue.Foreground = NegativeBrush;
            _combatDrawsValue.Foreground = NeutralBrush;

            _panel = new Border
            {
                Width = PanelWidth,
                Height = PanelHeight,
                Background = PanelBrush,
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Child = root,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };

            Panel.SetZIndex(_panel, 1000);
            Core.OverlayCanvas.Children.Add(_panel);

            _toggleButtonText = new TextBlock
            {
                Text = "Hide combat stats",
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = ValueBrush,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            _toggleButton = new Border
            {
                Width = PanelWidth,
                Height = ToggleButtonHeight,
                Background = ToggleButtonBrush,
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Child = _toggleButtonText,
                Cursor = Cursors.Hand,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = true,
                Visibility = Visibility.Collapsed
            };

            _toggleButton.MouseEnter += HandleToggleButtonMouseEnter;
            _toggleButton.MouseLeave += HandleToggleButtonMouseLeave;
            _toggleButton.MouseLeftButtonDown += HandleToggleButtonMouseDown;
            _toggleButton.MouseLeftButtonUp += HandleToggleButtonMouseUp;

            Panel.SetZIndex(_toggleButton, 1001);
            Core.OverlayCanvas.Children.Add(_toggleButton);

            // HDT's overlay window is normally click-through. Register only
            // this element as interactive so the game remains clickable
            // everywhere else. This is the same mechanism used by HDT's
            // native interactive overlay controls.
            OverlayExtensions.SetIsOverlayHitTestVisible(
                _toggleButton,
                true
            );
        }

        private void HandleToggleButtonMouseEnter(
            object sender,
            MouseEventArgs e)
        {
            if (_toggleButton != null)
                _toggleButton.Background = ToggleButtonHoverBrush;
        }

        private void HandleToggleButtonMouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (_toggleButton != null)
                _toggleButton.Background = ToggleButtonBrush;
        }

        private void HandleToggleButtonMouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (_toggleButton != null)
                _toggleButton.Background = ToggleButtonPressedBrush;

            e.Handled = true;
        }

        private void HandleToggleButtonMouseUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (_showingFinalSummary)
                return;

            _pluginVisible = !_pluginVisible;

            if (_toggleButton != null)
                _toggleButton.Background = ToggleButtonHoverBrush;

            UpdateOverlayVisibility();
            PositionOverlay();
            e.Handled = true;
        }

        private static void AddCategoryHeader(
            Grid parent,
            int rowIndex,
            string title)
        {
            Grid header = new Grid
            {
                Height = CategoryHeaderHeight,
                Margin = new Thickness(0, 2, 0, 0),
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
                    Width = new GridLength(1, GridUnitType.Star)
                }
            );

            TextBlock headerText = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = CategoryBrush,
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
                Background = DividerBrush,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            Grid.SetColumn(headerText, 0);
            Grid.SetColumn(line, 1);

            header.Children.Add(headerText);
            header.Children.Add(line);

            Grid.SetRow(header, rowIndex);
            parent.Children.Add(header);
        }

        private static void AddStatRow(
            Grid parent,
            int rowIndex,
            string label,
            out TextBlock value)
        {
            Grid row = new Grid
            {
                Height = StatRowHeight,
                Margin = new Thickness(0),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                IsHitTestVisible = false
            };

            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
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
                Foreground = LabelBrush,
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
                Foreground = ValueBrush,
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

            Grid.SetRow(row, rowIndex);
            parent.Children.Add(row);
        }

        private void UpdateOverlayValues()
        {
            if (_panel == null)
                return;

            UpdateMatchDurationValue();
            SetValue(_goldSpentValue, _goldSpent.ToString(CultureInfo.InvariantCulture));
            SetValue(_cardsBoughtValue, _cardsBought.ToString(CultureInfo.InvariantCulture));
            SetValue(_minionsBoughtValue, _minionsBought.ToString(CultureInfo.InvariantCulture));
            SetValue(_spellsBoughtValue, _spellsBought.ToString(CultureInfo.InvariantCulture));
            SetValue(_freeRollsObtainedValue, _freeRollsObtained.ToString(CultureInfo.InvariantCulture));
            SetValue(_tavernRollsValue, _tavernRolls.ToString(CultureInfo.InvariantCulture));
            SetValue(_cardsPlayedValue, _cardsPlayed.ToString(CultureInfo.InvariantCulture));
            SetValue(_minionsPlayedValue, _minionsPlayed.ToString(CultureInfo.InvariantCulture));
            SetValue(_playedSpellsValue, _playedSpells.ToString(CultureInfo.InvariantCulture));
            SetValue(_battlecriesValue, _battlecries.ToString(CultureInfo.InvariantCulture));
            SetValue(_ralliesValue, _rallies.ToString(CultureInfo.InvariantCulture));
            SetValue(
                _highestAttackValue,
                _highestAttack.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _highestHealthValue,
                _highestHealth.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _highestCreatureValue,
                _highestCreatureAttack.ToString(
                    CultureInfo.InvariantCulture
                )
                + " / "
                + _highestCreatureHealth.ToString(
                    CultureInfo.InvariantCulture
                )
            );
            SetValue(
                _highestTurnValue,
                _highestTurn.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _heroDamageDealtValue,
                _heroDamageDealt.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _maxHeroDamageDealtValue,
                _maxHeroDamageDealt.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _heroDamageTakenValue,
                _heroDamageTaken.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _maxHeroDamageTakenValue,
                _maxHeroDamageTaken.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _combatWinsValue,
                _combatWins.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _combatLossesValue,
                _combatLosses.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _combatDrawsValue,
                _combatDraws.ToString(CultureInfo.InvariantCulture)
            );
            SetValue(
                _tavernSpellBuffValue,
                FormatPositiveStats(
                    _highestTavernSpellAttack,
                    _highestTavernSpellHealth
                )
            );
            SetValue(
                _tavernMinionBuffValue,
                FormatPositiveStats(
                    _highestTavernMinionAttack,
                    _highestTavernMinionHealth
                )
            );
        }

        private void UpdateMatchDurationValue()
        {
            if (_matchDurationValue == null)
                return;

            TimeSpan duration = _trackingMatch
                ? _matchStopwatch.Elapsed
                : _finalMatchDuration;
            long elapsedSecond = (long)duration.TotalSeconds;

            // OnUpdate runs roughly every 100 ms. Avoid rewriting the WPF
            // text unless the displayed second has actually changed.
            if (elapsedSecond == _lastDisplayedMatchDurationSecond)
                return;

            _lastDisplayedMatchDurationSecond = elapsedSecond;
            _matchDurationValue.Text = FormatMatchDuration(duration);
        }

        private static string FormatMatchDuration(TimeSpan duration)
        {
            long totalHours = (long)duration.TotalHours;

            if (totalHours > 0)
            {
                return totalHours.ToString(CultureInfo.InvariantCulture)
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

        private static string FormatPositiveStats(
            int attack,
            int health)
        {
            return "+"
                + attack.ToString(CultureInfo.InvariantCulture)
                + " / +"
                + health.ToString(CultureInfo.InvariantCulture);
        }

        private void UpdateOverlayVisibility()
        {
            if (_panel == null || _toggleButton == null)
                return;

            bool hasData = _hasMatchData;
            bool showFinalSummary =
                hasData && _showingFinalSummary;
            bool showToggleButton =
                hasData && !showFinalSummary;

            _panel.Visibility =
                hasData && (showFinalSummary || _pluginVisible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            _toggleButton.Visibility =
                showToggleButton
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // Remove the hidden button from HDT's clickable regions while
            // the final summary is displayed in the menu.
            OverlayExtensions.SetIsOverlayHitTestVisible(
                _toggleButton,
                showToggleButton
            );

            if (_toggleButtonText != null)
            {
                _toggleButtonText.Text =
                    _pluginVisible
                        ? "Hide combat stats"
                        : "Show combat stats";
            }
        }

        private void PositionOverlay()
        {
            if (_panel == null || _toggleButton == null)
                return;

            double canvasWidth = Core.OverlayCanvas.ActualWidth;
            double canvasHeight = Core.OverlayCanvas.ActualHeight;
            double left = Math.Max(
                0,
                canvasWidth - PanelRight - PanelWidth
            );
            double buttonTop = Math.Max(
                0,
                canvasHeight - PanelBottom - ToggleButtonHeight
            );
            double panelTop;

            if (_showingFinalSummary)
            {
                // The button is removed in the menu, so the panel itself
                // uses the requested 50 px bottom margin.
                panelTop = Math.Max(
                    0,
                    canvasHeight - PanelBottom - PanelHeight
                );
            }
            else
            {
                panelTop = Math.Max(
                    0,
                    buttonTop - ToggleButtonGap - PanelHeight
                );
            }

            Canvas.SetLeft(_panel, left);
            Canvas.SetTop(_panel, panelTop);
            Canvas.SetLeft(_toggleButton, left);
            Canvas.SetTop(_toggleButton, buttonTop);
        }

        private static void SetValue(
            TextBlock textBlock,
            string value)
        {
            if (textBlock != null)
                textBlock.Text = value;
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        // ------------------------------------------------------------
        // Diagnostics
        // ------------------------------------------------------------

        private static void WriteDiagnostic(string message)
        {
            if (!EnableDiagnosticLog)
                return;

            try
            {
                string assemblyPath = typeof(Plugin).Assembly.Location;
                string directory = Path.GetDirectoryName(assemblyPath);

                if (string.IsNullOrEmpty(directory))
                    return;

                string path = Path.Combine(
                    directory,
                    "FinalStatsPlugin_debug.log"
                );

                File.AppendAllText(
                    path,
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss.fff",
                        CultureInfo.InvariantCulture
                    )
                    + " | " + message
                    + Environment.NewLine
                );
            }
            catch
            {
                // Diagnostics must never interrupt HDT.
            }
        }

        // ------------------------------------------------------------
        // Small immutable snapshot used for zone transitions
        // ------------------------------------------------------------

        private readonly struct EntityState
        {
            public EntityState(
                Zone zone,
                int controllerId)
            {
                Zone = zone;
                ControllerId = controllerId;
            }

            public Zone Zone { get; }
            public int ControllerId { get; }

            public static EntityState FromEntity(Entity entity)
            {
                return new EntityState(
                    (Zone)entity.GetTag(GameTag.ZONE),
                    entity.GetTag(GameTag.CONTROLLER)
                );
            }
        }
    }
}
