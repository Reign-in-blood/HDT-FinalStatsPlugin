using HearthDb.Enums;
using HearthMirror.Objects;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace FinalStatsPlugin
{
    /// <summary>
    /// Tracks the stable local/teammate identities in Duos and preserves the
    /// last valid recruitment boards for both players. Duos combat can remap
    /// the friendly controller to whichever teammate fights first, so final
    /// board capture must not rely on the live combat controller alone.
    /// </summary>
    internal sealed class DuosFinalBoardTracker
    {
        private const string UnknownPlayerName =
            "UNKNOWN HUMAN PLAYER";

        private static readonly TimeSpan SnapshotInterval =
            TimeSpan.FromMilliseconds(100);

        private static readonly Regex PlayerLineRegex = new Regex(
            @"PlayerID=(?<id>\d+), PlayerName=(?<name>.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private readonly Action<string> _log;
        private readonly Dictionary<int, string> _namesByPlayerId =
            new Dictionary<int, string>();
        private readonly List<Entity> _localBoardSnapshot =
            new List<Entity>();
        private readonly List<Entity> _partnerBoardSnapshot =
            new List<Entity>();

        private DateTime _nextSnapshotUtc = DateTime.MinValue;
        private int _localPlayerId;
        private int _partnerPlayerId;
        private string _partnerName;
        private string _lastTeammateStateDiagnostic;
        private bool _isDuosMatch;

        public DuosFinalBoardTracker(Action<string> log)
        {
            _log = log;
        }

        public bool IsDuosMatch => _isDuosMatch;

        public int LocalPlayerId => _localPlayerId;

        public int PartnerPlayerId => _partnerPlayerId;

        public string PartnerName => _partnerName;

        public IReadOnlyList<Entity> LocalBoardSnapshot =>
            _localBoardSnapshot;

        public IReadOnlyList<Entity> PartnerBoardSnapshot =>
            _partnerBoardSnapshot;

        public bool HasLocalBoardSnapshot =>
            _localBoardSnapshot.Count > 0;

        public bool HasPartnerBoardSnapshot =>
            _partnerBoardSnapshot.Count > 0;

        public void Reset()
        {
            _namesByPlayerId.Clear();
            _localBoardSnapshot.Clear();
            _partnerBoardSnapshot.Clear();
            _nextSnapshotUtc = DateTime.MinValue;
            _localPlayerId = 0;
            _partnerPlayerId = 0;
            _partnerName = null;
            _lastTeammateStateDiagnostic = null;
            _isDuosMatch = false;
        }

        public void BeginMatch()
        {
            Reset();
            _isDuosMatch = Core.Game.IsBattlegroundsDuosMatch;

            if (!_isDuosMatch)
                return;

            Log(
                "DUOS FINAL BOARD TRACKING"
                + " | enabled=true"
            );

            Update("match-start", true);
        }

        public bool ProcessPowerLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            EnsureDuosMode();

            if (!_isDuosMatch)
                return false;

            Match match = PlayerLineRegex.Match(line);
            if (!match.Success)
                return false;

            if (
                !int.TryParse(
                    match.Groups["id"].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int playerId
                )
                || playerId <= 0
            )
            {
                return false;
            }

            string name = StripBattleTag(
                match.Groups["name"].Value
            );

            if (!IsUsablePlayerName(name))
                return false;

            _namesByPlayerId[playerId] = name;

            if (
                playerId == _partnerPlayerId
                && !string.Equals(
                    _partnerName,
                    name,
                    StringComparison.Ordinal
                )
            )
            {
                _partnerName = name;
                Log(
                    "DUOS PARTNER NAME"
                    + " | source=powerlog"
                    + " | available=true"
                );
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves stable Duos identities and updates the last valid board
        /// snapshots. Returns true when final-board presentation data changed.
        /// </summary>
        public bool Update(
            string source,
            bool forceSnapshot)
        {
            EnsureDuosMode();

            if (!_isDuosMatch)
                return false;

            bool changed = false;

            int resolvedLocalPlayerId = ResolveLocalPlayerId();
            if (
                resolvedLocalPlayerId > 0
                && resolvedLocalPlayerId != _localPlayerId
            )
            {
                _localPlayerId = resolvedLocalPlayerId;
                changed = true;

                Log(
                    "DUOS LOCAL PLAYER RESOLVED"
                    + " | playerId=" + _localPlayerId
                    + " | primaryPlayerId="
                    + Core.Game.PrimaryPlayerId
                );
            }

            if (_localPlayerId > 0)
            {
                int resolvedPartnerPlayerId =
                    ResolvePartnerPlayerId(_localPlayerId);

                if (
                    resolvedPartnerPlayerId > 0
                    && resolvedPartnerPlayerId
                        != _partnerPlayerId
                )
                {
                    _partnerPlayerId = resolvedPartnerPlayerId;
                    changed = true;

                    Log(
                        "DUOS PARTNER RESOLVED"
                        + " | localPlayerId=" + _localPlayerId
                        + " | partnerPlayerId="
                        + _partnerPlayerId
                    );
                }
            }

            if (UpdatePartnerBoardFromTeammateState(source))
                changed = true;

            if (TryResolvePartnerName())
                changed = true;

            DateTime now = DateTime.UtcNow;

            if (!Core.Game.IsBattlegroundsCombatPhase)
            {
                if (!forceSnapshot && now < _nextSnapshotUtc)
                    return changed;

                _nextSnapshotUtc = now.Add(SnapshotInterval);

                if (
                    UpdateRecruitmentBoard(
                        _localPlayerId,
                        _localBoardSnapshot,
                        "local",
                        source
                    )
                )
                {
                    changed = true;
                }

                // The teammate board is normally exposed by HDT through the
                // dedicated HearthMirror teammate state above. Keep the normal
                // entity collection only as a safe fallback when it happens to
                // contain the partner controller too.
                if (
                    !HasPartnerBoardSnapshot
                    && UpdateRecruitmentBoard(
                        _partnerPlayerId,
                        _partnerBoardSnapshot,
                        "partner",
                        source
                    )
                )
                {
                    changed = true;
                }

                return changed;
            }

            // During combat Hearthstone can present either teammate through
            // the friendly controller. Only use that combat representation as
            // a fallback when we have no recruitment snapshot for that player,
            // and only while the visible board is still undamaged.
            if (TryCaptureMissingCombatBoard(source))
                changed = true;

            return changed;
        }

        private void EnsureDuosMode()
        {
            if (_isDuosMatch)
                return;

            if (Core.Game.IsBattlegroundsDuosMatch)
            {
                _isDuosMatch = true;
                Log(
                    "DUOS FINAL BOARD TRACKING"
                    + " | enabled=true"
                );
            }
        }

        private int ResolveLocalPlayerId()
        {
            if (_localPlayerId > 0)
                return _localPlayerId;

            // HDT stores the primary local player ID at the beginning of each
            // Battlegrounds shopping phase. Unlike the visible combat board,
            // this value is not tied to which Duos teammate fights first.
            int primaryPlayerId = Core.Game.PrimaryPlayerId;
            if (primaryPlayerId > 0)
                return primaryPlayerId;

            // Before HDT has populated PrimaryPlayerId, the normal Player ID is
            // still a safe fallback only during recruitment.
            if (!Core.Game.IsBattlegroundsCombatPhase)
                return Core.Game.Player?.Id ?? 0;

            return 0;
        }

        private static int ResolvePartnerPlayerId(
            int localPlayerId)
        {
            if (localPlayerId <= 0)
                return 0;

            List<Entity> entities =
                Core.Game.Entities.Values
                    .Where(entity => entity != null)
                    .ToList();

            Entity hero = FindAuthoritativeHero(
                entities,
                localPlayerId
            );

            return ResolveTaggedPlayerId(
                hero,
                entities,
                localPlayerId,
                GameTag.BACON_DUO_TEAMMATE_PLAYER_ID
            );
        }

        private bool TryResolvePartnerName()
        {
            if (_partnerPlayerId <= 0)
                return false;

            string resolvedName = null;
            string source = null;

            if (
                _namesByPlayerId.TryGetValue(
                    _partnerPlayerId,
                    out string powerLogName
                )
                && IsUsablePlayerName(powerLogName)
            )
            {
                resolvedName = powerLogName;
                source = "powerlog";
            }

            if (string.IsNullOrWhiteSpace(resolvedName))
            {
                resolvedName = ResolvePartnerNameFromLobbyInfo(
                    _partnerPlayerId
                );
                source = "lobbyInfo";
            }

            if (
                !IsUsablePlayerName(resolvedName)
                || string.Equals(
                    _partnerName,
                    resolvedName,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            _partnerName = resolvedName;

            Log(
                "DUOS PARTNER NAME"
                + " | source=" + source
                + " | available=true"
            );

            return true;
        }

        private static string ResolvePartnerNameFromLobbyInfo(
            int partnerPlayerId)
        {
            try
            {
                var lobbyPlayers =
                    Core.Game.MetaData
                        ?.BattlegroundsLobbyInfo
                        ?.Players;

                if (lobbyPlayers == null)
                    return null;

                Entity partnerHero = FindAuthoritativeHero(
                    Core.Game.Entities.Values,
                    partnerPlayerId
                );

                string partnerHeroCardId =
                    NormalizeHeroCardId(
                        partnerHero?.CardId
                    );

                if (string.IsNullOrWhiteSpace(partnerHeroCardId))
                {
                    partnerHeroCardId = NormalizeHeroCardId(
                        ResolvePartnerHeroCardIdFromTeammateState()
                    );
                }

                if (string.IsNullOrWhiteSpace(partnerHeroCardId))
                    return null;

                foreach (var info in lobbyPlayers)
                {
                    if (info == null)
                        continue;

                    string infoHeroCardId =
                        NormalizeHeroCardId(
                            info.HeroCardId
                        );

                    if (
                        !string.Equals(
                            infoHeroCardId,
                            partnerHeroCardId,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    string name = StripBattleTag(info.Name);
                    if (IsUsablePlayerName(name))
                        return name;
                }
            }
            catch
            {
                // Power.log remains the fallback when lobby metadata is not
                // available yet or HDT remote hero data is still loading.
            }

            return null;
        }

        private bool UpdatePartnerBoardFromTeammateState(
            string source)
        {
            if (_partnerPlayerId <= 0)
                return false;

            BattlegroundsDuosBoardState state =
                Core.Game.BattlegroundsDuosBoardState;

            int boardControllerId =
                ResolveTeammateStateBoardController(state);

            if (
                state?.Entities == null
                || state.Entities.Count == 0
                || !state.IsViewingTeammate
                || Core.Game.IsBattlegroundsCombatPhase
                || boardControllerId <= 0
            )
            {
                LogTeammateStateDiagnostic(
                    state,
                    boardControllerId,
                    0,
                    source
                );
                return false;
            }

            // HDT interprets HearthMirror's teammate state as a replacement
            // view of the friendly side. Its CONTROLLER values therefore use
            // Core.Game.Player.Id, not BACON_DUO_TEAMMATE_PLAYER_ID. The latter
            // remains the stable identity used for the teammate name only.
            List<BattlegroundsTeammateBoardStateEntity> rawBoard =
                state.Entities
                    .Where(
                        entity =>
                            entity != null
                            && GetTeammateTag(
                                entity,
                                GameTag.CONTROLLER
                            ) == boardControllerId
                            && GetTeammateTag(
                                entity,
                                GameTag.ZONE
                            ) == (int)Zone.PLAY
                            && GetTeammateTag(
                                entity,
                                GameTag.CARDTYPE
                            ) == (int)CardType.MINION
                    )
                    .OrderBy(
                        entity =>
                            GetTeammateTag(
                                entity,
                                GameTag.ZONE_POSITION
                            )
                    )
                    .ToList();

            LogTeammateStateDiagnostic(
                state,
                boardControllerId,
                rawBoard.Count,
                source
            );

            if (rawBoard.Count == 0)
                return false;

            // Never replace an intact recruitment snapshot with a combat board
            // after minions have started taking damage.
            if (
                rawBoard.Any(
                    entity =>
                        GetTeammateTag(
                            entity,
                            GameTag.DAMAGE
                        ) > 0
                )
            )
            {
                return false;
            }

            List<Entity> board = rawBoard
                .Select(ConvertTeammateEntity)
                .Where(entity => entity != null)
                .ToList();

            if (board.Count == 0)
                return false;

            if (HaveSameBoard(_partnerBoardSnapshot, board))
                return false;

            _partnerBoardSnapshot.Clear();
            _partnerBoardSnapshot.AddRange(board);

            Log(
                "DUOS TEAMMATE BOARD SNAPSHOT"
                + " | source=" + source
                + " | playerId=" + _partnerPlayerId
                + " | boardController=" + boardControllerId
                + " | viewing=" + state.IsViewingTeammate
                + " | minions=" + board.Count
            );

            return true;
        }

        private static int ResolveTeammateStateBoardController(
            BattlegroundsDuosBoardState state)
        {
            if (
                state?.IsViewingTeammate != true
                || Core.Game.IsBattlegroundsCombatPhase
            )
            {
                return 0;
            }

            return Core.Game.Player?.Id ?? 0;
        }

        private void LogTeammateStateDiagnostic(
            BattlegroundsDuosBoardState state,
            int boardControllerId,
            int partnerMinionCount,
            string source)
        {
            string controllers = "none";
            int entityCount = 0;
            bool viewing = false;

            if (state?.Entities != null)
            {
                entityCount = state.Entities.Count;
                viewing = state.IsViewingTeammate;
                controllers = string.Join(
                    ",",
                    state.Entities
                        .Where(entity => entity != null)
                        .Select(
                            entity => GetTeammateTag(
                                entity,
                                GameTag.CONTROLLER
                            )
                        )
                        .Where(controller => controller > 0)
                        .Distinct()
                        .OrderBy(controller => controller)
                );

                if (string.IsNullOrWhiteSpace(controllers))
                    controllers = "none";
            }

            string diagnostic =
                "DUOS TEAMMATE STATE"
                + " | viewing=" + viewing
                + " | entities=" + entityCount
                + " | partnerPlayerId=" + _partnerPlayerId
                + " | boardController=" + boardControllerId
                + " | controllers=" + controllers
                + " | partnerMinions=" + partnerMinionCount;

            if (
                string.Equals(
                    _lastTeammateStateDiagnostic,
                    diagnostic,
                    StringComparison.Ordinal
                )
            )
            {
                return;
            }

            _lastTeammateStateDiagnostic = diagnostic;
            Log(diagnostic + " | source=" + source);
        }

        private static string ResolvePartnerHeroCardIdFromTeammateState()
        {
            BattlegroundsDuosBoardState state =
                Core.Game.BattlegroundsDuosBoardState;

            int boardControllerId =
                ResolveTeammateStateBoardController(state);

            if (
                state?.Entities == null
                || boardControllerId <= 0
            )
            {
                return null;
            }

            BattlegroundsTeammateBoardStateEntity hero =
                state.Entities.FirstOrDefault(
                    entity =>
                        entity != null
                        && GetTeammateTag(
                            entity,
                            GameTag.CONTROLLER
                        ) == boardControllerId
                        && GetTeammateTag(
                            entity,
                            GameTag.CARDTYPE
                        ) == (int)CardType.HERO
                        && !string.IsNullOrWhiteSpace(
                            entity.CardId
                        )
                );

            return hero?.CardId;
        }

        private static int GetTeammateTag(
            BattlegroundsTeammateBoardStateEntity entity,
            GameTag tag)
        {
            if (
                entity?.Tags == null
                || !entity.Tags.TryGetValue(
                    (int)tag,
                    out int value
                )
            )
            {
                return 0;
            }

            return value;
        }

        private static Entity ConvertTeammateEntity(
            BattlegroundsTeammateBoardStateEntity source)
        {
            if (
                source == null
                || string.IsNullOrWhiteSpace(source.CardId)
            )
            {
                return null;
            }

            int entityId = GetTeammateTag(
                source,
                GameTag.ENTITY_ID
            );

            if (entityId <= 0)
            {
                int zonePosition = Math.Max(
                    0,
                    GetTeammateTag(
                        source,
                        GameTag.ZONE_POSITION
                    )
                );
                entityId = -1000000 - zonePosition;
            }

            Entity snapshot = new Entity(entityId)
            {
                CardId = source.CardId
            };

            if (source.Tags != null)
            {
                foreach (
                    KeyValuePair<int, int> pair in source.Tags
                )
                {
                    snapshot.Tags[(GameTag)pair.Key] = pair.Value;
                }
            }

            return snapshot;
        }

        private bool UpdateRecruitmentBoard(
            int playerId,
            List<Entity> destination,
            string role,
            string source)
        {
            if (playerId <= 0)
                return false;

            List<Entity> board = CollectBoardForController(playerId);

            // In Duos, the teammate board may simply not be present in the
            // normal entity collection during recruitment. Do not interpret an
            // unavailable empty result as a real empty final board and erase a
            // previously valid snapshot.
            if (board.Count == 0)
                return false;

            if (HaveSameBoard(destination, board))
                return false;

            destination.Clear();
            destination.AddRange(board);

            Log(
                "DUOS BOARD SNAPSHOT"
                + " | role=" + role
                + " | source=" + source
                + " | playerId=" + playerId
                + " | minions=" + board.Count
            );

            return true;
        }

        private bool TryCaptureMissingCombatBoard(string source)
        {
            int activePlayerId = ResolveActiveFriendlyPlayerId();
            if (activePlayerId <= 0)
                return false;

            List<Entity> destination;
            string role;

            if (
                activePlayerId == _localPlayerId
                && _localBoardSnapshot.Count == 0
            )
            {
                destination = _localBoardSnapshot;
                role = "local";
            }
            else if (
                activePlayerId == _partnerPlayerId
                && _partnerBoardSnapshot.Count == 0
            )
            {
                destination = _partnerBoardSnapshot;
                role = "partner";
            }
            else
            {
                return false;
            }

            int friendlyControllerId = Core.Game.Player?.Id ?? 0;
            if (friendlyControllerId <= 0)
                return false;

            List<Entity> board =
                CollectBoardForController(friendlyControllerId);

            if (
                board.Count == 0
                || board.Any(
                    entity =>
                        entity.GetTag(GameTag.DAMAGE) > 0
                )
            )
            {
                return false;
            }

            destination.Clear();
            destination.AddRange(board);

            Log(
                "DUOS COMBAT BOARD FALLBACK"
                + " | role=" + role
                + " | source=" + source
                + " | activePlayerId=" + activePlayerId
                + " | controller=" + friendlyControllerId
                + " | minions=" + board.Count
            );

            return true;
        }

        private static int ResolveActiveFriendlyPlayerId()
        {
            Entity playerEntity = Core.Game.PlayerEntity;
            int activeHeroEntityId =
                playerEntity?.GetTag(
                    GameTag.HERO_ENTITY
                ) ?? 0;

            if (
                activeHeroEntityId <= 0
                || !Core.Game.Entities.TryGetValue(
                    activeHeroEntityId,
                    out Entity activeHero
                )
                || activeHero == null
                || !activeHero.HasTag(GameTag.PLAYER_ID)
            )
            {
                return 0;
            }

            return activeHero.GetTag(GameTag.PLAYER_ID);
        }

        private static List<Entity> CollectBoardForController(
            int controllerId)
        {
            if (controllerId <= 0)
                return new List<Entity>();

            return Core.Game.Entities.Values
                .Where(
                    entity =>
                        entity != null
                        && entity.IsMinion
                        && entity.IsInPlay
                        && entity.IsControlledBy(controllerId)
                )
                .OrderBy(
                    entity =>
                        entity.GetTag(
                            GameTag.ZONE_POSITION
                        )
                )
                .Select(entity => entity.Clone())
                .ToList();
        }

        private static int ResolveTaggedPlayerId(
            Entity hero,
            IEnumerable<Entity> entities,
            int playerId,
            GameTag tag)
        {
            if (hero != null && hero.HasTag(tag))
            {
                int value = hero.GetTag(tag);
                if (value > 0 && value != playerId)
                    return value;
            }

            Entity taggedEntity = entities.FirstOrDefault(
                entity =>
                    entity != null
                    && entity.HasTag(GameTag.PLAYER_ID)
                    && entity.GetTag(GameTag.PLAYER_ID)
                        == playerId
                    && entity.HasTag(tag)
                    && entity.GetTag(tag) > 0
                    && entity.GetTag(tag) != playerId
            );

            return taggedEntity?.GetTag(tag) ?? 0;
        }

        private static Entity FindAuthoritativeHero(
            IEnumerable<Entity> entities,
            int playerId)
        {
            if (playerId <= 0 || entities == null)
                return null;

            List<Entity> candidates = entities
                .Where(
                    entity =>
                        entity != null
                        && entity.IsHero
                        && entity.HasTag(GameTag.PLAYER_ID)
                        && entity.GetTag(GameTag.PLAYER_ID)
                            == playerId
                        && entity.HasTag(
                            GameTag.PLAYER_LEADERBOARD_PLACE
                        )
                )
                .ToList();

            if (candidates.Count == 0)
                return null;

            return candidates.FirstOrDefault(
                       entity => entity.IsInPlay
                   )
                   ?? candidates[candidates.Count - 1];
        }

        private static bool HaveSameBoard(
            IReadOnlyList<Entity> first,
            IReadOnlyList<Entity> second)
        {
            if (first.Count != second.Count)
                return false;

            for (int index = 0; index < first.Count; index++)
            {
                Entity left = first[index];
                Entity right = second[index];

                if (
                    left == null
                    || right == null
                    || left.Id != right.Id
                    || !string.Equals(
                        GetBestCardId(left),
                        GetBestCardId(right),
                        StringComparison.Ordinal
                    )
                    || left.Attack != right.Attack
                    || left.GetTag(GameTag.HEALTH)
                        != right.GetTag(GameTag.HEALTH)
                    || left.GetTag(GameTag.ZONE_POSITION)
                        != right.GetTag(GameTag.ZONE_POSITION)
                    || left.GetTag(GameTag.DIVINE_SHIELD)
                        != right.GetTag(GameTag.DIVINE_SHIELD)
                    || left.GetTag(GameTag.TAUNT)
                        != right.GetTag(GameTag.TAUNT)
                    || left.GetTag(GameTag.REBORN)
                        != right.GetTag(GameTag.REBORN)
                    || left.GetTag(GameTag.WINDFURY)
                        != right.GetTag(GameTag.WINDFURY)
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetBestCardId(Entity entity)
        {
            if (entity == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(
                    entity.Info?.LatestCardId
                )
                ? entity.Info.LatestCardId
                : entity.CardId ?? string.Empty;
        }

        private static string NormalizeHeroCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return null;

            try
            {
                cardId =
                    BattlegroundsUtils.GetOriginalHeroId(cardId)
                    ?? cardId;
            }
            catch
            {
                // HDT remote data may not be ready yet.
            }

            int skinIndex = cardId.IndexOf(
                "_SKIN_",
                StringComparison.OrdinalIgnoreCase
            );

            return skinIndex > 0
                ? cardId.Substring(0, skinIndex)
                : cardId;
        }

        private static string StripBattleTag(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();
            int separatorIndex = name.LastIndexOf('#');

            if (
                separatorIndex <= 0
                || separatorIndex == name.Length - 1
            )
            {
                return name;
            }

            string code = name.Substring(separatorIndex + 1);
            return code.All(char.IsDigit)
                ? name.Substring(0, separatorIndex)
                : name;
        }

        private static bool IsUsablePlayerName(string name)
        {
            return
                !string.IsNullOrWhiteSpace(name)
                && !string.Equals(
                    name,
                    UnknownPlayerName,
                    StringComparison.OrdinalIgnoreCase
                )
                && !string.Equals(
                    name,
                    "...",
                    StringComparison.Ordinal
                );
        }

        private void Log(string message)
        {
            try
            {
                _log?.Invoke(message);
            }
            catch
            {
                // Diagnostics must never affect tracking.
            }
        }
    }
}
