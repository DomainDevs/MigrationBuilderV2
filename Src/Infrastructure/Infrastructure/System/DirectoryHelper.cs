namespace Infrastructure.System;

public static class DirectoryHelper
{
    public static DirectoryInfo Create(string path)
    {
        ValidatePath(path);

        return Directory.CreateDirectory(path);
    }

    public static DirectoryInfo Create(
        string path,
        FileAttributes attributes)
    {
        DirectoryInfo directory =
            Create(path);

        directory.Attributes = attributes;

        return directory;
    }

    public static bool Exists(string path)
    {
        ValidatePath(path);

        return Directory.Exists(path);
    }

    public static void Delete(
        string path,
        bool recursive = false)
    {
        ValidatePath(path);

        if (!Directory.Exists(path))
        {
            return;
        }

        Directory.Delete(
            path,
            recursive);
    }

    public static void EnsureExists(string path)
    {
        ValidatePath(path);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static void EnsureEmpty(string path)
    {
        ValidatePath(path);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return;
        }

        DirectoryInfo directory =
            new(path);

        foreach (FileInfo file in directory.GetFiles())
        {
            file.IsReadOnly = false;
            file.Delete();
        }

        foreach (DirectoryInfo subDirectory in directory.GetDirectories())
        {
            subDirectory.Delete(true);
        }
    }

    public static void SetAttributes(
        string path,
        FileAttributes attributes)
    {
        ValidateDirectory(path);

        DirectoryInfo directory =
            new(path);

        directory.Attributes = attributes;
    }

    public static void AddAttributes(
        string path,
        FileAttributes attributes)
    {
        ValidateDirectory(path);

        DirectoryInfo directory =
            new(path);

        directory.Attributes |= attributes;
    }

    public static void RemoveAttributes(
        string path,
        FileAttributes attributes)
    {
        ValidateDirectory(path);

        DirectoryInfo directory =
            new(path);

        directory.Attributes &= ~attributes;
    }

    private static void ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "Directory path cannot be null or empty.",
                nameof(path));
        }

        foreach (char invalidChar in Path.GetInvalidPathChars())
        {
            if (path.Contains(invalidChar))
            {
                throw new ArgumentException(
                    $"Directory path contains an invalid character '{invalidChar}'.",
                    nameof(path));
            }
        }
    }

    private static void ValidateDirectory(string path)
    {
        ValidatePath(path);

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"Directory '{path}' does not exist.");
        }
    }


}
