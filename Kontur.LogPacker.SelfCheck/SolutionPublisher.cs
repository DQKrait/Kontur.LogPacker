using System;
using System.Diagnostics;
using System.IO;

namespace Kontur.LogPacker.SelfCheck
{
    internal class SolutionPublisher
    {
        public string Publish(string pathToProject)
        {
            var options = new ProcessStartInfo("dotnet", "publish -c Release")
            {
                CreateNoWindow = true,
                WorkingDirectory = pathToProject
            };
            
            var process = Process.Start(options);
            if (process == null)
                throw new Exception("Failed to start 'dotnet publish'.");

            process.WaitForExit();

            var projectName = Path.GetFileName(pathToProject.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var binaryPath = Path.GetFullPath(Path.Combine(pathToProject, $"bin/Release/netcoreapp2.1/publish/{projectName}.dll"));
            if (!File.Exists(binaryPath))
                throw  new FileNotFoundException($"Running 'dotnet publish' did not create a binary at the expected path: {binaryPath}.");

            return binaryPath;
        }
    }
}