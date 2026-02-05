# Data Persistence Rules

This document describes what data ScalarScope stores, where it's stored, and what happens during install/upgrade/uninstall.

## Storage Locations

### Application Data
**Location**: `%LOCALAPPDATA%\ScalarScope\`

Contains:
- `settings.json` - User preferences (theme, default export settings)
- `recent.json` - Recently opened files list
- `window.json` - Window position and size
- `logs/` - Application logs (rotated, max 10 MB)

### Temporary Data
**Location**: `%TEMP%\ScalarScope\`

Contains:
- Frame export buffers (cleaned on exit)
- Crash dumps (preserved for support)

### User-Created Data
**Location**: User-specified paths

Contains:
- Exported screenshots (PNG)
- Exported frame sequences
- Support bundles (ZIP)

## Lifecycle Behavior

### Fresh Install
- Creates `%LOCALAPPDATA%\ScalarScope\` directory
- Initializes `settings.json` with defaults
- No migration needed

### Upgrade
- **Preserves all user data**
- May add new settings fields with defaults
- Logs are preserved (useful for debugging upgrade issues)
- Recent files list maintained

### Uninstall
- Removes application files
- **Preserves** `%LOCALAPPDATA%\ScalarScope\` by default
- User can manually delete to fully clean

### Full Clean Uninstall
```powershell
# After standard uninstall:
Remove-Item -Recurse "$env:LOCALAPPDATA\ScalarScope"
Remove-Item -Recurse "$env:TEMP\ScalarScope" -ErrorAction SilentlyContinue
```

## Settings Schema

### settings.json
```json
{
  "version": "1.0.0",
  "theme": "dark",
  "export": {
    "defaultWidth": 1920,
    "defaultHeight": 1080,
    "defaultFps": 30,
    "defaultFormat": "png"
  },
  "playback": {
    "defaultSpeed": 1.0
  },
  "annotations": {
    "showPhases": true,
    "showWarnings": true,
    "showInsights": true,
    "showFailures": true
  },
  "accessibility": {
    "reduceMotion": false,
    "highContrast": false
  }
}
```

### recent.json
```json
{
  "version": "1.0.0",
  "maxEntries": 10,
  "files": [
    {
      "path": "C:\\Data\\run1.json",
      "lastOpened": "2025-02-04T10:30:00Z",
      "displayName": "Path A Training Run"
    }
  ]
}
```

## Privacy Considerations

ScalarScope:
- Does **NOT** collect telemetry
- Does **NOT** phone home
- Does **NOT** upload user data
- All data stays local

## Backup Recommendations

To backup ScalarScope settings:
```powershell
Copy-Item -Recurse "$env:LOCALAPPDATA\ScalarScope" "D:\Backup\ScalarScope"
```

To restore:
```powershell
Copy-Item -Recurse "D:\Backup\ScalarScope" "$env:LOCALAPPDATA\ScalarScope"
```

## Version Migration

When upgrading across major versions, the app:
1. Reads existing settings
2. Applies migration transformations if needed
3. Writes updated settings with new version
4. Logs any migration issues

Migration is **non-destructive** - original files are backed up as `.bak` before modification.
