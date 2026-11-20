# maui-weather

A weather app in .NET MAUI: search for a place, see its forecast. One of four
builds of the same app, alongside
[flutter-weather](https://github.com/calemccammon/flutter-weather),
[kmp-weather](https://github.com/calemccammon/kmp-weather) and
[react-native-weather](https://github.com/calemccammon/react-native-weather),
to compare the frameworks.

The repository holds the project set-up only. The code is what the
`dotnet new` templates generate (`maui`, `classlib`, `xunit`); the app is still
to be written. The work is laid out as [milestones](../../milestones) and
[issues](../../issues).

## Layout

| Path | What it is |
| --- | --- |
| `src/MauiWeather` | The MAUI app: pages, XAML, platform folders |
| `src/MauiWeather.Core` | Plain .NET library for models, services and view models, so they can be unit-tested without a device |
| `tests/MauiWeather.Core.Tests` | xUnit tests for the core library |

## Platforms

MAUI targets Android, iOS, macOS (Mac Catalyst) and Windows. From Linux only
Android can be built; iOS and macOS need a Mac, Windows needs Windows. There
is no web target and no Linux desktop target, so unlike the other three
repositories this one has no web build and no GitHub Pages workflow.

## Data

- Place search and forecasts: [Open-Meteo](https://open-meteo.com/) (no key;
  free for non-commercial use; CC BY 4.0, attribution required).
- Map (map milestone): [Mapsui](https://mapsui.com/) over OpenStreetMap
  tiles. No key. MapLibre has no maintained MAUI control, which is why this
  repository differs from the other three here.

## Libraries already added

| Purpose | Package |
| --- | --- |
| MVVM (observable properties, commands) | `CommunityToolkit.Mvvm` |
| HTTP client factory | `Microsoft.Extensions.Http` |
| JSON | `System.Text.Json` (part of .NET) |
| Saved places, settings, device location | MAUI's own `Preferences` and `Geolocation` |
| Map | `Mapsui.Maui` |
| Tests | `xunit`, `NSubstitute` |

Any of these can be swapped; nothing depends on them yet.
`CommunityToolkit.Maui` (extra controls and behaviours) is deliberately not
installed: it requires a line of start-up code in `MauiProgram.cs`. Version
16.0.0 is the one for .NET 10.

## Running

.NET 10 SDK (pinned in `global.json`) with the `maui-android` workload.

```sh
dotnet build src/MauiWeather -f net10.0-android -t:Run   # build, install and start on the emulator
dotnet test tests/MauiWeather.Core.Tests
dotnet format                                            # fix formatting
dotnet format --verify-no-changes                        # what CI checks
```

Start the emulator first with `emulator -avd Pixel_API_36`.

## Static analysis

`Directory.Build.props` turns on the .NET analyzers at their recommended
level, checks the code style rules in `.editorconfig` during the build, and
treats every warning as an error. So a normal build is also the lint run.

## CI

`.github/workflows/ci.yml` runs on pushes to `main` and on pull requests:
format check, analyzers and tests for the core library, then the same for the
app and an Android debug APK uploaded as a workflow artifact.
