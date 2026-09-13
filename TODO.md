# TODO — HDT-FinalStatsPlugin

## Diagnostic log location

Current diagnostic log:

```text
FinalStatsPlugin_debug.log
```

Current location observed with HDT 1.57.8:

```text
%LOCALAPPDATA%\HearthstoneDeckTracker\app-1.57.8\Plugins\FinalStatsPlugin_debug.log
```

The current implementation writes the log next to the assembly actually loaded by HDT by using `typeof(Plugin).Assembly.Location`.

HDT synchronizes plugins into its versioned local application directory before loading them, so the diagnostic file ends up inside an `app-X.Y.Z\Plugins` folder. This location is difficult to find and can change when HDT updates.

### TODO

- Rework the diagnostic log location so it uses a stable per-user HDT/plugin data directory instead of the versioned HDT application folder.
- Prefer an HDT-provided stable path/API when available; otherwise use a dedicated stable folder below HDT AppData.
- Do not hard-code a Windows username, drive letter, or HDT version.
- Keep diagnostic failures non-fatal to HDT.
- Consider basic log cleanup/rotation if the diagnostic log remains enabled for normal builds.

Do not change this while debugging the current Duos final-board feature unless the log location itself blocks testing.
