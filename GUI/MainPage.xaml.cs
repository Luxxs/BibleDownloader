using GUI.BibleReader;
using ICSharpCode.SharpZipLib.Tar;
using Sword;
using Sword.reader;
using Sword.versification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string crosswireServer = "www.crosswire.org";
        const string packageDirectory = "/ftpmirror/pub/sword/packages/rawzip";
        const string zipExtension = ".zip";
        List<SwordBookMetaData> metadatas;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var uri = "http://www.crosswire.org//ftpmirror/pub/sword/raw/mods.d.tar.gz";
            var client = new HttpClient();
            var response = await client.GetAsync(uri);
            var downloadedDataStream = await response.Content.ReadAsStreamAsync();
            var gzipStream = new GZipStream(downloadedDataStream, CompressionMode.Decompress);
            using(gzipStream)
            {
                metadatas = GetMetaDatasFromStream(gzipStream);
            }
            var bibleKralicka = metadatas.Find(x => x.InternalName == "czebkr");
            await DownloadBookAsync(bibleKralicka);
            string genesis1 = await ReadChapterAsync("czebkr", "Gen", 0);
        }

        List<SwordBookMetaData> GetMetaDatasFromStream(GZipStream gzipStream)
        {
            var metadatas = new List<SwordBookMetaData>();
            var tarInputStream = new TarInputStream(gzipStream);
            string configDirectory = BibleZtextReader.DirConf + '/';

            TarEntry entry;
            while ((entry = tarInputStream.GetNextEntry()) != null)
            {
                string filename = entry.Name;
                if (!entry.IsDirectory)
                {
                    var size = (int)entry.Size;

                    // Every now and then an empty entry sneaks in
                    if (size == 0)
                    {
                        Logger.Fail("Empty entry: " + filename);
                        continue;
                    }

                    var buffer = new byte[size];
                    using(var entryStream = new MemoryStream(buffer) { Position = 0 })
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
            return metadatas;
        }

        string RemoveExtensionFromPath(string path, string extension)
            => path.Substring(0, path.Length - extension.Length);
        string RemoveDirectoryFromPath(string path, string directoryPath)
            => path.Substring(directoryPath.Length);

        // DOwnloads zip archive and extract files into a filesystem (app folder)
        public async Task DownloadBookAsync(SwordBookMetaData bookMetadata)
        {
            string pathToHost = "http://" + crosswireServer + packageDirectory + "/" + bookMetadata.Initials + zipExtension;
            var client = new HttpClient();
            var response = await client.GetAsync(pathToHost);
            if(response.IsSuccessStatusCode)
            {
                StorageFolder rootFolder = ApplicationData.Current.LocalFolder;
                Stream responseStream = await response.Content.ReadAsStreamAsync();
                using (var zipStream = new ZipArchive(responseStream))
                {
                    ReadOnlyCollection<ZipArchiveEntry> entries = zipStream.Entries;
                    foreach (ZipArchiveEntry zipArchiveEntry in entries)
                    {
                        // make sure it is not just a directory
                        // TODO: into a method
                        if (!zipArchiveEntry.FullName.EndsWith("\\") && !zipArchiveEntry.FullName.EndsWith("/") && zipArchiveEntry.Length != 0)
                        {
                            await CreatePathInStorageIfNotExists(zipArchiveEntry.FullName);

                            //TODO: Access denied on creation
                            StorageFile file =
                                await
                                rootFolder.CreateFileAsync(
                                    zipArchiveEntry.FullName.Replace("/", "\\"), CreationCollisionOption.ReplaceExisting);
                            using (Stream fileStream = await file.OpenStreamForWriteAsync())
                            {
                                var buffer = new byte[10000];
                                int bytesRead;
                                using (Stream zipEntryStream = zipArchiveEntry.Open())
                                {
                                    while ((bytesRead = zipEntryStream.Read(buffer, 0, buffer.GetUpperBound(0))) > 0)
                                    {
                                        fileStream.Write(buffer, 0, bytesRead);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static async Task CreatePathInStorageIfNotExists(string path)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string[] directories = path.Split("/\\".ToCharArray());
            directories = directories.Take(directories.Length - 1).ToArray();
            foreach(string directory in directories)
            {
                IReadOnlyList<StorageFolder> existingFolders = await folder.CreateFolderQuery().GetFoldersAsync();
                if (!existingFolders.Any(p => p.Name.Equals(directory)))
                {
                    folder = await folder.CreateFolderAsync(directory);
                }
                else
                {
                    folder = await folder.GetFolderAsync(directory);
                }
            }
        }

        async Task<string> ReadChapterAsync(string bibleCodeName, string bookShortName, int chapter)
        {
            var book = metadatas.Find(x => x.InternalName == bibleCodeName);
            string bookPath = book.GetCetProperty(ConfigEntryType.ADataPath).ToString().Substring(2); // Remove "./" on the beginning
            bool isIsoEncoding = !book.GetCetProperty(ConfigEntryType.Encoding).Equals("UTF-8");
            string modDrv = ((string)book.GetProperty(ConfigEntryType.ModDrv)).ToUpper();
            if (modDrv.Equals("ZTEXT"))
            {
                //Initialize (get book,chapter,verse positions):
                var language = book.GetCetProperty(ConfigEntryType.Lang);
                string versification = book.GetCetProperty(ConfigEntryType.Versification) as string;
                var canon = CanonManager.GetCanon(versification);
                var bibleLoader = new BibleLoader();
                var oldTestamentChapterPositions = await bibleLoader.LoadVersePositionsAsync(bookPath, "ot", 0, canon.OldTestBooks.Count(), canon.OldTestBooks, canon);
                var newTestamentChapterPositions = await bibleLoader.LoadVersePositionsAsync(bookPath, "nt", canon.OldTestBooks.Count(), canon.OldTestBooks.Count() + canon.NewTestBooks.Count(), canon.NewTestBooks, canon);
                var chapterPositions = oldTestamentChapterPositions.Concat(newTestamentChapterPositions).ToList();
                var bibleReader = new BibleZTextReader(chapterPositions, (language as Language).Code, canon, bookPath, book.Name);
                return await bibleReader.GetChapterHtml(new DisplaySettings(), bookShortName, chapter, false, true);
            }
            return "Unknown modDrv";
        }
    }
}
