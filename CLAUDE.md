# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a BepInEx mod for Hollow Knight: Silksong that provides teleportation functionality. The mod allows players to save and load positions using keyboard shortcuts or gamepad controls, with persistent data storage and sound effects.

## Architecture

- **Main Class**: `TeleportMod` - BepInEx plugin implementation with Unity mod hooks
- **Configuration**: Extensive BepInEx configuration system for customizing all keybindings and settings
- **Save System**: JSON-based coordinate persistence in Unity's LocalLow directory
- **Audio System**: Embedded WAV files with volume control and easter egg sounds
- **Input Handling**: Dual support for keyboard (configurable modifier keys) and gamepad input

## Build Commands

### Prerequisites
Set environment variable: `set HollowKnightPath=E:\Steam\steamapps\common\Hollow Knight Silksong`

### Build Options

**Using build script (recommended):**
```cmd
build.bat
```

**Using MSBuild directly:**
```cmd
msbuild TeleportMod.csproj /p:Configuration=Release /p:Platform=AnyCPU
```

**Using .NET SDK (if available):**
```cmd
dotnet build Teleport.csproj --configuration Release
```

The build automatically copies the DLL to the game's BepInEx plugins directory if `HollowKnightPath` is set.

## Project Structure

- **TeleportMod.cs**: Main plugin code (~1400 lines) with configuration, input handling, and teleportation logic
- **Two project files**: 
  - `Teleport.csproj` (newer .NET SDK style for netstandard2.1)
  - `TeleportMod.csproj` (legacy MSBuild format for .NET Framework 4.7.2)
- **Audio assets**: `Gamesave.wav` and `manbo.wav` embedded as resources
- **build.bat**: Automated build script with environment validation

## Dependencies

**Game References** (must be from Hollow Knight: Silksong installation):
- Assembly-CSharp.dll
- Unity engine modules (Core, Audio, InputLegacy, Physics2D, etc.)
- BepInEx framework

**NuGet Packages**:
- Newtonsoft.Json (for save data serialization)

## Key Features to Understand

1. **Dual Input Systems**: Keyboard uses modifier keys + numbers, gamepad uses button combinations
2. **Save Slot System**: 5 coordinate slots with JSON persistence
3. **Safety Features**: Safe respawn cycling through entry points, coordinate reset functionality
4. **Audio Feedback**: Configurable sound effects on save operations
5. **Full Customization**: All keybindings can be reconfigured via BepInEx config

## Development Notes

- Target framework varies between project files (netstandard2.1 vs .NET 4.7.2)
- Uses reflection and Unity coroutines extensively
- Heavy use of BepInEx configuration system for user customization
- Chinese/English bilingual support throughout