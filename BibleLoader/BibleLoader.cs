using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BibleLoader.Dto;
using BibleLoader.Enums;
using BibleLoader.Interfaces;
using Ionic.Zlib;
using Sword;
using Sword.versification;

namespace BibleLoader
{
    public class BibleLoader : IBibleLoader
    {
        readonly IBibleFileManager bibleFileManager;

        public BibleLoader(IBibleFileManager bibleFileManager)
        {
            this.bibleFileManager = bibleFileManager;
        }

        public async Task<List<ChapterPosition>> LoadChapterPositionsAsync(SwordBookMetaData swordBookMetaData)
        {
            var oldTestamentChapterPositions = await LoadChapterPositionsAsync(swordBookMetaData, Testament.Old);
            var newTestamentChapterPositions = await LoadChapterPositionsAsync(swordBookMetaData, Testament.New);
            return oldTestamentChapterPositions.Concat(newTestamentChapterPositions).ToList();
        }

        public async Task<byte[]> GetChapterBytesAsync(SwordBookMetaData swordBookMetaData, int absoluteChapterNumber, List<ChapterPosition> chapterPositions)
        {
            ChapterPosition versesPositionsForChapter = chapterPositions[absoluteChapterNumber];
            long blockStartPosition = versesPositionsForChapter.ChapterStartPosition;
            long blockLength = versesPositionsForChapter.Length;
            byte[] chapterBuffer = new byte[blockLength];

            Testament testamentOfTheChapter = IsChapterInOldTestament(swordBookMetaData, absoluteChapterNumber) ? Testament.Old : Testament.New;
            using (Stream fileStream = await bibleFileManager.OpenBzzFileForReadAsync(swordBookMetaData, testamentOfTheChapter))
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

        // Load book positions from versification file
        async Task<List<ChapterPosition>> LoadChapterPositionsAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            List<BookPosition> bookPositions = await LoadBookPositionsAsync(swordBookMetaData, testament);

            var canon = swordBookMetaData.GetCanon();
            CanonBookDef[] booksInFile = testament == Testament.Old ? canon.OldTestBooks : canon.NewTestBooks;

            using (Stream fileStream = await bibleFileManager.OpenVersificationFileForReadAsync(swordBookMetaData, testament))
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

                        for (int k = 0; k < canon.VersesInChapter[bookDefinition.VersesInChapterStartIndex + chapterIndex]; k++)
                        {
                            int verseBookNumber = GetShortIntFromStream(fileStream, out var isEnd);
                            long verseStartPosition = GetInt48FromStream(fileStream, out isEnd);
                            if (verseStartPosition != 0)
                            {
                                lastNonZeroStartPos = verseStartPosition;
                            }

                            int length = GetShortIntFromStream(fileStream, out isEnd);

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

        async Task<List<BookPosition>> LoadBookPositionsAsync(SwordBookMetaData swordBookMetaData, Testament testament)
        {
            using (Stream fileStream = await bibleFileManager.OpenBzsFileForReadAsync(swordBookMetaData, testament))
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

        bool IsChapterInOldTestament(SwordBookMetaData swordBookMetaData, int absoluteChapterNumber)
        {
            var canon = swordBookMetaData.GetCanon();
            CanonBookDef lastBookInOldTestament = canon.OldTestBooks.Last();
            int lastChapterAbsoluteNumber = lastBookInOldTestament.VersesInChapterStartIndex;
            return absoluteChapterNumber < lastChapterAbsoluteNumber + lastBookInOldTestament.NumberOfChapters;
        }
    }
}
