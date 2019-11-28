using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using BibleLoader.Interfaces;
using ICSharpCode.SharpZipLib.Tar;
using Sword;
using Sword.reader;

namespace BibleLoader
{
    public class BibleDownloader : IBibleDownloader
    {
        const string CrosswireServer = "http://www2.crosswire.org";
        const string MetadatasUrl = "/ftpmirror/pub/sword/raw/mods.d.tar.gz";
        const string BookPackagesDirectory = "/ftpmirror/pub/sword/packages/rawzip";
        const string ZipExtension = ".zip";

        readonly HttpClient httpClient = new HttpClient();

        public async Task<List<SwordBookMetaData>> DownloadBookMetadatasAsync()
        {
            string url = CreateMetadatasUrl();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            Stream downloadedDataStream = await response.Content.ReadAsStreamAsync();
            using (var gzipStream = new GZipStream(downloadedDataStream, CompressionMode.Decompress))
            {
                return GetMetadatasFromStream(gzipStream);
            }
        }

        /// <summary>
        /// Downloads zip archive as Stream
        /// </summary>
        public async Task<Stream> DownloadBookAsync(SwordBookMetaData bookMetadata)
        {
            string bookUrl = CreateBookUrl(bookMetadata.Initials);
            HttpResponseMessage response = await httpClient.GetAsync(bookUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            throw new HttpRequestException($"Bible download was not successful. Request ended with status code {response.StatusCode}.");
        }

        List<SwordBookMetaData> GetMetadatasFromStream(GZipStream gzipStream)
        {
            var metadatas = new List<SwordBookMetaData>();
            string configDirectory = BibleZtextReader.DirConf + '/';

            using (var tarInputStream = new TarInputStream(gzipStream))
            {
                TarEntry entry;
                while ((entry = tarInputStream.GetNextEntry()) != null)
                {
                    if (!entry.IsDirectory)
                    {
                        string filename = entry.Name;
                        var size = (int)entry.Size;
                        if (size == 0) // Every now and then an empty entry sneaks in
                        {
                            continue;
                        }

                        var buffer = new byte[size];
                        using (var entryStream = new MemoryStream(buffer) { Position = 0 })
                        {
                            tarInputStream.CopyEntryContents(entryStream);
                            if (entryStream.Position != size)
                            {
                                // This should not happen, but if it does then skip it.
                                continue;
                            }
                        }

                        if (filename.EndsWith(BibleZtextReader.ExtensionConf))
                        {
                            filename = RemoveExtensionFromPath(filename, BibleZtextReader.ExtensionConf);
                        }
                        else
                        {
                            continue;
                        }

                        if (filename.StartsWith(configDirectory))
                        {
                            filename = RemoveDirectoryFromPath(filename, configDirectory);
                        }

                        metadatas.Add(new SwordBookMetaData(buffer, filename));
                    }
                }
            }
            return metadatas;
        }

        string RemoveExtensionFromPath(string path, string extension)
            => path.Substring(0, path.Length - extension.Length);
        string RemoveDirectoryFromPath(string path, string directoryPath)
            => path.Substring(directoryPath.Length);

        string CreateMetadatasUrl()
            => $"{CrosswireServer}{MetadatasUrl}";
        string CreateBookUrl(string bookInitials)
            => $"{CrosswireServer}{BookPackagesDirectory}/{bookInitials}{ZipExtension}";
    }
}
