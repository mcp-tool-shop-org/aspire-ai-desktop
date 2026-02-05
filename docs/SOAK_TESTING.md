# Soak Testing Guide

This document describes the soak testing process for ScalarScope, designed to prove stability under continuous use.

## Overview

Soak testing runs the application through repeated scenarios over an extended period (default: 2 hours) to detect:
- Memory leaks
- Performance degradation
- Unhandled exceptions
- UI thread hangs

## Running Soak Tests

### Quick Test (5 minutes)
```bash
cd tests/ScalarScope.SoakTests
dotnet run -- --quick
```

### Full Test (2 hours)
```bash
cd tests/ScalarScope.SoakTests
dotnet run
```

### Custom Duration
```bash
dotnet run -- --duration 30  # 30 minutes
```

### Save Report
```bash
dotnet run -- --output path/to/report.json
```

## Test Scenarios

Each iteration includes:

### 1. Memory Stability Check
- Measures current memory vs. start memory
- Fails if growth exceeds 100 MB threshold
- Forces GC to get accurate measurements

### 2. Playback Simulation
- Iterates through time values (0.0 to 1.0)
- Verifies loop completes in under 1 second
- Simulates continuous playback

### 3. Export Simulation
- Allocates 4K RGBA buffer (8+ MB)
- Releases and garbage collects
- Verifies no memory leaks from export operations

### 4. Theme Toggle
- Simulates switching between light/dark themes
- Verifies toggle completes in under 100ms
- Alternates each iteration

### 5. Annotation Toggle
- Toggles all annotation categories
- Verifies all toggles complete in under 100ms
- Tests: phases, warnings, insights, failures

## Success Criteria

A soak test **passes** if:
- Zero unhandled exceptions
- Memory growth < 100 MB over test duration
- No individual test failures
- All operations complete within time limits

## Report Format

```json
{
  "startTime": "2025-02-04T10:00:00Z",
  "endTime": "2025-02-04T12:00:00Z",
  "duration": "02:00:00",
  "totalIterations": 1440,
  "totalTests": 7200,
  "passedTests": 7200,
  "failedTests": 0,
  "startMemoryMB": 45.2,
  "endMemoryMB": 52.1,
  "memoryGrowthMB": 6.9,
  "overallPassed": true,
  "results": [...]
}
```

## CI Integration

The soak test can run in CI with a shorter duration:

```yaml
- name: Soak Test
  run: |
    cd tests/ScalarScope.SoakTests
    dotnet run -- --quick --output soak_report.json

- name: Upload Report
  uses: actions/upload-artifact@v4
  with:
    name: soak-test-report
    path: tests/ScalarScope.SoakTests/soak_report.json
```

## Interpreting Results

### Memory Growth Chart

The report includes memory measurements at each iteration. Plot these to visualize:
- Stable: Flat line with occasional spikes (GC)
- Leak: Steadily increasing line
- Bounded Growth: Initial growth that plateaus

### Performance Metrics

Each test records execution time. Look for:
- Increasing latency over time (degradation)
- Spikes in execution time (contention)
- Consistent times (healthy)

## Troubleshooting

### "Memory growth exceeded threshold"
1. Run with shorter duration to isolate
2. Check for event handler leaks
3. Verify disposables are disposed
4. Review static collections

### "Playback loop timeout"
1. Check UI thread blocking
2. Review async/await patterns
3. Profile with VS Performance Profiler

### "Unhandled exception"
1. Check full stack trace in report
2. Review exception patterns
3. Add defensive handling

## Manual Soak Testing

For release certification, also perform manual soak testing:

1. Launch ScalarScope
2. Load a training run
3. Let playback run continuously for 30 minutes
4. Toggle annotations, change speed, resize window
5. Open comparison view with two runs
6. Export several screenshots
7. Monitor Task Manager for memory growth
8. Record a 60-90 second video of the flow

## Phase 12 Evidence

For Phase 12 certification:
- [ ] 2-hour automated soak test passes
- [ ] Memory growth < 100 MB
- [ ] No unhandled exceptions
- [ ] Screenshot: memory chart at start vs end
- [ ] 60-90 second screen recording of soak flow
