namespace Infrastructure.System;

public static class DiskHelper
{
    public static IReadOnlyList<DiskInfo> GetFixedDrives()
    {
        List<DiskInfo> result = new();

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            if (drive.DriveType != DriveType.Fixed)
            {
                continue;
            }

            result.Add(new DiskInfo
            {
                Name = drive.Name,
                Label = drive.VolumeLabel,
                Format = drive.DriveFormat,
                TotalBytes = drive.TotalSize,
                FreeBytes = drive.AvailableFreeSpace
            });
        }

        return result;
    }
}
