using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Kontur.LogPacker.SubmitHelper
{
    internal static class EntryPoint
    {
        public static void Main()
        {
            var path = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
            var desiredPathSuffix = "Kontur.LogPacker/Kontur.LogPacker.SubmitHelper/bin/Release/netcoreapp2.1/".Replace('/', Path.DirectorySeparatorChar);

            if (!path.EndsWith(desiredPathSuffix))
                throw new Exception("Unexpected base directory! Please make sure to run SubmitHelper in Release configuration.");

            var solutionRoot = Path.GetFullPath(Path.Combine(path, "../../../../"));
            var logPackerProjectPath = Path.GetFullPath(Path.Combine(solutionRoot, "Kontur.LogPacker/Kontur.LogPacker.csproj"));

            if (!File.Exists(logPackerProjectPath))
                throw new Exception($"Unexpected directory structure! File '{logPackerProjectPath}' does not exist.");

            var zipPath = Path.GetFullPath(Path.Combine(solutionRoot, "Kontur.LogPacker.zip"));
            var filesToPack = TraverseDirectory(solutionRoot)
                .Where(file => !Path.GetRelativePath(solutionRoot, file).Split(Path.DirectorySeparatorChar).Any(s => IgnoredSubfolders.Contains(s)))
                .Where(file => Path.GetExtension(file) != ".zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in filesToPack)
                {
                    Console.WriteLine($"Packing '{file}'..");
                    zip.CreateEntryFromFile(file, Path.GetRelativePath(solutionRoot, file).Replace(Path.DirectorySeparatorChar, '/'));
                }
            }
        }

        private static readonly string[] IgnoredSubfolders = {"bin", "obj", ".idea", ".vs", ".git"};

        private static IEnumerable<string> TraverseDirectory(string directory)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
                yield return file;

            foreach (var dir in Directory.EnumerateDirectories(directory))
            foreach (var file in TraverseDirectory(dir))
                yield return file;
        }
    }
}