# Release QA Checklist

Pre-release verification for ScalarScope Desktop.

---

## Environment Setup

- [ ] Windows 10 (19041+) or Windows 11
- [ ] .NET 9.0 SDK installed
- [ ] Clean build: `dotnet clean && dotnet build -c Release`
- [ ] Sample files available: `correlated_professors.json`, `orthogonal_professors.json`

---

## 1. File Loading & Validation

### Happy Path
- [ ] Load valid JSON file via file picker
- [ ] Load sample files from Overview page (both buttons work)
- [ ] Large file (10MB+) loads with warning displayed
- [ ] File with many timesteps (10,000+) triggers performance mode

### Error Cases
- [ ] Non-existent file path shows "File Not Found" error
- [ ] Empty file shows "Empty File" error
- [ ] Truncated JSON shows "Truncated File" error
- [ ] Invalid JSON syntax shows "Invalid JSON Format" with line number
- [ ] Wrong schema (missing trajectory) shows "Schema Mismatch" error
- [ ] File too large (>100MB) shows "File Too Large" error
- [ ] All error messages include "What you can try" suggestions
- [ ] "Try Again" button works after error

### State Management
- [ ] Loading new file resets all previous state
- [ ] Playback stops when loading new file
- [ ] Timeline resets to 0% after load
- [ ] No stale data from previous run visible

---

## 2. Performance Safeguards

### Large Run Handling
- [ ] Run with 10,000+ timesteps shows "Large file" warning
- [ ] Frame skip auto-enables for large runs
- [ ] Performance indicator (lightning bolt) visible in canvas legend
- [ ] Playback remains smooth with frame skipping

### Memory & Responsiveness
- [ ] No UI freeze during file load
- [ ] Scrubbing timeline is responsive
- [ ] Switching tabs doesn't cause delays
- [ ] Memory usage stable during extended playback

---

## 3. Playback Controls

### Basic Playback
- [ ] Space toggles play/pause
- [ ] Left/Right arrows step frame
- [ ] Shift+Left/Right for fine stepping (0.1%)
- [ ] Home jumps to start
- [ ] End jumps to end
- [ ] Speed controls work: 0.25x, 0.5x, 1x, 2x, 4x
- [ ] Speed indicator appears briefly when speed changes

### State Consistency
- [ ] All canvases update when time changes
- [ ] Playback stops at end (doesn't loop)
- [ ] Time display matches actual position

---

## 4. Export Functionality

### Single Image Export
- [ ] Quick Screenshot button works
- [ ] Export Settings dialog opens
- [ ] 1080p export produces correct resolution
- [ ] 4K export produces correct resolution
- [ ] Output file exists at expected location
- [ ] Export status updates during operation

### Frame Sequence Export
- [ ] 5s @ 30fps produces 150 frames
- [ ] 10s @ 60fps produces 600 frames
- [ ] Frames are numbered correctly (frame_00000.png, etc.)
- [ ] Progress shown during export
- [ ] Output directory created automatically

### Error Handling
- [ ] Export without loaded run shows error
- [ ] Invalid path shows file write error
- [ ] Cancellation cleans up partial files

---

## 5. Comparison View

### Loading
- [ ] Load Path A works
- [ ] Load Path B works
- [ ] Both runs display simultaneously
- [ ] Interpretation strip shows verdict

### Visual Dominance
- [ ] Stronger run shows "STRONGER" badge
- [ ] Weaker run appears dimmed
- [ ] Equal runs show no dominance indicator

### Synchronized Playback
- [ ] Both canvases animate together
- [ ] Time controls affect both trajectories
- [ ] Metrics update for both runs

### Reset
- [ ] Can reload different files
- [ ] State resets properly between comparisons

---

## 6. Inline Hints (First-Run Guidance)

- [ ] Hints visible on first launch
- [ ] Dismiss button (x) hides hint
- [ ] Dismissed hint stays dismissed (persists)
- [ ] Hints reappear for new hint IDs

---

## 7. Annotation Overlay

### Density Levels
- [ ] Minimal shows 2 phase annotations max
- [ ] Standard shows 5 phase annotations max
- [ ] Full shows 10 phase annotations max

### Content
- [ ] Phase labels appear at significant points
- [ ] Curvature warnings appear at high-curvature regions
- [ ] Annotations don't overlap center position marker

---

## 8. Keyboard Shortcuts

| Shortcut | Action | Works? |
|----------|--------|--------|
| Space | Play/Pause | [ ] |
| Left | Step back 1% | [ ] |
| Right | Step forward 1% | [ ] |
| Shift+Left | Step back 0.1% | [ ] |
| Shift+Right | Step forward 0.1% | [ ] |
| Home | Jump to start | [ ] |
| End | Jump to end | [ ] |
| Up | Increase speed | [ ] |
| Down | Decrease speed | [ ] |
| 0 | Reset speed to 1x | [ ] |
| S | Quick screenshot | [ ] |
| A | Toggle annotations | [ ] |
| ? | Help | [ ] |

---

## 9. Visual Consistency

- [ ] All pages have consistent dark theme
- [ ] No white flashes during navigation
- [ ] Accent colors match (cyan #00d9ff, green #00ff88, etc.)
- [ ] Text is readable on all backgrounds
- [ ] Icons are clear and consistent

---

## 10. Edge Cases

- [ ] Empty trajectory (0 timesteps) handled gracefully
- [ ] Single timestep trajectory renders
- [ ] NaN values in data don't crash rendering
- [ ] Very small viewport still renders
- [ ] Very large viewport (4K+) renders correctly

---

## 11. Invariant & Consistency Checks (Phase 4)

### Invariant Guards
- [ ] Time values always clamped to [0, 1]
- [ ] Empty trajectory shows "no data" message, not crash
- [ ] Eigenvalue calculations are consistent across views
- [ ] Diagnostics show invariant violation count

### Cross-View Consistency
- [ ] First factor variance identical in EigenSpectrum and Comparison
- [ ] Interpretation category matches across all views
- [ ] Effective dimensionality calculation is identical everywhere

### Golden Run Validation
- [ ] Can capture golden snapshots (via developer tools)
- [ ] Diagnostics reports golden snapshot count
- [ ] Comparison against golden detects drift

---

## Pre-Release Sign-Off

| Area | Status | Tester | Date |
|------|--------|--------|------|
| File Loading | | | |
| Performance | | | |
| Playback | | | |
| Export | | | |
| Comparison | | | |
| Hints/Annotations | | | |
| Keyboard | | | |
| Visual | | | |
| Edge Cases | | | |
| Invariants/Consistency | | | |

---

## Known Issues

*(Document any known issues here with workarounds)*

1. SkiaSharp deprecation warnings (349+) - Cosmetic, no functional impact
2. XamlC binding warnings - Cosmetic, no functional impact

---

## Smoke Test (5-Minute Version)

Quick verification for urgent releases:

1. [ ] App launches without crash
2. [ ] Load sample run (correlated)
3. [ ] Play/pause with Space
4. [ ] Navigate all tabs
5. [ ] Export one screenshot
6. [ ] Load different run (orthogonal)
7. [ ] Compare view shows both runs
8. [ ] Close app cleanly

---

*Last updated: Phase 4 Instrument Readiness & Trust*
