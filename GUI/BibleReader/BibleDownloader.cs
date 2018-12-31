using ICSharpCode.SharpZipLib.Tar;
using Sword;
using Sword.reader;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using GUI.BibleReader.Interfaces;

namespace GUI.BibleReader
{
    class BibleDownloader : IBibleDownloader
    {
        const string CrosswireServer = "http://www.crosswire.org";
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
                            Logger.Fail("Empty entry: " + filename);
                            continue;
                        }

                        var buffer = new byte[size];
                        using (var entryStream = new MemoryStream(buffer) { Position = 0 })
                        {
                            tarInputStream.CopyEntryContents(entryStream);
                            if (entryStream.Position != size)
                            {
                                // This should not happen, but if it does then skip it.
                                Logger.Fail("Did not read all that was expected " + filename);
                                continue;
                            }
                        }

                        if (filename.EndsWith(BibleZtextReader.ExtensionConf))
                        {
                            filename = RemoveExtensionFromPath(filename, BibleZtextReader.ExtensionConf);
                        }
                        else
                        {
                            Logger.Fail("Not a SWORD config file: " + filename);
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
