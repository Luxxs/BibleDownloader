using Ionic.Zlib;
using Sword;
using Sword.versification;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace GUI.BibleReader
{
	class BibleLoader
	{
		public Canon Canon { get; }
		readonly string bookPath;

		public BibleLoader(SwordBookMetaData swordBookMetadata)
		{
			bookPath = swordBookMetadata.GetCetProperty(ConfigEntryType.ADataPath).ToString().Substring(2).Replace("/", "\\"); // Remove "./" on the beginning, change '/' to '\'
			string versification = swordBookMetadata.GetCetProperty(ConfigEntryType.Versification) as string;
			Canon = CanonManager.GetCanon(versification);
		}

		// Load book positions from versification file
		public async Task<List<ChapterPosition>> LoadVersePositionsAsync(Testament testament)
        {
			string bzsFilePath = CreateBzsFilePath(bookPath, testament, IndexingBlockType.Book);
			List<BookPosition> bookPositions = await LoadBookPositionsAsync(bzsFilePath);

			CanonBookDef[] booksInFile = testament == Testament.Old ? Canon.OldTestBooks : Canon.NewTestBooks;

			string filePath = CreateVersificationFilePath(bookPath, testament, IndexingBlockType.Book);
			using (Stream fileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath))
            {
                for (int i = 0; i < 4; i++)
                    DumpPost(fileStream);

                List<ChapterPosition> chapters = new List<ChapterPosition>();
				for (int bookIndex = 0; bookIndex < booksInFile.Length; bookIndex++)
                {
                    long bookStartPosition = bookPositions[bookIndex+1].StartPosition ;

                    CanonBookDef bookDefinition = booksInFile[bookIndex];
                    for (int chapterIndex = 0; chapterIndex < bookDefinition.NumberOfChapters; chapterIndex++)
					{
						const int invalidChapterStartPosition = -1; // TODO: use Default property or int? or something instead
						ChapterPosition chapterPosition = new ChapterPosition(bookStartPosition, bookIndex, invalidChapterStartPosition, chapterIndex);
						long lastNonZeroStartPos = 0;
						long lastNonZeroLength = 0;
						int length = 0;

                        for (int k = 0; k < Canon.VersesInChapter[bookDefinition.VersesInChapterStartIndex + chapterIndex]; k++)
                        {
                            int verseBookNumber = GetShortIntFromStream(fileStream, out var isEnd);
                            long verseStartPosition = GetInt48FromStream(fileStream, out isEnd);
                            if (verseStartPosition != 0)
                            {
                                lastNonZeroStartPos = verseStartPosition;
                            }

                            length = GetShortIntFromStream(fileStream, out isEnd);

                            if (length != 0)
                            {
                                lastNonZeroLength = length;
                                chapterPosition.IsEmpty = false;

                                if (chapterPosition.ChapterStartPosition == invalidChapterStartPosition)
                                {
                                    chapterPosition.ChapterStartPosition = 0; // non-zero when this.BlockType != IndexingBlockType.Chapter, but we use only IndexingBlockType.Book, here
                                    chapterPosition.BookStartPosition = bookPositions[verseBookNumber].StartPosition;
                                }
                                chapterPosition.Verses.Add(new VersePosition(verseStartPosition - chapterPosition.ChapterStartPosition, length, bookIndex));
                            }
                            else
                            {
                                chapterPosition.Verses.Add(new VersePosition(0, 0, bookIndex));
                            }
                        }
                        chapterPosition.Length = (int)(lastNonZeroStartPos - chapterPosition.ChapterStartPosition + lastNonZeroLength);
                        chapters.Add(chapterPosition);
                        DumpPost(fileStream);
                    }
                    DumpPost(fileStream);
                }
                return chapters;
            }
        }

        async Task<List<BookPosition>> LoadBookPositionsAsync(string filePath)
        {
            using (Stream fileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath))
            {
                var bookPositions = new List<BookPosition>();
                bool isEnd;
                while (true)
                {
                    long start = GetIntFromStream(fileStream, out isEnd);
                    if (isEnd)
                        break;

                    long length = GetIntFromStream(fileStream, out isEnd);
                    if (isEnd)
                        break;

                    long unused = GetIntFromStream(fileStream, out isEnd);
                    if (isEnd)
                        break;

                    bookPositions.Add(new BookPosition(start, length, unused));
                }
                return bookPositions;
            }
		}

		string CreateVersificationFilePath(string bookPath, Testament testament, IndexingBlockType blockType)
			=> bookPath + (testament == Testament.Old ? "ot" : "nt") + "." + (char)blockType + "zv";

		string CreateBzsFilePath(string bookPath, Testament testament, IndexingBlockType blockType)
			=> bookPath + (testament == Testament.Old ? "ot" : "nt") + "." + (char)blockType + "zs";

		string CreateBzzFilePath(int absoluteChapterNumber, Testament testament, IndexingBlockType blockType)
			=> bookPath + (testament == Testament.Old ? "ot" : "nt") + "." + (char)blockType + "zz";

		void DumpPost(Stream stream)
        {
            GetShortIntFromStream(stream, out bool isEnd);
            GetInt48FromStream(stream, out isEnd);
            GetShortIntFromStream(stream, out isEnd);
        }

        long GetIntFromStream(Stream fs, out bool isEnd)
        {
            var buf = new byte[4];
            isEnd = fs.Read(buf, 0, 4) != 4;
            if (isEnd)
            {
                return 0;
            }

            return buf[3] * 0x100000 + buf[2] * 0x10000 + buf[1] * 0x100 + buf[0];
        }
        long GetInt48FromStream(Stream fs, out bool isEnd)
        {
            var buf = new byte[6];
            isEnd = fs.Read(buf, 0, 6) != 6;
            if (isEnd)
            {
                return 0;
            }

            return buf[1] * 0x100000000000 + buf[0] * 0x100000000 + buf[5] * 0x1000000 + buf[4] * 0x10000
                   + buf[3] * 0x100 + buf[2];
        }
        int GetShortIntFromStream(Stream fs, out bool isEnd)
        {
            var buf = new byte[2];
            isEnd = fs.Read(buf, 0, 2) != 2;
            if (isEnd)
            {
                return 0;
            }

            return buf[1] * 0x100 + buf[0];
        }


		public async Task<byte[]> GetChapterBytes(int absoluteChapterNumber, List<ChapterPosition> chapterPositions)
        {
            ChapterPosition versesPositionsForChapter = chapterPositions[absoluteChapterNumber];
            long blockStartPosition = versesPositionsForChapter.ChapterStartPosition;
            long blockLength = versesPositionsForChapter.Length;
			byte[] chapterBuffer = new byte[blockLength];

			Testament testamentOfTheChapter = IsChapterInOldTestament(absoluteChapterNumber) ? Testament.Old : Testament.New;
			string filePath = CreateBzzFilePath(absoluteChapterNumber, testamentOfTheChapter, IndexingBlockType.Book);
			using (Stream fileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath))
			{
				// adjust the start postion of the stream to where this book begins.
				// we must read the entire book up to the chapter we want even though we just want one chapter.
				fileStream.Position = versesPositionsForChapter.BookStartPosition;

				//zipStream = string.IsNullOrEmpty(this.Serial.CipherKey) ? new ZInputStream(fs) : new ZInputStream(new SapphireStream(fs, this.Serial.CipherKey));
				using (ZlibStream zipStream = new ZlibStream(fileStream, CompressionMode.Decompress))
				{
					int totalBytesRead = 0;
					int totalBytesCopied = 0;
					int len = 0;
					byte[] buffer = new byte[10000];
					while (true)
					{
						len = zipStream.Read(buffer, 0, 10000);
						if (len <= 0)
						{
							// we should never come to this point.  Just here as a safety procaution
							break;
						}

						totalBytesRead += len;
						if (totalBytesRead >= blockStartPosition)
						{
							// we are now inside of where the chapter we want is so we need to start saving it.
							int startOffset = 0;
							if (totalBytesCopied == 0)
							{
								// but our actual chapter might begin in the middle of the buffer.  Find the offset from the
								// beginning of the buffer.
								startOffset = len - (totalBytesRead - (int)blockStartPosition);
							}

							for (int i = totalBytesCopied; i < blockLength && (i - totalBytesCopied) < (len - startOffset); i++)
							{
								chapterBuffer[i] = buffer[i - totalBytesCopied + startOffset];
							}

							totalBytesCopied += len - startOffset;
							if (totalBytesCopied >= blockLength)
							{
								// we are done. no more reason to read anymore of this book stream, just get out.
								break;
							}
						}
					}
				}
			}
            return chapterBuffer;
		}

		bool IsChapterInOldTestament(int absoluteChapterNumber)
		{
			CanonBookDef lastBookInOldTestament = Canon.OldTestBooks.Last();
			int lastChapterAbsoluteNumber = lastBookInOldTestament.VersesInChapterStartIndex;
			return absoluteChapterNumber < lastChapterAbsoluteNumber + lastBookInOldTestament.NumberOfChapters;
		}
	}
}
