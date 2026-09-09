# Logging

How log messages are filtered in Stride, and how to see them while working on the engine or Game
Studio. For crashes, see [crash-reporting.md](crash-reporting.md).

## Two filters

A message passes two filters before it is displayed.

1. **The logger that emits it.** Every `Logger` has a minimum level (`ActivateLog`). Below it the
   message is never created, so this is the cheap filter. Global loggers (`GlobalLogger.GetLogger(module)`)
   start at Info, from `Logger.MinimumLevelEnabled`. `GlobalLogger.SetModuleLevel(module, level)` sets
   the level of a module's logger, whether it already exists or is created later.
2. **The listener that displays it.** Every `LogListener` has a `MinimumLevel` (default Debug, so it
   lets everything through) and `ModuleLevels`, per-module overrides keyed by the message's `Module`.
   A listener can stay quiet overall while following a few modules in detail.

Both are needed: the emitter level saves the cost of messages nobody wants, the listener level lets
several sinks (console, debugger, a log pane) show different amounts of the same stream.

Severity and module are metadata, so filters act on them. How much of that metadata gets printed is
the formatter's choice.

## Game Studio

Game Studio routes everything logged through `GlobalLogger` to the attached debugger's output
(the VS Output pane), in Debug builds only. What reaches it is configured in
`%APPDATA%\stride\GameStudioSettings.conf`, under the `Logging` keys. They are read once at startup,
so a change needs a restart.

| Key | Default | In the Settings dialog |
|---|---|---|
| `Logging/DebugOutputLevel` | `Warning` | yes, under Logging |
| `Logging/DebugOutputModuleLevels` | `GraphicsDevice: Debug`, `GraphicsDebug: Debug` | no, file only |
| `Logging/SourceModuleLevels` | `AssetBuilderService`, `EffectCompilerCache`, `Preview`, `Quantum`: `Debug` | no, file only |

- `DebugOutputLevel` is the general level of the debugger output listener. Warning by default because
  Info and Verbose volume, during an asset build or a NuGet restore, slows the debugger noticeably.
- `DebugOutputModuleLevels` overrides it per module. The two graphics modules default to Debug: that is
  where the backends report the validation layer status and messages, and they are quiet without a
  debug device (`/DebugEditorGraphics`).
- `SourceModuleLevels` sets the emitting level of global loggers. The defaults cover the loggers shown
  in the debug window (Help > Show debug window), so those pages get their full history.

The file is sparse: only what you set is written, everything else keeps its default. The two maps
merge over their defaults, so one entry overrides one module and the others stay as they are.
To silence a default entry, set it explicitly to a higher level.

Levels are `Debug`, `Verbose`, `Info`, `Warning`, `Error`, `Fatal`.

### Examples

See Info from everything, and follow the build engine step by step:

```yaml
!SettingsFile
Settings:
    Logging/DebugOutputLevel: Info
    Logging/DebugOutputModuleLevels:
        BuildStep: Debug
```

Keep the default Warning level but silence the graphics modules:

```yaml
    Logging/DebugOutputModuleLevels:
        GraphicsDevice: Warning
        GraphicsDebug: Warning
```

Make a global logger emit Debug so its debug page shows everything, or so it can be followed in the
debugger output:

```yaml
    Logging/SourceModuleLevels:
        Preview: Debug
    Logging/DebugOutputModuleLevels:
        Preview: Debug
```

Both entries are needed, one per filter. The `Preview` logger starts at Info, so without the first
its Debug calls return without creating a message, and no listener setting can show what was never
emitted. Without the second, the debugger output listener drops the messages, since its general
level is Warning.

### Build pane

The Build pane shows what MSBuild reports for the project build, relayed with its severity and
location: errors and warnings appear as `file(line,col): CODE: text`. The asset compiler runs with
`--verbose` under Game Studio and its output travels as Verbose, hidden by default; turn on the Verbose
toggle to see it. Its errors and warnings keep their severity, so they show in colour regardless.

## Games

A `Game` attaches a `ConsoleLogListener` to `GlobalLogger`. `Game.ConsoleLogLevel` sets its level.
`ConsoleLogMode` decides whether a console window is opened for it: `Always`, `None`, or `Auto`,
which opens one for Debug builds when no debugger is attached. An attached debugger receives every
message that passes the level, whatever the mode.
