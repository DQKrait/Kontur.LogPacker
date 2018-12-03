using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Kontur.LogPacker.SelfCheck
{
    internal class SolutionChecker
    {
        private const string TemporaryDirectoryPath = "test";
        private static readonly string UncompressedFile = Path.GetFullPath(Path.Combine(TemporaryDirectoryPath, "example.log"));
        private static readonly string CompressedFile = Path.GetFullPath(Path.Combine(TemporaryDirectoryPath, "example.log.compressed"));
        private static readonly string DecompressedFile = Path.GetFullPath(Path.Combine(TemporaryDirectoryPath, "example.log.decompressed"));
        
        private readonly string pathToSolutionBinary;
        private readonly string pathToGZipBinary;

        public SolutionChecker(string pathToProject, string pathToGZipProject)
        {
            pathToSolutionBinary = new SolutionPublisher().Publish(pathToProject);
            pathToGZipBinary = new SolutionPublisher().Publish(pathToGZipProject);
        }

        private static readonly string[] Tests =
        {
            nameof(Should_restore_original_file_after_decompression),
            nameof(Should_correctly_handle_random_binary_data),
            nameof(Should_produce_input_smaller_than_gzip),
            nameof(Should_not_compress_too_slow),
            nameof(Should_not_compress_too_slow_with_random_data_input),
            nameof(Should_not_decompress_too_slow),
            nameof(Should_not_decompress_too_slow_with_random_data_input),
            nameof(Should_not_leave_temporary_files_after_compression),
            nameof(Should_not_leave_temporary_files_after_decompression),
        };

        public bool Run()
        {
            var someTestsFailed = false;
            
            foreach (var test in Tests)
            {
                Console.WriteLine($"{test}:");
                try
                {
                    Directory.CreateDirectory(TemporaryDirectoryPath);
                    try
                    {
                        File.Copy("example.log", UncompressedFile);
                        GetType().GetMethod(test, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this, null);
                    }
                    finally
                    {
                        Directory.Delete(TemporaryDirectoryPath, true);
                    }
                    Console.WriteLine("\tPassed.");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"\tFailed: {error?.InnerException?.Message ?? error.ToString()}");
                    someTestsFailed = true;
                }
                Console.WriteLine();
            }

            return !someTestsFailed;
        }

        #region Helpers

        private void Compress(bool useGzip = false, string outPath = null)
        {
            var project = useGzip ? pathToGZipBinary : pathToSolutionBinary;
            
            RunSolution(project, $"{UncompressedFile} {outPath ?? CompressedFile}");
        }
        
        private void Decompress(bool useGzip = false, string inPath = null)
        {
            var project = useGzip ? pathToGZipBinary : pathToSolutionBinary;

            RunSolution(project, $"-d {inPath ?? CompressedFile} {DecompressedFile}");
        }

        private TimeSpan Time(Action action)
        {
            const int iterations = 20;
            
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                action();

            return watch.Elapsed.Divide(iterations);
        }

        private void RunSolution(string binaryPath, string args)
        {
            var options = new ProcessStartInfo("dotnet", $"{binaryPath} {args}")
            {
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(binaryPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            var process = Process.Start(options);
            if (process == null)
                throw new Exception("Failed to start 'dotnet'.");

            process.WaitForExit();
            
            Console.Write(process.StandardOutput.ReadToEnd());
            Console.Write(process.StandardError.ReadToEnd());
        }

        #endregion

        #region Tests

        private void Should_produce_input_smaller_than_gzip()
        {
            double originalLength = new FileInfo(UncompressedFile).Length;
            
            Compress();
            var solutionLength = new FileInfo(CompressedFile).Length;
            File.Delete(CompressedFile);
            
            Compress(useGzip: true);
            var gzipLength = new FileInfo(CompressedFile).Length;

            var gzipRatePct = gzipLength * 100 / originalLength;
            var solutionRatePct = solutionLength * 100 / originalLength;

            Console.WriteLine($"File sizes: {gzipLength} bytes ({gzipRatePct:f2}%) - gzip, {solutionLength} bytes ({solutionRatePct:f2}%) - solution");
            if (gzipRatePct - solutionRatePct < 1)
                throw new Exception("The compression rate of the tested solution should be at least 1 percent point better than the compression rate of GZip!");
        }

        private void Should_restore_original_file_after_decompression()
        {
            Compress();
            Decompress();
            
            var equals = StructuralComparisons.StructuralEqualityComparer.Equals(
                File.ReadAllBytes(UncompressedFile), File.ReadAllBytes(DecompressedFile));

            if (!equals)
                throw new Exception("File was corrupted after decompression!");
        }

        private void Should_correctly_handle_random_binary_data()
        {
            var bytes = new byte[1024 * 1024];
            new Random().NextBytes(bytes);
            File.WriteAllBytes(UncompressedFile, bytes);
            
            Should_restore_original_file_after_decompression();
        }

        private void Should_not_compress_too_slow()
        {
            var gzipTime = Time(() => Compress(useGzip: true));
            var solutionTime = Time(() => Compress());

            Console.WriteLine($"Compression times: {gzipTime} - gzip, {solutionTime} - solution");
            if (solutionTime.Divide(2) > gzipTime)
                throw new Exception("Compression time of the tested solution was more than twice the time of GZip compression!");
        }
        
        private void Should_not_compress_too_slow_with_random_data_input()
        {
            var bytes = new byte[1024 * 1024];
            new Random().NextBytes(bytes);
            File.WriteAllBytes(UncompressedFile, bytes);

            Should_not_compress_too_slow();
        }

        private void Should_not_decompress_too_slow()
        {
            var gzipOutput = CompressedFile + "_gzip";
            Compress(useGzip: true, outPath: gzipOutput);
            Compress();
            
            var gzipTime = Time(() => Decompress(useGzip: true, inPath: gzipOutput));
            var solutionTime = Time(() => Decompress());

            Console.WriteLine($"Decompression times: {gzipTime} - gzip, {solutionTime} - solution");
            if (solutionTime.Divide(2) > gzipTime)
                throw new Exception("Decompression time of the tested solution was more than twice the time of GZip decompression!");
        }
        
        private void Should_not_decompress_too_slow_with_random_data_input()
        {
            var bytes = new byte[1024 * 1024];
            new Random().NextBytes(bytes);
            File.WriteAllBytes(UncompressedFile, bytes);

            Should_not_decompress_too_slow();
        }
        
        private void Should_not_leave_temporary_files_after_compression()
        {
            Compress();
            
            if (Directory.GetFiles(TemporaryDirectoryPath).Length > 2)
                throw new Exception("Some extra files were left in the working directory!");
        }
        
        private void Should_not_leave_temporary_files_after_decompression()
        {
            Compress();
            Decompress();
            
            if (Directory.GetFiles(TemporaryDirectoryPath).Length > 3)
                throw new Exception("Some extra files were left in the working directory!");
        }
        
        #endregion
    }
}