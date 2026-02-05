# ScalarScope Beta Guide

Welcome to the ScalarScope beta program! This guide explains what to test, how to report issues, and what to include in your feedback.

## What is ScalarScope?

ScalarScope is a visualization tool for machine learning training dynamics. It lets you:
- Visualize 2D training trajectories in real-time
- Compare runs side-by-side with synchronized playback
- Analyze eigenvalue spectra and geometry
- Export publication-quality screenshots
- Identify failures and anomalies

## Beta Goals

During this beta period, we're looking for feedback on:
1. **Stability** - Does the app crash or freeze?
2. **Performance** - Is playback smooth? Is export fast?
3. **Usability** - Is the UI intuitive? Are features discoverable?
4. **Correctness** - Do visualizations match expected behavior?
5. **Missing Features** - What would make this tool more useful?

## What to Test

### Basic Workflow
1. Load a training run JSON file
2. Play/pause the trajectory animation
3. Adjust playback speed
4. Toggle annotation categories
5. Switch between tabs (Trajectory, Scalars, Geometry, etc.)
6. Export a screenshot

### Comparison Workflow
1. Navigate to Compare tab
2. Load two different runs
3. Verify synchronized playback
4. Check analytics panel for meaningful metrics

### Stress Testing
1. Load large files (1000+ timesteps)
2. Run for extended periods (30+ minutes)
3. Export multiple screenshots rapidly
4. Switch themes multiple times

### Edge Cases
1. Load malformed or incomplete JSON
2. Resize window to extreme dimensions
3. Use with screen readers or high-contrast modes

## How to Report Issues

### Before Reporting
1. Check [existing issues](https://github.com/mcp-tool-shop-org/scalarscope-desktop/issues)
2. Create a support bundle (Help > Create Support Bundle)

### What to Include
- **Steps to reproduce**: Exactly what you did
- **Expected behavior**: What should have happened
- **Actual behavior**: What actually happened
- **Support bundle**: Attach the generated file
- **Screenshots/recordings**: If visual issue

### Issue Template
```markdown
## Bug Description
[Brief description]

## Steps to Reproduce
1. ...
2. ...
3. ...

## Expected Behavior
[What should happen]

## Actual Behavior
[What actually happens]

## Environment
- OS: [e.g., Windows 11 23H2]
- Version: [e.g., 1.0.0-rc.1]
- GPU: [e.g., NVIDIA RTX 5080]

## Support Bundle
[Attach file]

## Screenshots
[If applicable]
```

## What Logs to Include

When reporting issues, include:
1. **Support bundle** (Help > Create Support Bundle) - contains:
   - System information
   - Memory usage
   - Recent crash logs
   - App configuration
2. **Screenshots or screen recordings** of the issue
3. **The training run file** (if reproducible with specific data)

## Known Limitations

For this RC1 release:
- **Windows only** - macOS/Linux not yet tested
- **Self-signed certificate** - You may need Developer Mode enabled
- **No auto-update** - Manual download for new versions
- **No cloud features** - All data is local

## Feedback Channels

- **Bug reports**: [GitHub Issues](https://github.com/mcp-tool-shop-org/scalarscope-desktop/issues)
- **Feature requests**: [GitHub Issues](https://github.com/mcp-tool-shop-org/scalarscope-desktop/issues) with `[FEATURE]` prefix
- **Questions**: [GitHub Discussions](https://github.com/mcp-tool-shop-org/scalarscope-desktop/discussions)
- **Security issues**: See SECURITY.md

## Timeline

| Phase | Dates | Focus |
|-------|-------|-------|
| RC1 Beta | Feb 2025 | Stability, core features |
| RC2 | TBD | Bug fixes, polish |
| 1.0 Release | TBD | Microsoft Store submission |

## Thank You!

Your feedback directly shapes the product. Every bug report, feature request, and usability observation helps make ScalarScope better for the research community.

We're particularly interested in hearing from:
- ML researchers analyzing training dynamics
- Educators teaching optimization
- Anyone working with learned critics

Happy testing! 🚀
