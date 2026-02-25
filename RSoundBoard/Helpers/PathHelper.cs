namespace TestApp1.Helpers;

public static class PathHelper
{
    public static string GetFullPath(string filePath)
    {
        if (Path.IsPathRooted(filePath))
            return filePath;

        var exeDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
        return Path.Combine(exeDirectory, filePath);
    }

    public static string ConvertToRelativePathIfPossible(string filePath)
    {
        try
        {
            var exeDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            if (string.IsNullOrEmpty(exeDirectory))
                return filePath;

            var fullFilePath = Path.GetFullPath(filePath);
            var fullExePath = Path.GetFullPath(exeDirectory);

            if (fullFilePath.StartsWith(fullExePath, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(fullExePath, fullFilePath);
            }
        }
        catch
        {
            // If any error occurs, return original path
        }

        return filePath;
    }
}
