# AGENTS.md — HDT-FinalStatsPlugin

## 1. Purpose and scope

This file contains the permanent development instructions for the repository:

```text
Reign-in-blood/HDT-FinalStatsPlugin
```

It applies to the entire repository unless a more specific `AGENTS.md` file is later added inside a subdirectory.

Before changing any file:

1. read this file completely;
2. inspect the relevant source files;
3. understand the current behavior;
4. make the smallest coherent change;
5. review the complete diff;
6. compile and test when the environment allows it;
7. clearly report what was and was not verified.

Direct instructions from the user for a specific task take priority over this file.

---

## 2. Project identity — do not confuse this repository

This repository is **HDT-FinalStatsPlugin**.

It is a Windows plugin for **Hearthstone Deck Tracker (HDT)**, designed specifically for **Hearthstone Battlegrounds**.

The plugin displayed inside HDT is currently named:

```text
Battlegrounds Final Stats
```

Its purpose is to collect and display cumulative statistics from the current Battlegrounds match.

Examples include:

- gold spent;
- Tavern refreshes;
- free refreshes gained;
- cards bought;
- minions bought;
- Tavern spells bought;
- cards played;
- minions played;
- Tavern spells played;
- Battlecries played;
- Rally effects triggered;
- highest minion Attack;
- highest minion Health;
- highest combined minion stats;
- Tavern buff values when detectable;
- spell-related Tavern buff values when detectable;
- hero damage dealt;
- maximum hero damage dealt in one combat;
- hero damage taken;
- maximum hero damage taken in one combat;
- highest turn reached.

The statistics are shown in a compact WPF overlay during the match. The final summary remains visible after the match until the next match begins.

### This repository is not BGMMRPlugin

Never confuse this project with `BGMMRPlugin` or `HDT-BGMMRPlugin`.

This project does **not** primarily:

- display opponent player names;
- look up player MMR;
- display leaderboard MMR;
- display opponent Tavern Tiers beside avatars;
- mark the last opponent on the Battlegrounds leaderboard;
- track moving leaderboard avatar positions.

Those features belong to another plugin.

### This repository is not BoardStatsPlugin

Never assume this project is `BoardStatsPlugin`.

This plugin tracks cumulative match statistics. It is not primarily a board layout or board-only statistics plugin.

When writing documentation, commit messages, comments, release notes, or code, always refer to the correct project:

```text
Repository: HDT-FinalStatsPlugin
Plugin concept: Battlegrounds cumulative match statistics
HDT display name: Battlegrounds Final Stats
```

---

## 3. Canonical DLL and release name

The canonical compiled plugin filename is:

```text
HDT-FinalStatsPlugin.dll
```

This name must be used consistently in:

- the `.csproj` assembly output;
- `Build.bat`;
- the `bin` build result;
- the `dist` copy;
- installation instructions;
- README documentation;
- release archives;
- release notes;
- GitHub releases;
- diagnostic messages referring to the distributed DLL.

The old output filename:

```text
FinalStatsPlugin.dll
```

must not remain as the distributed build artifact.

### Required project configuration

The project file must ultimately contain:

```xml
<AssemblyName>HDT-FinalStatsPlugin</AssemblyName>
```

The source namespace may remain:

```csharp
namespace FinalStatsPlugin
```

Changing the DLL filename does not require renaming the C# namespace or every internal type.

Do not perform a broad namespace rename only to change the output DLL name.

### Required build paths

Expected Release build output:

```text
bin\Release\HDT-FinalStatsPlugin.dll
```

Expected distribution output:

```text
dist\HDT-FinalStatsPlugin.dll
```

`Build.bat` must:

1. compile the project;
2. verify that `bin\Release\HDT-FinalStatsPlugin.dll` exists;
3. copy it to `dist\HDT-FinalStatsPlugin.dll`;
4. report that exact path to the user.

Do not allow `Build.bat` to check one filename, copy another filename, and print a third filename.

After any build-script or project-file change, search the repository for stale references:

```text
FinalStatsPlugin.dll
```

Any remaining occurrence must be reviewed and either updated or intentionally documented.

---

## 4. Current repository structure

Current primary files:

```text
AGENTS.md
FinalStatsPlugin.sln
FinalStatsPlugin.csproj
FinalStatsPlugin.cs
FinalBoardSummaryOverlay.cs
Build.bat
find_hdt_assembly.ps1
README.md
LICENSE.txt
.gitignore
lib/
dist/
```

### `FinalStatsPlugin.cs`

This is currently the main source file.

It contains most of the plugin logic, including:

- the `IPlugin` implementation;
- HDT lifecycle handling;
- game start and game end handling;
- match state;
- combat state;
- cumulative counters;
- entity transition tracking;
- `Power.log` parsing;
- shop purchase detection;
- Tavern refresh tracking;
- hero damage tracking;
- WPF overlay construction;
- overlay value updates;
- Show/Hide button behavior;
- diagnostic logging.

Do not assume the source file has already been split into multiple services.

### `FinalBoardSummaryOverlay.cs`

This file contains the separate WPF overlay used to display the last intact
Battlegrounds minion board after the match.

It reuses HDT's public `BattlegroundsMinion` control and must remain
non-interactive.

### `FinalStatsPlugin.csproj`

Current technical target:

```text
TargetFramework: net472
OutputType: Library
UseWPF: true
PlatformTarget: x64
Platforms: x64
LangVersion: 10
```

The project references local HDT assemblies:

```text
lib\HearthstoneDeckTracker.exe
lib\HearthDb.dll
```

These assemblies are local build dependencies and must not be committed.

### `FinalStatsPlugin.sln`

The solution currently references:

```text
FinalStatsPlugin.csproj
```

The project file and solution file do not need to be renamed solely because the output DLL is named `HDT-FinalStatsPlugin.dll`.

### `Build.bat`

The Windows build script:

1. accepts an HDT installation directory or `HearthstoneDeckTracker.exe`;
2. invokes `find_hdt_assembly.ps1`;
3. locates the real managed HDT assembly;
4. copies HDT dependencies to `lib`;
5. locates MSBuild;
6. compiles `Release|x64`;
7. copies the final DLL to `dist`.

### `find_hdt_assembly.ps1`

This script searches for the real managed:

```text
HearthstoneDeckTracker.exe
```

It must avoid selecting a non-managed launcher or unrelated executable.

### `README.md`

Public GitHub documentation.

It must describe this plugin as a cumulative Battlegrounds statistics tracker and use:

```text
HDT-FinalStatsPlugin.dll
```

in installation instructions.

### `AGENTS.md`

Development instructions for Codex and other coding agents.

Do not add end-user installation instructions here unless they affect development or release validation.

---

## 5. Technical constraints

Required platform:

- Windows;
- .NET Framework 4.7.2;
- WPF;
- x64;
- C# 10;
- Hearthstone Deck Tracker plugin API;
- HearthDb.

Do not migrate the project to:

- .NET 6, 7, 8, 9, or later;
- WinUI;
- Avalonia;
- Electron;
- a standalone desktop application;
- a Windows service;

unless the user explicitly requests and approves that architectural change.

Do not add a NuGet dependency unless it is clearly necessary and approved.

Prefer framework APIs and existing HDT APIs.

The normal plugin must not require Internet access.

The plugin must never modify the user's HDT installation beyond normal plugin loading and local build dependency copying performed by `Build.bat`.

Never commit:

- personal absolute paths;
- Windows usernames;
- access tokens;
- private identifiers;
- local HDT binaries;
- generated DLLs;
- PDB files;
- personal logs;
- temporary files.

---

## 6. User communication

The main user is French-speaking and is a beginner in C#/.NET development.

Source code, identifiers, technical comments, and this file may be written in English.

Reports to the user should normally be written in French.

When reporting work:

- explain exactly what changed;
- avoid unnecessary jargon;
- give exact commands when manual action is required;
- distinguish between code review, compilation, and in-game testing;
- never claim that a build succeeded unless it was actually run successfully;
- never claim that a statistic is correct without either automated evidence or an in-game test;
- clearly list remaining manual tests;
- explain errors in a way a beginner can follow.

Do not provide only isolated snippets when the task requires a complete repository change. Modify the appropriate files coherently.

---

## 7. Build instructions

Preferred Windows build command from the repository root:

```bat
Build.bat "PATH_TO_HDT_INSTALLATION"
```

The argument may also be the full path to:

```text
HearthstoneDeckTracker.exe
```

Non-interactive execution:

```bat
cmd /c Build.bat "PATH_TO_HDT_INSTALLATION"
```

Expected final artifact:

```text
dist\HDT-FinalStatsPlugin.dll
```

Before building, ensure that the build script can obtain:

```text
lib\HearthstoneDeckTracker.exe
lib\HearthDb.dll
```

### Build failure procedure

When compilation fails:

1. read the complete MSBuild output;
2. identify the first real compiler or project error;
3. fix that error rather than hiding it;
4. rebuild;
5. verify the exact DLL path;
6. report the real result.

Do not suppress errors simply to produce a file.

If the environment does not have Windows, MSBuild, Visual Studio Build Tools, or the HDT assemblies:

- run all possible static checks;
- inspect the diff carefully;
- state explicitly that the HDT build was not executed;
- do not fabricate a successful build result.

---

## 8. General code-change rules

### 8.1 Prefer small changes

Make changes that are:

- focused;
- isolated;
- reviewable;
- reversible;
- testable.

Do not combine an unrelated refactor with a bug fix.

Do not rewrite the entire plugin to add one counter.

### 8.2 Protect stable statistics

A task involving one statistic must not silently change the behavior of other statistics.

Examples:

- a hero damage fix must not alter gold tracking;
- an overlay layout change must not alter counter calculations;
- JSON storage must not change the values displayed by the current overlay;
- dashboard work must not change HDT event subscriptions unless required.

### 8.3 Avoid unsafe global replacements

Never use broad text replacement without reviewing every result.

Before renaming a method, field, class, or output filename:

1. search all occurrences;
2. inspect declarations and call sites;
3. change only intended references;
4. review the final diff;
5. search for stale and duplicated names.

Avoid accidental names such as:

```text
TryTryExtractEntityId
```

### 8.4 Preserve readability

Use:

- 4-space indentation;
- braces on separate lines;
- `PascalCase` for types and methods;
- `_camelCase` for private fields;
- clear local variable names;
- explicit state transitions;
- short useful comments;
- `CultureInfo.InvariantCulture` for technical serialization and stable numeric formats.

Do not introduce `dynamic` unless there is no reasonable typed alternative.

### 8.5 Error handling

The plugin must not crash HDT.

For frequent update paths:

- catch exceptions at an appropriate boundary;
- log useful context;
- avoid empty `catch` blocks except inside the final diagnostic fallback itself;
- do not throw intentionally from `OnUpdate()`;
- do not perform slow blocking work every 100 ms.

---

## 9. HDT lifecycle and match state

Current HDT events include:

```text
GameEvents.OnGameStart
GameEvents.OnGameEnd
GameEvents.OnInMenu
GameEvents.OnEntityWillTakeDamage
```

Current lifecycle and tracking methods include:

```text
OnLoad
OnUnload
OnButtonPress
OnUpdate
HandleGameStart
HandleGameEnd
HandleInMenu
BeginMatch
FinishMatch
ResetStatistics
TrackMatch
```

Important state fields include:

```text
_loaded
_pluginVisible
_trackingMatch
_hasMatchData
_gameEndObserved
_showingFinalSummary
_newGameEventPending
_previousCombatPhase
```

Rules:

- subscribe once during plugin loading;
- do not accidentally subscribe the same handler multiple times;
- make match start and match finish operations idempotent;
- avoid double initialization;
- avoid double finalization;
- preserve the final summary until the next real match begins;
- do not reset statistics merely because the game briefly changes state;
- remember that `OnUpdate()` runs frequently;
- do not scan the complete `Power.log` or write large files on every update.

Required lifecycle scenarios:

1. HDT starts in the menu;
2. plugin is enabled;
3. a Battlegrounds match begins;
4. the first Tavern phase begins;
5. multiple Tavern and combat phases occur;
6. a combat ends in a win;
7. a combat ends in a loss;
8. a combat ends in a draw;
9. the match ends normally;
10. the game returns to the menu;
11. the final summary remains visible;
12. a new match starts;
13. old values are reset only at the correct time;
14. the plugin is disabled and enabled again;
15. HDT is restarted.

---

## 10. WPF overlay behavior

Current reference layout:

```text
PanelWidth: 250
PanelHeight: 750
PanelRight: 15
PanelBottom: 50
ToggleButtonHeight: 30
ToggleButtonGap: 6
StatRowHeight: 23
CategoryHeaderHeight: 20
```

Current final-board summary layout:

```text
Width: 900
Height: 220
Left: 305
Bottom: 100
Top corner radius: 0
Bottom corner radius: 36
```

Current visual direction:

- compact graphite background;
- Segoe UI with display-optimized rendering;
- tabular lining digits for statistic values;
- restrained gold accents;
- semantic colors for combat wins, losses, and draws;
- subtle borders and category dividers.

### During a Battlegrounds match

- the Show/Hide button is visible;
- the overlay can be hidden;
- the overlay can be shown again;
- the visibility preference remains stable during the match.

Current button texts:

```text
Hide combat stats
Show combat stats
```

### After the match

- the final statistics panel is forced visible;
- the Show/Hide button is hidden;
- the button's interactive hit-test area is disabled;
- final statistics remain visible until the next match.

### Next match

- the button becomes available again;
- the normal in-match visibility behavior resumes.

### WPF and HDT safety rules

- create and update WPF controls through `Core.OverlayCanvas.Dispatcher`;
- do not update WPF controls from a non-UI thread;
- do not recreate the entire overlay every update;
- keep the statistics panel non-interactive;
- register only the intended button as overlay-hit-test-visible;
- unregister the button's hit-test area when hidden or removed;
- never make the whole overlay intercept Hearthstone clicks;
- preserve normal, hover, and pressed button states;
- remove controls cleanly when unloading.

Use the HDT overlay helper appropriately:

```csharp
OverlayExtensions.SetIsOverlayHitTestVisible(element, true);
```

---

## 11. Current statistic semantics

Do not change the meaning, label, or counting method of an existing statistic without an explicit task.

Current categories include:

```text
Highest turn
Gold spent
Tavern rolls
Free rolls gained
Battlecries played
Rally triggered
Cards bought
Minions bought
Spells bought
Played cards
Played minions
Played spells
Highest creature
Highest ATK
Highest HP
Tavern buff max
Spell power buff
Hero damage dealt
Max damage dealt
Hero damage taken
Max damage taken
Combat wins
Combat losses
Combat draws
Match duration
```

### 11.1 Gold spent

Current intended method:

- observe `RESOURCES_USED`;
- add only positive increases;
- treat a decrease as a new baseline;
- do not count minion sales as spending;
- do not add the current value again merely because the turn changed.

Do not restore `NUM_RESOURCES_SPENT_THIS_GAME` as the sole source unless it is proven reliable for the targeted Hearthstone and HDT versions.

Required tests:

- buy a minion;
- buy a Tavern spell;
- refresh the Tavern;
- upgrade the Tavern;
- sell a minion;
- gain extra gold;
- spend gold after receiving extra gold;
- change turns.

### 11.2 Tavern rolls

The statistic must count actual Tavern refresh actions.

It must distinguish where possible between:

- paid refreshes;
- free refreshes used;
- free refreshes gained.

Do not infer a refresh only from an unrelated resource change.

Required tests:

- normal paid refresh;
- refresh with one free refresh available;
- multiple free refreshes;
- refresh immediately after a turn transition;
- actions occurring quickly;
- no duplicate count from one refresh.

### 11.3 Cards bought

Current purchase detection is based on shop entity tracking and entity movement toward the player's hand.

It should:

- remember known shop entity IDs;
- identify the same entity entering the player's hand;
- classify minions and Tavern spells;
- avoid counting the same entity twice.

Known limitation:

- a card obtained for free directly from the shop may look similar to a purchase.

Required tests:

- buy a minion;
- buy a Tavern spell;
- buy several cards quickly;
- buy the third copy that immediately creates a golden triple;
- receive a shop card for free;
- sell a card after buying it.

### 11.4 Played cards

Played cards, minions, and Tavern spells may use multiple HDT and `Power.log` signals.

Rules:

- deduplicate by entity ID where possible;
- do not count automatic effects as player-played cards;
- do not count the same Tavern spell through both a tag and a log line;
- keep explicit fallback counters separate until the final result is selected.

### 11.5 Battlecries and Rally

Only count effects that match the intended Battlegrounds mechanic.

Do not count unrelated triggered effects merely because they use a general trigger block.

When Hearthstone introduces new mechanics or changes logging behavior:

1. add targeted diagnostics;
2. collect evidence;
3. test several cards;
4. then update the counter.

### 11.6 Highest minion statistics

Track:

- highest Attack reached by one relevant minion;
- highest Health reached by one relevant minion;
- highest combined Attack and Health;
- the Attack and Health of the minion that achieved the highest combined total.

Do not combine Attack from one minion and Health from another to create a fake “highest creature.”

Exclude irrelevant temporary or non-board entities when appropriate.

### 11.7 Tavern buffs

The Tavern buff counters depend on data exposed by Hearthstone and HDT.

Known issue:

- HDT's own Tavern buff information may stop working after a Hearthstone patch.

Rules:

- do not invent a value;
- do not sum every generic `TAG_SCRIPT_DATA_NUM_1` or `TAG_SCRIPT_DATA_NUM_2`;
- these tags can belong to unrelated entities;
- add targeted diagnostics before changing the calculation;
- record the entity ID, card ID, controller, zone, tags, and phase;
- validate the result in real matches.

A missing value is better than a confident but false value.

### 11.8 Hero combat damage

Current source:

```text
GameEvents.OnEntityWillTakeDamage
PREDAMAGE
```

Mandatory filtering:

- plugin loaded;
- active tracked Battlegrounds match;
- current Battlegrounds combat phase;
- positive damage value;
- target is a hero;
- target entity ID exactly matches the active hero entity referenced by the relevant player's `HERO_ENTITY` tag.

Why exact hero matching matters:

- Battlegrounds can expose multiple hero-like entities;
- leaderboard or visual copies may exist;
- one impact may produce duplicate-looking signals.

Current intended behavior:

- retain the largest valid `PREDAMAGE` value for each side during one combat;
- finalize the combat once;
- add that value once to the match total;
- update the single-combat maximum;
- reset the per-combat snapshot.

A draw should produce:

```text
damage dealt: 0
damage taken: 0
```

Do not return to a naive calculation based only on:

```text
DAMAGE delta + ARMOR delta
```

That older approach can count an opponent's armor reset as damage after a draw.

Required tests:

- win with no opponent armor;
- win against armor;
- loss with player armor;
- draw;
- lethal overkill;
- multiple consecutive combats;
- final combat ending the match;
- no duplicated values.

---

## 12. Power.log processing

The plugin processes selected lines from:

```text
Core.Game.PowerLog
```

Rules:

- process only new lines using `_processedPowerLogLines`;
- do not rescan the full log every update;
- protect against incomplete lines;
- use entity IDs, card IDs, tags, zones, and controllers where possible;
- avoid depending on localized card names;
- keep regex patterns focused;
- avoid expensive or catastrophic regex patterns;
- deduplicate events from multiple log sources;
- reset the processed-line state only at the correct lifecycle point.

When a parser changes, test against real log samples if available.

Do not assume Hearthstone logging is stable across patches.

---

## 13. Diagnostics

Current diagnostic file:

```text
FinalStatsPlugin_debug.log
```

The diagnostic filename may remain independent from the distributed DLL filename unless a dedicated renaming task is requested.

Diagnostic rules:

- diagnostics must never crash HDT;
- the final logging method must be protected by `try/catch`;
- log transitions and decisions, not only generic errors;
- avoid writing the same message every 100 ms;
- use structured lines for fragile trackers;
- do not commit personal log files;
- reduce excessive temporary logging after a bug is fixed.

Preferred format:

```text
EVENT NAME | key=value | key=value
```

Example:

```text
HERO PREDAMAGE DEALT | target=42 | value=8 | combatMax=8
```

Useful diagnostics for a tracker should explain:

- what event occurred;
- which entity was involved;
- why it was accepted or rejected;
- which value was stored;
- whether it changed a total or maximum.

---

## 14. Versioning

The plugin version is defined in the `IPlugin` implementation, currently in a form similar to:

```csharp
public Version Version => new Version(MAJOR, MINOR, PATCH);
```

Rules:

- increment the version for every distributed or user-tested build;
- bug fixes and small features normally increment `PATCH`;
- do not silently replace an already distributed version with different code;
- keep version references consistent across code, documentation, changelog, release title, and archive name;
- do not increment the version for analysis-only work with no file changes.

Suggested release naming:

```text
HDT-FinalStatsPlugin_v0.1.24.zip
```

The archive should contain the correctly named:

```text
HDT-FinalStatsPlugin.dll
```

---

## 15. Git and GitHub rules

Repository:

```text
Reign-in-blood/HDT-FinalStatsPlugin
```

Default branch currently used by the repository:

```text
master
```

Before editing:

```bash
git status
git branch --show-current
```

After editing:

```bash
git diff --check
git diff
git status
```

Rules:

- do not overwrite unrelated user changes;
- do not delete untracked files without permission;
- do not use `git reset --hard`;
- do not force-push;
- do not rewrite published history;
- use a separate branch for significant features when practical;
- do not commit, push, or open a pull request unless the user asks;
- keep commits focused and descriptive.

Example commit messages:

```text
fix: use the canonical HDT-FinalStatsPlugin DLL name
fix: prevent duplicate hero damage counting
feat: record local match history
feat: add static local statistics dashboard
docs: clarify build and installation steps
```

Do not commit:

```text
bin/
obj/
dist/*.dll
lib/HearthstoneDeckTracker.exe
lib/HearthDb.dll
*.pdb
*.log
.vs/
```

Respect `.gitignore`.

---

## 16. Required checks before delivery

For every C# change:

1. inspect the complete diff;
2. run `git diff --check`;
3. verify braces and syntax;
4. verify declarations and call sites;
5. search for accidental duplicate names;
6. compile with `Build.bat` when possible;
7. confirm creation of:

```text
dist\HDT-FinalStatsPlugin.dll
```

8. confirm that the old distributed DLL name was not regenerated;
9. list all changed files;
10. update version and documentation when distributing a test build.

### For DLL naming changes

Search for both:

```text
FinalStatsPlugin.dll
HDT-FinalStatsPlugin.dll
```

Expected final state:

- internal project filenames may remain `FinalStatsPlugin.*`;
- source namespace may remain `FinalStatsPlugin`;
- distributed artifact must be `HDT-FinalStatsPlugin.dll`;
- README installation instructions must use `HDT-FinalStatsPlugin.dll`;
- Build.bat must verify, copy, and print `HDT-FinalStatsPlugin.dll`;
- project `<AssemblyName>` must be `HDT-FinalStatsPlugin`.

### For overlay changes

Test:

- visible panel;
- hidden panel;
- Show/Hide button;
- button hover and press;
- no blocked Hearthstone clicks;
- final summary in the menu;
- next-match reset;
- at least one common screen resolution;
- additional resolutions when layout changes are substantial.

### For statistic changes

Test:

- first occurrence;
- repeated occurrence;
- zero occurrence;
- fast consecutive events;
- turn transition;
- combat transition;
- match end;
- menu return;
- new match without restarting HDT;
- no double counting.

If in-game testing is not possible, provide a precise checklist for the user.

---

## 17. Definition of done

A task is complete only when:

- the requested behavior is implemented;
- unrelated stable behavior was preserved;
- the diff is focused and clean;
- naming is consistent;
- compilation succeeded, or the inability to compile is explicitly stated;
- executed tests are listed;
- remaining manual tests are listed;
- version and documentation are coherent when required;
- no generated or private files were committed;
- the report is understandable to a beginner.

---

## 18. Planned local match history

A future goal is to store structured local match and combat history.

This is planned functionality, not a reason to rewrite the current plugin prematurely.

### Design principles

- no companion `.exe`;
- no Windows service;
- no mandatory local HTTP server;
- no open network port by default;
- no telemetry;
- no cloud upload;
- no remote account;
- offline operation;
- readable local files;
- failure of history storage must not break the live overlay.

### Canonical storage format

Use JSON as the source of truth.

Suggested structure:

```text
Dashboard/
├── index.html
├── style.css
├── app.js
├── data.js
└── Data/
    ├── games-index.json
    └── games/
        ├── game-YYYY-MM-DD-HH-mm-ss-ID.json
        └── ...
```

Suggested match fields:

```text
schemaVersion
pluginVersion
gameId
startedAt
endedAt
heroCardId
heroName
placement
turnCount
duration
finalStats
combats
```

Suggested combat fields:

```text
turn
opponent
result
damageDealt
damageTaken
tavernTier
turnStats
playerBoard
opponentBoard
```

Do not store uncertain values as facts.

Use:

```text
null
unknown
```

when the data cannot be determined reliably.

### Schema compatibility

Every canonical JSON root should include:

```json
{
  "schemaVersion": 1
}
```

Rules:

- add new optional fields compatibly when possible;
- increment `schemaVersion` for incompatible changes;
- tolerate missing fields when reading old files;
- document migrations;
- never silently rewrite old data with a different meaning.

### Safe file writing

Use atomic writes:

1. write a temporary file;
2. flush and close it;
3. rename or replace the final file.

Do not write large JSON files every 100 ms.

Collect data in memory and persist at meaningful transitions.

A file-system error must:

- be logged;
- leave the live overlay running;
- not crash HDT;
- not block `OnUpdate()` repeatedly.

### Privacy

By default:

- all data remains local;
- do not save a full BattleTag unless required;
- allow future opponent-name anonymization;
- do not store personal absolute paths;
- do not load remote scripts;
- do not send match data externally.

---

## 19. Planned local HTML dashboard

The dashboard must use local web technologies only:

```text
HTML
CSS
JavaScript
JSON
```

Do not create a Java `.java` file.

Do not create a companion executable.

### Local file loading

Browsers may restrict `fetch()` for pages opened through `file://`.

Recommended design:

- keep `.json` files as canonical data;
- generate a local `data.js` file for the static page;
- expose a controlled global variable:

```javascript
window.FINAL_STATS_DATA = {
    schemaVersion: 1,
    games: []
};
```

Load it before the main script:

```html
<script src="data.js"></script>
<script src="app.js"></script>
```

Do not depend on a CDN.

If a chart library is included:

- distribute it locally;
- verify its license;
- record its source and exact version;
- avoid a large dependency when native SVG or Canvas is sufficient.

### Opening the dashboard from HDT

The HDT `IPlugin.MenuItem` property can later expose a menu such as:

```text
Plugins
└── Battlegrounds Final Stats
    ├── Open statistics dashboard
    └── Open data folder
```

When opening the dashboard:

1. verify required files exist;
2. regenerate or update `data.js`;
3. open `index.html` with the default browser;
4. handle missing files safely;
5. log errors without crashing HDT.

Do not repurpose the current overlay Show/Hide button when a dedicated plugin menu is more appropriate.

### Initial dashboard scope

Start with validated data only:

- total matches;
- average placement;
- average duration;
- average selected match statistics;
- recent match list;
- individual match details;
- hero damage dealt and taken by combat;
- gold spent by turn.

Do not build advanced graphs before validating the underlying data.

---

## 20. Strategy for new statistics

For every new statistic:

1. define its exact meaning;
2. identify the HDT or `Power.log` source;
3. determine when the value becomes available;
4. identify the relevant entity and controller;
5. add temporary targeted diagnostics;
6. test multiple matches;
7. test zero and unusual cases;
8. remove excessive diagnostics;
9. only then expose or persist the statistic.

Do not mix:

- lifetime or historical totals;
- current match cumulative totals;
- current turn deltas;
- current combat values;
- maximum values.

For per-turn data, preserve:

```text
value before turn
value after turn
turn delta
```

For per-combat data, use a dedicated combat object finalized once.

---

## 21. Gradual refactoring

The main source file is currently large.

A future gradual structure may include:

```text
Plugin.cs
Tracking/MatchTracker.cs
Tracking/CombatTracker.cs
Tracking/PowerLogTracker.cs
Tracking/PurchaseTracker.cs
Models/MatchHistory.cs
Models/CombatHistory.cs
Storage/JsonHistoryStore.cs
Dashboard/DashboardGenerator.cs
Overlay/StatsOverlay.cs
Diagnostics/DiagnosticLogger.cs
```

This is a direction, not a command to refactor everything immediately.

Rules:

- extract one responsibility at a time;
- preserve behavior during extraction;
- compile after each meaningful extraction;
- do not hide logic changes inside a refactor;
- do not create unnecessary abstraction;
- do not split the project into many files without a clear benefit.

The future JSON history feature is a reasonable point to begin introducing separate models and storage classes.

---

## 22. Project priorities

Priority order:

1. never crash HDT;
2. never block Hearthstone interactions;
3. report accurate statistics;
4. prevent double counting;
5. preserve correct state across phases;
6. maintain HDT compatibility;
7. produce `HDT-FinalStatsPlugin.dll` reproducibly;
8. keep the code understandable;
9. maintain a clean overlay;
10. add advanced features.

When priorities conflict, choose stability and accuracy over additional features.
