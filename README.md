# maui-weather

A weather app in .NET MAUI, and the same app again in the browser with
Blazor WebAssembly. One screen: a map with a Material 3 search bar over it.
Search for a place, or press and hold on the map, and the forecast for the
week appears.

The two front ends share everything below the views: the Blazor app was
added without changing the domain, data or presentation projects.

**Live demo (the Blazor web app):** <https://calemccammon.github.io/maui-weather/>

| Wide window                                                                                                 | Phone                                                                                         |
| ----------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| ![The app on a wide window: a side panel with the week's forecast beside the map](docs/screenshot-wide.png) | ![The app on a phone: the forecast in a bottom sheet over the map](docs/screenshot-phone.png) |

It is one of six cross-platform builds of the same app, written to compare
the frameworks. The others are
[flutter-weather](https://github.com/calemccammon/flutter-weather),
[kmp-weather](https://github.com/calemccammon/kmp-weather),
[react-native-weather](https://github.com/calemccammon/react-native-weather),
[ionic-weather](https://github.com/calemccammon/ionic-weather) and
[capacitor-weather](https://github.com/calemccammon/capacitor-weather).
[android-weather](https://github.com/calemccammon/android-weather) is the
native Android build in Kotlin and Jetpack Compose, which goes further with
saved places, offline forecasts and a home-screen widget.

## What it does

- **Search as you type**, debounced; a late response to an older query never
  overwrites a newer one.
- **Pick a point** by pressing and holding on the map.
- **Forecast**: current conditions, then seven days with the chance of
  precipitation and a bar placing each day's low and high within the week's
  range.
- **Adapts to the window**: a bottom sheet on a phone, a side panel from
  840 dp wide (a phone on its side, a tablet). The map keeps the chosen
  place clear of both.
- **Celsius or Fahrenheit**, switched from the forecast and saved between
  launches. The default follows the device's region.
- **Light and dark** themes from one seed colour, following the system.
- **Accessible**: screen readers read each forecast block as one sentence,
  and loading and errors are announced.
- **Errors**: a timeout, no connection, a refused request and an
  overloaded service each get their own message and a Try again button.

## Architecture

Clean Architecture with one project per layer, so the compiler enforces the
dependency rule: a project can only use what it references.

| Project                        | References | What it holds                                                                                                                                                                                                                                        |
| ------------------------------ | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/MauiWeather.Domain`       | nothing    | The app's own types (`Place`, `Forecast`, `WeatherCondition`, `Units`), the repository interfaces (`IWeatherRepository` with its failure kinds, `ISettingsRepository`), and the use cases (`SearchPlaces`, `GetForecast`, `GetUnits`, `SetUnits`).    |
| `src/MauiWeather.Data`         | Domain     | Open-Meteo: the wire-shaped models (`*Dto`), their translation into domain types, and the repository implementation over `HttpClient`. Settings go into a key-value store the host app supplies.                                                         |
| `src/MauiWeather.Presentation` | Domain     | Display-ready models (`*UiModel`) and the two view models that produce them. All strings are in `Resources/Strings.resx`. It depends on neither the data layer nor MAUI, so it is tested without a device.                      |
| `src/MauiWeather`              | all three  | The MAUI app: the page and its views in XAML, and `MauiProgram`, the composition root that binds each interface to its implementation.                                                                                                           |
| `src/MauiWeather.Web`          | all three  | The Blazor WebAssembly app: the same page and views as Razor components, and `Program.cs`, which does for the browser what `MauiProgram` does for a device.                                                                                           |
| `tests/MauiWeather.Tests`      | the three libraries and the web app | xUnit tests for the domain, data and presentation layers, and bUnit tests for the Razor components.                                                                                                                          |

Each layer has its own model, with a mapping between them:

|              | Data                           | Domain       | Presentation                                                          |
| ------------ | ------------------------------ | ------------ | --------------------------------------------------------------------- |
| Type         | `ForecastDto`                  | `Forecast`   | `ForecastUiModel`                                                     |
| A day's high | `temperature_2m_max[1] = 64.3` | `High: 64.3` | `High: "64°"`, `BarEnd: 0.75`                                         |
| Conditions   | `weather_code: 3`              | `Overcast`   | an icon and "Overcast"                                                |
| A failure    | HTTP 400, a timeout            | `Rejected`   | `Strings.FailureRejected`: "The weather service refused the request." |

State is held by two view models, `HomeViewModel` and `SearchViewModel`,
built on the MVVM Toolkit's observable properties and commands; the XAML
binds to them with compiled bindings. A request that has been overtaken
(another place selected, more text typed) is cancelled, and its answer is
ignored if it still arrives.

In the MAUI app the map is [Mapsui](https://mapsui.com/) over
OpenStreetMap's tiles, since MapLibre has no maintained MAUI control. MAUI
has no bottom sheet either, so `BottomSheet` is the app's own.

### The web app

`src/MauiWeather.Web` holds only what a browser needs that a device does not:

| Path                 | What it holds                                                                                                                                                                        |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Views`              | `HomePage` and its components (`PlaceSearchBar`, `ForecastPanel`, `BottomSheet`, `WeatherMap`), each with a scoped style sheet. They render the view models' state.          |
| `Data/BrowserStore`  | The data layer's key-value store, backed by `localStorage`. On a device the MAUI app supplies one backed by its preferences.                                            |
| `wwwroot/js`         | The JavaScript interop: the map, drawn with [MapLibre GL JS](https://maplibre.org/), and small helpers for window size, theme changes, focus and pointer capture.                                            |
| `Program.cs`         | The composition root.                                                                                                                                                                  |

The view models raise `PropertyChanged`, which XAML bindings observe
directly. Blazor has no equivalent bindings, so `HomePage` subscribes to the
event and re-renders. That subscription is the only adaptation the view
models needed to run under a second UI framework.

MapLibre GL JS is fetched by [LibMan](https://learn.microsoft.com/aspnet/core/client-side/libman/)
when the project is built, at the version in `libman.json`.

## Running

.NET 10 SDK (pinned in `global.json`). The MAUI app also needs the
`maui-android` workload; the web app needs nothing more.

```sh
dotnet build src/MauiWeather -f net10.0-android -t:Run   # build, install and start on the emulator
dotnet run --project src/MauiWeather.Web                 # in a browser, at the address it prints
dotnet publish src/MauiWeather.Web -c Release -o publish # static site in publish/wwwroot
```

Start the emulator first with `emulator -avd Pixel_API_36`.

MAUI targets Android, iOS, macOS (Mac Catalyst) and Windows; only Android
can be built from Linux. MAUI has no web target, which is why the browser gets its own views.

## Checks

```sh
dotnet test tests/MauiWeather.Tests
dotnet format --verify-no-changes   # `dotnet format` fixes
```

`Directory.Build.props` turns on the .NET analyzers at their recommended
level, checks the code style rules in `.editorconfig` during the build, and
treats every warning as an error, so a normal build is also the lint run.

The MAUI views have no tests, since MAUI has no UI test framework that runs
without a device; the views only bind to the view models, which are tested.
The web app's views are tested with [bUnit](https://bunit.dev/), which
renders Razor components without a browser, with the JavaScript faked.

`.github/workflows/ci.yml` runs on pushes to `main` and on pull requests. It
runs the format check, analyzers and tests for the three libraries and the
web app, publishes the web app, then analyzes and builds the MAUI app. The
site and an Android debug APK are uploaded as workflow artifacts.

`.github/workflows/pages.yml` publishes the live demo. It runs only when
started by hand from the Actions tab.

## Data and credits

- Place search and forecasts: [Open-Meteo](https://open-meteo.com/) (no key;
  free for non-commercial use; CC BY 4.0).
- Map, MAUI app: tiles from [OpenStreetMap](https://www.openstreetmap.org/copyright)
  (© OpenStreetMap contributors), drawn with [Mapsui](https://mapsui.com/).
  OpenStreetMap's own tile servers are for development and light use only.
- Map, web app: [OpenFreeMap](https://openfreemap.org/) styles and tiles (no
  key), from [OpenMapTiles](https://openmaptiles.org/) and
  © OpenStreetMap contributors, drawn with [MapLibre GL JS](https://maplibre.org/).
- Icons: Google's [Material Icons](https://fonts.google.com/icons) font
  (Apache License 2.0).

## Known limits

- **Android and the browser only so far.** The other platforms MAUI targets
  have not been built or run.
- **English only.** All strings are in `Strings.resx` and weekday names
  follow the device's locale, so adding a language means adding one file.
- **Places are not saved**: the chosen place is forgotten when the app
  closes. Only the choice of units is kept.
- **In the MAUI app the map stays light in the dark theme**, because
  OpenStreetMap's tiles come in one style. The web app's map follows the
  theme.
- **The bottom sheet drags by its handle only**, between two stops.
  Scrolling its content does not pull the sheet up.
- **Very large text** gets no extra room in the MAUI app, where the
  forecast's columns are a fixed width. In the web app they widen with the
  text.
- **The web app downloads the .NET runtime** (a few megabytes compressed)
  before it starts; the browser caches it.

## Not built

Ideas that were considered and left out, roughly in the order they would be
worth doing:

- **Saved places** shown when the app opens.
- **Forecast for the current location**, which needs the location permission
  on each platform.
- **A second language**, to exercise the localisation set-up.
