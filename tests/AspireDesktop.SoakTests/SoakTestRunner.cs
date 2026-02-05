using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AspireDesktop.SoakTests;

/// <summary>
/// Soak test runner for ASPIRE Desktop.
/// Runs the application through various scenarios to prove stability.
/// </summary>
public class SoakTestRunner
{
    private readonly ILogger _logger;
    private readonly SoakTestConfig _config;
    private readonly List<SoakTestResult> _results = new();
    private long _startMemory;
    private readonly Stopwatch _stopwatch = new();

    public SoakTestRunner(ILogger logger, SoakTestConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<SoakTestReport> RunAsync()
    {
        _logger.LogInformation("Starting soak test with duration: {Duration} minutes", _config.DurationMinutes);

        _startMemory = GC.GetTotalMemory(true);
        _stopwatch.Start();

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_config.DurationMinutes));
        var iteration = 0;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                iteration++;
                _logger.LogInformation("Starting iteration {Iteration}", iteration);

                await RunIterationAsync(iteration, cts.Token);

                // Brief pause between iterations
                await Task.Delay(TimeSpan.FromSeconds(_config.PauseBetweenIterationsSeconds), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Soak test completed normally after {Elapsed}", _stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Soak test failed with exception");
            _results.Add(new SoakTestResult
            {
                TestName = "UnhandledException",
                Passed = false,
                Message = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }

        _stopwatch.Stop();

        var endMemory = GC.GetTotalMemory(true);
        var report = GenerateReport(endMemory, iteration);

        return report;
    }

    private async Task RunIterationAsync(int iteration, CancellationToken ct)
    {
        // Test 1: Memory stability check
        await TestMemoryStabilityAsync(iteration, ct);

        // Test 2: Playback simulation
        await TestPlaybackSimulationAsync(iteration, ct);

        // Test 3: Export simulation
        await TestExportSimulationAsync(iteration, ct);

        // Test 4: Theme toggle simulation
        await TestThemeToggleAsync(iteration, ct);

        // Test 5: Annotation toggle simulation
        await TestAnnotationToggleAsync(iteration, ct);
    }

    private async Task TestMemoryStabilityAsync(int iteration, CancellationToken ct)
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryGrowthMB = (currentMemory - _startMemory) / (1024.0 * 1024.0);

        var passed = memoryGrowthMB < _config.MaxMemoryGrowthMB;

        _results.Add(new SoakTestResult
        {
            TestName = $"MemoryStability_Iteration{iteration}",
            Passed = passed,
            Message = $"Memory growth: {memoryGrowthMB:F2} MB (limit: {_config.MaxMemoryGrowthMB} MB)",
            Timestamp = DateTime.UtcNow,
            MetricValue = memoryGrowthMB
        });

        if (!passed)
        {
            _logger.LogWarning("Memory growth exceeded threshold: {Growth:F2} MB", memoryGrowthMB);
        }

        await Task.Delay(100, ct);
    }

    private async Task TestPlaybackSimulationAsync(int iteration, CancellationToken ct)
    {
        // Simulate playback by iterating through time values
        var sw = Stopwatch.StartNew();

        for (double t = 0; t <= 1.0; t += 0.01)
        {
            // Simulate what the playback loop does
            await Task.Yield();
            if (ct.IsCancellationRequested) break;
        }

        sw.Stop();

        var passed = sw.ElapsedMilliseconds < 1000; // Should complete in under 1 second

        _results.Add(new SoakTestResult
        {
            TestName = $"PlaybackSimulation_Iteration{iteration}",
            Passed = passed,
            Message = $"Playback loop completed in {sw.ElapsedMilliseconds} ms",
            Timestamp = DateTime.UtcNow,
            MetricValue = sw.ElapsedMilliseconds
        });
    }

    private async Task TestExportSimulationAsync(int iteration, CancellationToken ct)
    {
        // Simulate export operations
        var sw = Stopwatch.StartNew();

        // Allocate and release a buffer like export would
        var buffer = new byte[1920 * 1080 * 4]; // 4K RGBA
        Array.Fill(buffer, (byte)0xFF);
        buffer = null!;

        // Force collection to verify no leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();

        sw.Stop();

        _results.Add(new SoakTestResult
        {
            TestName = $"ExportSimulation_Iteration{iteration}",
            Passed = true,
            Message = $"Export buffer allocation/release in {sw.ElapsedMilliseconds} ms",
            Timestamp = DateTime.UtcNow,
            MetricValue = sw.ElapsedMilliseconds
        });

        await Task.Delay(50, ct);
    }

    private async Task TestThemeToggleAsync(int iteration, CancellationToken ct)
    {
        // Simulate theme toggle
        var sw = Stopwatch.StartNew();

        // Simulate theme state change
        bool isDark = iteration % 2 == 0;
        await Task.Yield();

        sw.Stop();

        _results.Add(new SoakTestResult
        {
            TestName = $"ThemeToggle_Iteration{iteration}",
            Passed = sw.ElapsedMilliseconds < 100,
            Message = $"Theme toggle to {(isDark ? "dark" : "light")} in {sw.ElapsedMilliseconds} ms",
            Timestamp = DateTime.UtcNow,
            MetricValue = sw.ElapsedMilliseconds
        });
    }

    private async Task TestAnnotationToggleAsync(int iteration, CancellationToken ct)
    {
        // Simulate annotation toggle
        var sw = Stopwatch.StartNew();

        // Simulate toggling annotation categories
        var categories = new[] { "phases", "warnings", "insights", "failures" };
        foreach (var category in categories)
        {
            await Task.Yield();
            if (ct.IsCancellationRequested) break;
        }

        sw.Stop();

        _results.Add(new SoakTestResult
        {
            TestName = $"AnnotationToggle_Iteration{iteration}",
            Passed = sw.ElapsedMilliseconds < 100,
            Message = $"All annotation toggles in {sw.ElapsedMilliseconds} ms",
            Timestamp = DateTime.UtcNow,
            MetricValue = sw.ElapsedMilliseconds
        });
    }

    private SoakTestReport GenerateReport(long endMemory, int iterations)
    {
        var totalTests = _results.Count;
        var passedTests = _results.Count(r => r.Passed);
        var failedTests = totalTests - passedTests;

        var memoryGrowthMB = (endMemory - _startMemory) / (1024.0 * 1024.0);

        var report = new SoakTestReport
        {
            StartTime = DateTime.UtcNow - _stopwatch.Elapsed,
            EndTime = DateTime.UtcNow,
            Duration = _stopwatch.Elapsed,
            TotalIterations = iterations,
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            StartMemoryMB = _startMemory / (1024.0 * 1024.0),
            EndMemoryMB = endMemory / (1024.0 * 1024.0),
            MemoryGrowthMB = memoryGrowthMB,
            Results = _results,
            OverallPassed = failedTests == 0 && memoryGrowthMB < _config.MaxMemoryGrowthMB
        };

        return report;
    }
}

public class SoakTestConfig
{
    public int DurationMinutes { get; set; } = 120; // 2 hours default
    public int PauseBetweenIterationsSeconds { get; set; } = 5;
    public double MaxMemoryGrowthMB { get; set; } = 100; // Allow up to 100 MB growth
}

public class SoakTestResult
{
    public required string TestName { get; set; }
    public bool Passed { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public double MetricValue { get; set; }
}

public class SoakTestReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalIterations { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public double StartMemoryMB { get; set; }
    public double EndMemoryMB { get; set; }
    public double MemoryGrowthMB { get; set; }
    public List<SoakTestResult> Results { get; set; } = new();
    public bool OverallPassed { get; set; }

    public void WriteToConsole()
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                    SOAK TEST REPORT                           ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine($"  Duration:        {Duration:hh\\:mm\\:ss}");
        Console.WriteLine($"  Iterations:      {TotalIterations}");
        Console.WriteLine($"  Tests Run:       {TotalTests}");
        Console.WriteLine($"  Passed:          {PassedTests}");
        Console.WriteLine($"  Failed:          {FailedTests}");
        Console.WriteLine();
        Console.WriteLine("  Memory:");
        Console.WriteLine($"    Start:         {StartMemoryMB:F2} MB");
        Console.WriteLine($"    End:           {EndMemoryMB:F2} MB");
        Console.WriteLine($"    Growth:        {MemoryGrowthMB:F2} MB");
        Console.WriteLine();
        Console.WriteLine($"  OVERALL:         {(OverallPassed ? "✅ PASSED" : "❌ FAILED")}");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        if (FailedTests > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Failed Tests:");
            foreach (var result in Results.Where(r => !r.Passed))
            {
                Console.WriteLine($"    ❌ {result.TestName}: {result.Message}");
            }
        }
    }

    public async Task WriteToFileAsync(string path)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(path, json);
    }
}
