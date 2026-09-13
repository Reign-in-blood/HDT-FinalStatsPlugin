using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace FinalStatsPlugin
{
    /// <summary>
    /// Tracks Duos identity and preserves the last valid board observed for
    /// the local player's teammate. This class intentionally tracks the real
    /// data independently from presentation settings so future HDT options can
    /// hide Duo UI without destroying the captured teammate state.
    /// </summary>
    internal sealed class DuosFinalBoardTracker
    {
        private const string UnknownPlayerName =
            "UNKNOWN HUMAN PLAYER";

        private static readonly TimeSpan SnapshotInterval =
            TimeSpan.FromMilliseconds(500);

        private static readonly Regex PlayerLineRegex = new Regex(
            @"PlayerID=(?<id>\d+), PlayerName=(?<name>.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private readonly Action<string> _log;
        private readonly Dictionary<int, string> _namesByPlayerId =
            new Dictionary<int, string>();
        private readonly List<Entity> _partnerBoardSnapshot =
            new List<Entity>();

        private DateTime _nextSnapshotUtc = DateTime.MinValue;
        private int _partnerPlayerId;
        private string _partnerName;
        private bool _isDuosMatch;

        public DuosFinalBoardTracker(Action<string> log)
        {
            _log = log;
        }

        public bool IsDuosMatch => _isDuosMatch;

        public int PartnerPlayerId => _partnerPlayerId;

        public string PartnerName => _partnerName;

        public IReadOnlyList<Entity> PartnerBoardSnapshot =>
            _partnerBoardSnapshot;

        public bool HasPartnerBoardSnapshot =>
            _partnerBoardSnapshot.Count > 0;

        public void Reset()
        {
            _namesByPlayerId.Clear();
            _partnerBoardSnapshot.Clear();
            _nextSnapshotUtc = DateTime.MinValue;
            _partnerPlayerId = 0;
            _partnerName = null;
            _isDuosMatch = false;
        }

        public void BeginMatch()
        {
            Reset();
            _isDuosMatch = Core.Game.IsBattlegroundsDuosMatch;

            if (_isDuosMatch)
            {
                Log(
                    "DUOS FINAL BOARD TRACKING"
                    + " | enabled=true"
                );

                Update("match-start", true);
            }
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
                    + " | available=true"
                );
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves the teammate and refreshes the last valid board snapshot.
        /// Returns true when data relevant to the final Duo presentation may
        /// have changed.
        /// </summary>
        public bool Update(
            string source,
            bool forceSnapshot)
        {
            EnsureDuosMode();

            if (!_isDuosMatch)
                return false;

            bool changed = false;
            int resolvedPartnerPlayerId =
                ResolvePartnerPlayerId();

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
                    + " | playerId=" + _partnerPlayerId
                );
            }

            if (_partnerPlayerId <= 0)
                return changed;

            if (
                _namesByPlayerId.TryGetValue(
                    _partnerPlayerId,
                    out string resolvedName
                )
                && IsUsablePlayerName(resolvedName)
                && !string.Equals(
                    _partnerName,
                    resolvedName,
                    StringComparison.Ordinal
                )
            )
            {
                _partnerName = resolvedName;
                changed = true;

                Log(
                    "DUOS PARTNER NAME"
                    + " | available=true"
                );
            }

            // Combat entities can be damaged, destroyed or temporarily
            // replaced. They are not a safe source for the final intact
            // teammate board. Normal tracking only snapshots recruitment
            // phases. A forced capture is reserved for the same precise
            // tavern-to-combat boundary already used by the local board.
            if (
                !forceSnapshot
                && Core.Game.IsBattlegroundsCombatPhase
            )
            {
                return changed;
            }

            DateTime now = DateTime.UtcNow;
            if (!forceSnapshot && now < _nextSnapshotUtc)
                return changed;

            _nextSnapshotUtc = now.Add(SnapshotInterval);

            List<Entity> currentBoard =
                Core.Game.Entities.Values
                    .Where(
                        entity =>
                            entity != null
                            && entity.IsMinion
                            && entity.IsInPlay
                            && entity.IsControlledBy(
                                _partnerPlayerId
                            )
                    )
                    .OrderBy(
                        entity =>
                            entity.GetTag(
                                GameTag.ZONE_POSITION
                            )
                    )
                    .Select(entity => entity.Clone())
                    .ToList();

            // A teammate board can disappear temporarily while HDT/Hearthstone
            // replaces entities. Never erase the last known valid board just
            // because the current tick exposes no teammate minions.
            if (currentBoard.Count == 0)
            {
                if (
                    forceSnapshot
                    && _partnerBoardSnapshot.Count > 0
                )
                {
                    Log(
                        "DUOS PARTNER BOARD RETAINED"
                        + " | source=" + source
                        + " | minions="
                        + _partnerBoardSnapshot.Count
                    );
                }

                return changed;
            }

            if (!HaveSameBoard(_partnerBoardSnapshot, currentBoard))
            {
                _partnerBoardSnapshot.Clear();
                _partnerBoardSnapshot.AddRange(currentBoard);
                changed = true;

                Log(
                    "DUOS PARTNER BOARD SNAPSHOT"
                    + " | source=" + source
                    + " | minions=" + currentBoard.Count
                );
            }

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

        private static int ResolvePartnerPlayerId()
        {
            int playerId = Core.Game.Player?.Id ?? 0;
            if (playerId <= 0)
                return 0;

            List<Entity> entities =
                Core.Game.Entities.Values
                    .Where(entity => entity != null)
                    .ToList();

            Entity hero = FindAuthoritativeHero(
                entities,
                playerId
            );

            return ResolveTaggedPlayerId(
                hero,
                entities,
                playerId,
                GameTag.BACON_DUO_TEAMMATE_PLAYER_ID
            );
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
            if (playerId <= 0)
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
