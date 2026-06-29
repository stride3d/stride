# Stride `dotnet new` templates

C# project templates for [Stride](https://stride3d.net), an open-source 3D game engine. Use them to scaffold a new Stride game from the command line.

```bash
dotnet new install Stride.Templates.Games
dotnet new stride-game -n MyGame
cd MyGame && dotnet run --project MyGame.Windows
```

## Packages

| Package | Templates | Notes |
|---|---|---|
| `Stride.Templates.Games` | `stride-game` | Blank starter; bundled with the GameStudio installer |
| `Stride.Templates.Games.Starters` | `stride-fps`, `stride-platformer2d`, `stride-topdownrpg`, `stride-thirdpersonplatformer`, `stride-vrsandbox` | Opinionated genre starters |
| `Stride.Templates.Samples` | 18 feature demos (tutorials, graphics, physics, UI, particles, input, audio…) | Self-contained samples |

`dotnet new -l` after installing any of these lists every available `stride-*` short name.

## Common parameters

| Parameter | Values | Meaning |
|---|---|---|
| `-n` / `--name` | string | Project name; substituted everywhere `MyTemplate` would appear |
| `--platforms` | `host` / `windows` / `linux` / `macos` / `ios` / `android` (repeat for multiple) | Per-platform exec projects to include. `host` auto-detects current OS. |
| `--HDR` | `true` / `false` | HDR rendering pipeline (requires `--graphicsProfile >= 10.0`) |
| `--graphicsProfile` | `9.0` / `10.0` / `11.0` | Shader feature level |
| `--orientation` | `Default` / `LandscapeLeft` / `LandscapeRight` / `Portrait` | Mobile display orientation |

Per-template parameter list: `dotnet new <template> --help`.

## Links

- [Stride homepage](https://stride3d.net)
- [GitHub repository](https://github.com/stride3d/stride)
- [Documentation](https://doc.stride3d.net)
- [Community Discord](https://discord.gg/stride3d)
