namespace DataToolkit.Library.Fluent.Compilation;

public sealed class SqlTrace
{
    public bool Enabled { get; }

    private readonly List<string> _steps = new();

    public IReadOnlyList<string> Steps => _steps;

    internal SqlTrace(bool enabled = false)
    {
        Enabled = enabled;
    }

    internal void Add(string message)
    {
        if (!Enabled) return;

        _steps.Add(message);
    }

    internal void Clear()
    {
        _steps.Clear();
    }
}