using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BibleLoader;
using BibleLoader.Enums;
using BibleLoader.Interfaces;
using Sword;

namespace BibleReaderConsole
{
    class BibleFileManager : BibleFileManagerBase, IBibleFileManager
    {
        DirectoryInfo rootFolder = new DirectoryInfo("Bibles");

        /// <summary>
        /// Unzips bible files from Stream.
        /// </summary>
        public async Task SaveBible(Stream responseStream)
        {
            using (var zipStream = new ZipArchive(responseStream))
            {
                ReadOnlyCollection<ZipArchiveEntry> entries = zipStream.Entries;
                foreach (ZipArchiveEntry zipArchiveEntry in entries)
                {
                    if (!IsZipEntryDirectory(zipArchiveEntry))
                    {
                        var normalizedZipEntryPath = NormalizePath(zipArchiveEntry.FullName);
                        CreatePathInStorageIfNotExists(normalizedZipEntryPath);
                        var filePath = Path.Combine(rootFolder.FullName, normalizedZipEntryPath);
                        using (FileStream fileStream = File.Create(filePath, 10000, FileOptions.None))
                        {
                            var buffer = new byte[10000];
                            using (Stream zipEntryStream = zipArchiveEntry.Open())
                            {
                                int bytesRead;
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

        /// <summary>
        /// Returs true if all bible files are present in filesystem.
        /// </summary>
        public bool IsBibleSaved(SwordBookMetaData swordBookMetaData)
        {
            var fileChecks = new List<bool>
            {
                new FileInfo(CreateVersificationFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(CreateBzsFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(CreateBzzFilePath(swordBookMetaData, Testament.Old)).Exists,
                new FileInfo(CreateVersificationFilePath(swordBookMetaData, Testament.New)).Exists,
                new FileInfo(CreateBzsFilePath(swordBookMetaData, Testament.New)).Exists,
                new FileInfo(CreateBzzFilePath(swordBookMetaData, Testament.New)).Exists
            };
            return fileChecks.All(x => x);
        }

        public async Task<Stream> OpenBzsFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = CreateBzsFilePath(swordBookMetaData, testament);
            return File.OpenRead(filePath);
        }

        public async Task<Stream> OpenBzzFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = CreateBzzFilePath(swordBookMetaData, testament);
            return File.OpenRead(filePath);
        }

        public async Task<Stream> OpenVersificationFileForReadAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            string filePath = CreateVersificationFilePath(swordBookMetaData, testament);
            return File.OpenRead(filePath);
        }

        void CreatePathInStorageIfNotExists(string normalizedPath)
        {
            DirectoryInfo folder = rootFolder;
            if(!folder.Exists)
                folder.Create();

            string[] directories = normalizedPath.Split(Path.DirectorySeparatorChar);
            foreach (string directory in directories.Take(directories.Length - 1))
            {
                folder = folder.GetDirectories().FirstOrDefault(p => p.Name.Equals(directory)) ??
                         folder.CreateSubdirectory(directory);
            }
        }

        string NormalizePath(string path)
            => path.Replace('/', Path.DirectorySeparatorChar);

        new string CreateVersificationFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.CreateVersificationFilePath(swordBookMetaData, testament)));

        new string CreateBzsFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.CreateBzsFilePath(swordBookMetaData, testament)));

        new string CreateBzzFilePath(SwordBookMetaData swordBookMetaData, Testament testament)
            => Path.Combine(rootFolder.FullName,
                NormalizePath(base.CreateBzzFilePath(swordBookMetaData, testament)));
    }
}
