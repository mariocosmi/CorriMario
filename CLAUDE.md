# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Corri Mario is a fitness tracking app migrated from Windows Phone 7.1 Silverlight to .NET MAUI. It tracks running distance via GPS and gamifies the experience by showing how much "musetto" (Italian cured meat) you've earned based on distance.

This is a **single-page application** with all UI and logic in MainPage. The app targets .NET 8.0 and supports Android, iOS, Windows, and macOS Catalyst.

## Build and Run Commands

### Restore and Build

```bash
# Restore NuGet packages
dotnet restore

# Build for specific platforms
dotnet build -f net8.0-android
dotnet build -f net8.0-ios                        # Requires macOS
dotnet build -f net8.0-windows10.0.19041.0        # Requires Windows
dotnet build -f net8.0-maccatalyst                # Requires macOS
```

### Run on Devices/Emulators

```bash
# Android
dotnet build -f net8.0-android -t:Run

# iOS (requires macOS)
dotnet build -f net8.0-ios -t:Run

# Windows
dotnet build -f net8.0-windows10.0.19041.0 -t:Run
```

## High-Level Architecture

### Single-Page Application Pattern

The entire app runs on a single ContentPage (`MainPage.xaml/.cs`). There is no navigation framework—all functionality is self-contained:

- **App.xaml.cs**: Minimal application entry point that sets MainPage as the root
- **MainPage.xaml.cs**: Contains all GPS tracking logic, state management, and UI updates
- **MauiProgram.cs**: Standard MAUI app configuration

### Data Persistence Strategy

**Uses MAUI Preferences API** (key-value store that persists across app sessions):

- `totale`: Total distance run in meters (double)
- `distanza`: Formatted distance string displayed to user
- `percentuale`: Visual fill percentage for current level
- `avviato`: Boolean tracking state (running/paused)
- `lastLat`, `lastLong`: Last GPS position for distance calculation

**Important**: Properties in MainPage read/write directly to Preferences—there is no in-memory state. Every property getter/setter uses `Preferences.Get()`/`Preferences.Set()`.

### GPS Tracking Architecture

**Async polling loop pattern** (not event-based):

1. User taps Start → `StartTracking()` creates a `CancellationTokenSource`
2. Spawns background `Task.Run()` loop that polls GPS every 10 seconds
3. Each GPS reading calls `OnPositionChanged()` on main thread via `MainThread.InvokeOnMainThreadAsync()`
4. Distance calculated only if moved >25m from last position (filters GPS jitter)
5. User taps Pause → `StopTracking()` cancels token, stopping the loop

**Key technical decisions**:
- **25-meter movement threshold**: Ignores GPS readings within 25m of last position to prevent drift
- **10-second polling interval**: `await Task.Delay(10000)` between GPS checks
- **High accuracy mode**: Uses `GeolocationAccuracy.Best` for precise distance tracking
- **Permission handling**: Requests `LocationWhenInUse` permission on first use

### Visual "Musettometro" System

**Progressive image clipping** using XAML RectangleGeometry:

The UI shows overlaid images (foreground + background) that are clipped to create a "filling" effect:

1. **Level progression** (4 levels based on distance):
   - 0-3000m: level0.jpg background, level1.jpg foreground
   - 3000-6000m: level1.jpg background, level2.jpg foreground
   - 6000-9000m: level2.jpg background, level3.jpg foreground
   - 9000m+: Fully filled (level3.jpg)

2. **Clipping rectangles**:
   - `clipMusetto`: Shows "filled" portion (height = percentage of current 3000m segment)
   - `clipMusettoSfondo`: Shows "unfilled" portion (remaining height)

3. **Update trigger**:
   - Called from `OnPositionChanged()` when distance increases
   - Also called on `SizeChanged` event to handle layout changes/rotations

**Why this matters**: When modifying UI, remember that images must load before clipping works. The `SizeChanged` handler ensures clipping is recalculated after layout.

## Platform-Specific Configuration

### Android (Platforms/Android/)
- **AndroidManifest.xml**: Declares `ACCESS_FINE_LOCATION` and `ACCESS_COARSE_LOCATION` permissions
- **MainActivity.cs**: Standard MAUI activity with orientation/screen size change handling

### iOS (Platforms/iOS/)
- **Info.plist**: Includes `NSLocationWhenInUseUsageDescription` with Italian explanation
- **AppDelegate.cs**: Standard MAUI iOS delegate

### Windows (Platforms/Windows/)
- **App.xaml**: WinUI application wrapper for MAUI

## Migration Notes (Windows Phone → MAUI)

If you need to migrate or modernize code, understand these key replacements:

| Windows Phone 7.1 | .NET MAUI Equivalent |
|-------------------|----------------------|
| `GeoCoordinateWatcher` + events | `Geolocation.GetLocationAsync()` polling |
| `PhoneApplicationService.State` | `Preferences` API |
| `PhoneApplicationPage` | `ContentPage` |
| `GeoCoordinate.GetDistanceTo()` | `Location.CalculateDistance()` |
| Phone namespaces | `Microsoft.Maui.*` namespaces |

**Files with `.old` extension** are original Windows Phone code preserved as reference.

## Key Implementation Details

### Why Task.Run for GPS tracking?
MAUI's Geolocation API is request/response (not event-driven like old GeoCoordinateWatcher). The app uses `Task.Run()` with a while loop to create continuous tracking, manageable via `CancellationToken`.

### Why both DataContext and BindingContext?
`this.DataContext = this` was from Windows Phone migration; `this.BindingContext = this` is MAUI standard. Both are set for compatibility during migration.

### INotifyPropertyChanged implementation
MainPage implements `INotifyPropertyChanged` manually. Properties like `Distanza`, `Avviato`, `Percentuale` notify UI of changes via `OnPropertyChanged()`. This is necessary because values are stored in Preferences (not backing fields), so the UI wouldn't auto-update otherwise.

### OnDisappearing lifecycle
When the page disappears (app backgrounded), `OnDisappearing()` stops GPS tracking to save battery. Data persists in Preferences, so tracking can resume when user returns.

## Adding New Features

### To add a new tracked metric:
1. Add Preference key initialization in `MainPage()` constructor
2. Create public property with `get`/`set` that uses `Preferences.Get()`/`Preferences.Set()`
3. Call `OnPropertyChanged()` in setter
4. Bind to UI element in MainPage.xaml

### To modify GPS behavior:
- Change polling interval: Modify `await Task.Delay(10000)` in StartTracking()
- Change accuracy: Modify `GeolocationAccuracy.Best` in GeolocationRequest
- Change movement threshold: Modify `if (dist >= 25)` in OnPositionChanged()

### To add new level images:
1. Add image to `Resources/Images/`
2. Update `ImmagineAdatta()` and `ImmagineSfondo()` logic
3. Update `AggiornaMusetto()` calculation (currently `totDist % 3000` for 3000m levels)
