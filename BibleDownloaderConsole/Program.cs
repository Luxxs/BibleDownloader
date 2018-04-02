using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BibleDownloaderConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "http://www.crosswire.org/";
            var client = new WebClient();
            client.BaseAddress = uri;
            var downloadedData = client.DownloadData("/ftpmirror/pub/sword/raw/mods.d.tar.gz");
            var downloadedDataStream = new MemoryStream(downloadedData);
            var gzipStream = new GZipStream(downloadedDataStream, CompressionMode.Decompress);
        }
    }
}
