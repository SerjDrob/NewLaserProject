using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
