using ICSharpCode.SharpZipLib.Tar;
using Sword;
using Sword.reader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;

namespace GUI.BibleReader
{
	class BibleDownloader
	{
		const string crosswireServer = "http://www.crosswire.org";
		const string metadatasUrls = "/ftpmirror/pub/sword/raw/mods.d.tar.gz";
		const string bookPackagesDirectory = "/ftpmirror/pub/sword/packages/rawzip";
		const string zipExtension = ".zip";

		string CreateUrlToBook(string bookInitials)
			=> crosswireServer + bookPackagesDirectory + "/" + bookInitials + zipExtension;

		HttpClient httpClient = new HttpClient();

		public async Task<List<SwordBookMetaData>> DownloadBookMetadatasAsync()
		{
			string url = crosswireServer + metadatasUrls;
			HttpResponseMessage response = await httpClient.GetAsync(url);
			Stream downloadedDataStream = await response.Content.ReadAsStreamAsync();
			var metadatas = new List<SwordBookMetaData>();
			using (var gzipStream = new GZipStream(downloadedDataStream, CompressionMode.Decompress))
			{
				metadatas = GetMetaDatasFromStream(gzipStream);
			}
			return metadatas;
		}

		List<SwordBookMetaData> GetMetaDatasFromStream(GZipStream gzipStream)
		{
			var metadatas = new List<SwordBookMetaData>();
			string configDirectory = BibleZtextReader.DirConf + '/';

			using (var tarInputStream = new TarInputStream(gzipStream))
			{
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


		/// <summary>
		/// Downloads zip archive and extract files into a filesystem (app folder)
		/// </summary>
		public async Task DownloadBookAsync(SwordBookMetaData bookMetadata)
		{
			string bookUrl = CreateUrlToBook(bookMetadata.Initials);
			HttpResponseMessage response = await httpClient.GetAsync(bookUrl);
			if (response.IsSuccessStatusCode)
			{
				StorageFolder rootFolder = ApplicationData.Current.LocalFolder;
				Stream responseStream = await response.Content.ReadAsStreamAsync();
				using (var zipStream = new ZipArchive(responseStream))
				{
					ReadOnlyCollection<ZipArchiveEntry> entries = zipStream.Entries;
					foreach (ZipArchiveEntry zipArchiveEntry in entries)
					{
						if (!IsZipEntryDirectory(zipArchiveEntry))
						{
							await CreatePathInStorageIfNotExistsAsync(zipArchiveEntry.FullName);

							StorageFile file = await
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

		bool IsZipEntryDirectory(ZipArchiveEntry zipArchiveEntry)
			=> zipArchiveEntry.FullName.EndsWith("\\") && !zipArchiveEntry.FullName.EndsWith("/") && zipArchiveEntry.Length != 0;

		static async Task CreatePathInStorageIfNotExistsAsync(string path)
		{
			StorageFolder folder = ApplicationData.Current.LocalFolder;
			string[] directories = path.Split("/\\".ToCharArray());
			directories = directories.Take(directories.Length - 1).ToArray();
			foreach (string directory in directories)
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
	}
}
