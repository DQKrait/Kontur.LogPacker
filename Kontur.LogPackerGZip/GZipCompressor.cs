using System.IO;
using System.IO.Compression;

namespace Kontur.LogPackerGZip
{
    internal class GZipCompressor
    {
        public void Compress(Stream inputStream, Stream outputStream)
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
                inputStream.CopyTo(gzipStream);
        }

        public void Decompress(Stream inputStream, Stream outputStream)
        {
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                gzipStream.CopyTo(outputStream);
        }
    }
}