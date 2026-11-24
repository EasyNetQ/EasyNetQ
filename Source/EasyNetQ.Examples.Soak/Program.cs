using System.Diagnostics;
using System.Text;
using EasyNetQ.Examples.Soak;

// Feature soak test: the v9 features production apps do not exercise naturally, each as an assertion-based scenario
// against a real broker. Short mode (CI) runs every scenario --iterations times; --minutes N runs a randomized mix
// for N minutes and prints the counters every --report-seconds. Exit code 1 when any check failed.
//   EasyNetQ.Examples.Soak [--connection "host=localhost"] [--delayed-connection "host=...;port=..."]
//                          [--management http://guest:guest@localhost:15672] [--iterations 1] [--minutes 0]
//                          [--scenario name[,name]] [--seed n] [--report-seconds 60] [--verbose] [--list]
var arguments = ParseArguments(args);
if (arguments.ContainsKey("list"))
{
    foreach (var scenario in ScenarioCatalog.All)
        Console.WriteLine($"{scenario.Name,-16} {(scenario.AotSafe ? "aot-safe" : "jit-only"),-9} {scenario.Covers}");
    return 0;
}

var options = new SoakOptions(
    Get("connection", "EASYNETQ_CONNECTION") ?? "host=localhost",
    Get("delayed-connection", "EASYNETQ_DELAYED_CONNECTION"),
    Get("management", "EASYNETQ_MANAGEMENT") is { } management ? new Uri(management) : null,
    arguments.ContainsKey("verbose")
);
var iterations = int.Parse(Get("iterations", null) ?? "1");
var minutes = double.Parse(Get("minutes", null) ?? "0", System.Globalization.CultureInfo.InvariantCulture);
var reportEvery = TimeSpan.FromSeconds(int.Parse(Get("report-seconds", null) ?? "60"));
var random = new Random(int.Parse(Get("seed", null) ?? Environment.TickCount.ToString()));
var filter = Get("scenario", null)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var selected = ScenarioCatalog.All.Where(s => filter is null || filter.Contains(s.Name)).ToList();
if (selected.Count == 0)
{
    Console.Error.WriteLine("No scenario matches --scenario");
    return 2;
}

Console.WriteLine($"EasyNetQ soak: {selected.Count} scenarios, {(minutes > 0 ? $"{minutes} minutes randomized" : $"{iterations} iteration(s)")}");
Console.WriteLine($"  broker {options.Connection}; delayed-exchange broker {options.DelayedConnection ?? "(none)"}; management {(options.Management is null ? "(none)" : options.Management.GetLeftPart(UriPartial.Authority))}");

await using var admin = new AdminClient(options);
var stats = selected.ToDictionary(s => s.Name, s => new ScenarioStats(s));
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

var started = Stopwatch.StartNew();
if (minutes > 0)
{
    var deadline = TimeSpan.FromMinutes(minutes);
    var nextReport = reportEvery;
    while (started.Elapsed < deadline && !stop.IsCancellationRequested)
    {
        await RunScenarioAsync(selected[random.Next(selected.Count)], quiet: true);
        if (started.Elapsed >= nextReport)
        {
            PrintTable($"after {started.Elapsed:hh\\:mm\\:ss}");
            nextReport += reportEvery;
        }
    }
}
else
{
    for (var i = 0; i < iterations && !stop.IsCancellationRequested; i++)
        foreach (var scenario in selected)
            await RunScenarioAsync(scenario, quiet: iterations > 1 && i > 0);
}

PrintTable($"final, {started.Elapsed:hh\\:mm\\:ss}");
WriteGitHubSummary();
var failedRuns = stats.Values.Sum(s => s.FailedRuns);
Console.WriteLine(failedRuns == 0 ? "ALL OK" : $"{failedRuns} FAILED RUN(S)");
return failedRuns == 0 ? 0 : 1;

async Task RunScenarioAsync(Scenario scenario, bool quiet)
{
    var stat = stats[scenario.Name];
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
    timeout.CancelAfter(TimeSpan.FromMinutes(3));
    var context = new ScenarioContext(scenario, options, admin, timeout.Token);
    var watch = Stopwatch.StartNew();
    Exception? crash = null;
    try
    {
        await scenario.RunAsync(context);
    }
    catch (Exception exception)
    {
        crash = exception;
    }
    finally
    {
        await context.DisposeAsync();
    }
    watch.Stop();

    var failed = context.Checks.Where(c => c.Ok == false).ToList();
    stat.Record(context, watch.Elapsed, crash);
    if (!quiet || failed.Count > 0 || crash is not null)
    {
        foreach (var check in context.Checks)
            if (!quiet || check.Ok == false)
                Console.WriteLine($"{(check.Ok switch { true => "OK  ", false => "FAIL", null => "SKIP" })} [{scenario.Name}] {check.Name} {check.Detail}");
        if (crash is not null)
            Console.WriteLine($"FAIL [{scenario.Name}] crashed: {crash}");
    }
}

void PrintTable(string title)
{
    Console.WriteLine();
    Console.WriteLine($"--- {title} ---");
    Console.WriteLine($"{"scenario",-16} {"aot",-4} {"runs",5} {"failed",6} {"checks",7} {"failed",6} {"skip",5} {"sent",9} {"received",9} {"avg ms",7}");
    foreach (var s in stats.Values.Where(s => s.Runs > 0))
        Console.WriteLine(
            $"{s.Scenario.Name,-16} {(s.Scenario.AotSafe ? "yes" : "no"),-4} {s.Runs,5} {s.FailedRuns,6} {s.Checks,7} {s.FailedChecks,6} {s.SkippedChecks,5} {s.Sent,9} {s.Received,9} {s.AverageMilliseconds,7:F0}"
        );
    Console.WriteLine($"{"total",-16} {"",-4} {stats.Values.Sum(s => s.Runs),5} {stats.Values.Sum(s => s.FailedRuns),6} {stats.Values.Sum(s => s.Checks),7} {stats.Values.Sum(s => s.FailedChecks),6} {stats.Values.Sum(s => s.SkippedChecks),5} {stats.Values.Sum(s => s.Sent),9} {stats.Values.Sum(s => s.Received),9}");
    Console.WriteLine();
}

void WriteGitHubSummary()
{
    if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is not { Length: > 0 } path) return;
    var summary = new StringBuilder();
    summary.AppendLine("## Feature soak").AppendLine();
    summary.AppendLine("| Scenario | AOT-safe | Runs | Failed runs | Checks | Failed | Skipped | Sent | Received |");
    summary.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|");
    foreach (var s in stats.Values.Where(s => s.Runs > 0))
        summary.AppendLine($"| {s.Scenario.Name} | {(s.Scenario.AotSafe ? "yes" : "no")} | {s.Runs} | {s.FailedRuns} | {s.Checks} | {s.FailedChecks} | {s.SkippedChecks} | {s.Sent} | {s.Received} |");
    var skipped = stats.Values.SelectMany(s => s.SkipReasons.Select(r => $"- {s.Scenario.Name}: {r}")).Distinct().ToList();
    if (skipped.Count > 0)
        summary.AppendLine().AppendLine("Skipped:").AppendLine(string.Join(Environment.NewLine, skipped));
    File.AppendAllText(path, summary.ToString());
}

string? Get(string name, string? environmentVariable)
    => arguments.TryGetValue(name, out var value) ? value
        : environmentVariable is null ? null
        : Environment.GetEnvironmentVariable(environmentVariable) is { Length: > 0 } fromEnvironment ? fromEnvironment : null;

static Dictionary<string, string> ParseArguments(string[] args)
{
    var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Unexpected argument {args[i]}");
        var name = args[i][2..];
        var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
        parsed[name] = hasValue ? args[++i] : "true";
    }
    return parsed;
}

internal sealed class ScenarioStats(Scenario scenario)
{
    public Scenario Scenario { get; } = scenario;
    public int Runs { get; private set; }
    public int FailedRuns { get; private set; }
    public int Checks { get; private set; }
    public int FailedChecks { get; private set; }
    public int SkippedChecks { get; private set; }
    public long Sent { get; private set; }
    public long Received { get; private set; }
    public HashSet<string> SkipReasons { get; } = new();
    private TimeSpan elapsed;

    public double AverageMilliseconds => Runs == 0 ? 0 : elapsed.TotalMilliseconds / Runs;

    public void Record(ScenarioContext context, TimeSpan duration, Exception? crash)
    {
        Runs++;
        elapsed += duration;
        var failed = context.Checks.Count(c => c.Ok == false) + (crash is null ? 0 : 1);
        if (failed > 0) FailedRuns++;
        Checks += context.Checks.Count;
        FailedChecks += failed;
        SkippedChecks += context.Checks.Count(c => c.Ok is null);
        foreach (var skipped in context.Checks.Where(c => c.Ok is null))
            SkipReasons.Add($"{skipped.Name}: {skipped.Detail}");
        Sent += context.Sent;
        Received += context.Received;
    }
}
