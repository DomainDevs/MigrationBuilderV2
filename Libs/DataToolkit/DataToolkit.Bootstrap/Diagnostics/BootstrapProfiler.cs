using System.Diagnostics;

namespace DataToolkit.Bootstrap.Diagnostics;

internal sealed class BootstrapProfiler
{
    private readonly Dictionary<BootstrapPhase, TimeSpan> _times = [];
    private readonly Stopwatch _stopwatch = new();

    private BootstrapPhase _current;

    public void Start(BootstrapPhase phase)
    {
        _current = phase;

        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Stop();

        TimeSpan elapsed = _stopwatch.Elapsed;

        if (_times.TryGetValue(_current, out TimeSpan current))
        {
            _times[_current] = current + elapsed;
        }
        else
        {
            _times[_current] = elapsed;
        }
    }

    public TimeSpan Get(BootstrapPhase phase)
    {
        return _times.TryGetValue(phase, out TimeSpan value)
            ? value
            : TimeSpan.Zero;
    }

    public IReadOnlyDictionary<BootstrapPhase, TimeSpan> GetAll()
    {
        return _times;
    }

    public TimeSpan Total =>
        TimeSpan.FromTicks(_times.Values.Sum(x => x.Ticks));
}

internal enum BootstrapPhase
{
    AssemblyScan,
    Reflection,
    DescriptorBuild,
    DiRegistration,
    ConsoleOutput
}