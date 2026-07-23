namespace Infrastructure.System;

public sealed class DiskInfo
{
    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public long TotalBytes { get; set; }

    public long FreeBytes { get; set; }

    public long UsedBytes
    {
        get
        {
            return TotalBytes - FreeBytes;
        }
    }
}
