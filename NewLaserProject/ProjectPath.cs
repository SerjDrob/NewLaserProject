using System;
using System.IO;

namespace NewLaserProject
{
    internal static class ProjectPath
    {
        public static string GetFolderPath(string folder)
        {
            var workingDirectory = Environment.CurrentDirectory;
            var projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            return Path.Combine(projectDirectory, folder);
        }

        public static string GetFilePathInFolder(string folder, string filename)
        {
            return Path.Combine(Path.Combine(GetFolderPath(folder), filename));
        }
    }

    internal static class ProjectFolders
    {
        public const string DATA = "Data";
        public const string APP_SETTINGS = "AppSettings";
        public const string TECHNOLOGY_FILES = "TechnologyFiles";
        public const string TEMP_FILES = "TempFiles";
    }
}
