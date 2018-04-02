using Ionic.Zlib;
using Sword.reader;
using Sword.versification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace GUI.BibleReader
{
    class BibleLoader
    {
        // Load book positions from versification file
        public async Task<List<ChapterPosition>> LoadVersePositionsAsync(string bookPath, string fileName, int startBookIndex, int endBookIndex, CanonBookDef[] booksInFile, Canon canon)
        {
            List<BookPosition> bookPositions = await LoadBookPositionsAsync(bookPath, fileName);
            string filePath = bookPath.Replace("/", "\\") + fileName + "." + ((char)IndexingBlockType.Book) + "zv";
            using (Stream fileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath))
            {
                for (int i = 0; i < 4; i++)
                    DumpPost(fileStream);

                List<ChapterPosition> chapters = new List<ChapterPosition>();
                for (int bookIndex = startBookIndex; bookIndex < endBookIndex; bookIndex++)
                {
                    int bookIndexInCanon = bookIndex - startBookIndex;
                    int bookNumber = bookIndex - startBookIndex + 1;
                    long bookStartPosition = bookNumber < bookPositions.Count ? bookPositions[bookNumber].StartPosition : 0;

                    CanonBookDef bookDefinition = booksInFile[bookIndex - startBookIndex];
                    for (int chapterIndex = 0; chapterIndex < bookDefinition.NumberOfChapters; chapterIndex++)
                    {
                        const int invalidChapterStartPosition = -1;
                        ChapterPosition chapterPosition = new ChapterPosition(bookStartPosition, bookIndex, invalidChapterStartPosition, chapterIndex);
                        long lastNonZeroStartPos = 0;
                        long lastNonZeroLength = 0;
                        int length = 0;

                        for (int k = 0; k < canon.VersesInChapter[bookDefinition.VersesInChapterStartIndex + chapterIndex]; k++)
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
        async Task<List<BookPosition>> LoadBookPositionsAsync(string bookPath, string fileName)
        {
            string filePath = bookPath.Replace("/", "\\") + fileName + "." + ((char)IndexingBlockType.Book) + "zs";
            using (Stream fileStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filePath))
            {
                // read book position index
                var bookPositions = new List<BookPosition>();
                bool isEnd;
                while (true)
                {
                    long start = this.GetintFromStream(fileStream, out isEnd);
                    if (isEnd)
                        break;

                    long length = this.GetintFromStream(fileStream, out isEnd);
                    if (isEnd)
                        break;

                    long unused = this.GetintFromStream(fileStream, out isEnd);
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

        private enum IndexingBlockType
        {
            Book = 'b',
            Chapter = 'c'
        }
        long GetintFromStream(Stream fs, out bool isEnd)
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

        public static async Task<byte[]> GetChapterBytes(int absoluteChapterNumber, string bookPath, List<ChapterPosition> chapterPositions, Canon canon)
        {
            //Debug.WriteLine("getChapterBytes start");
            /*
            int numberOfChapters = chapterPositions.Count;
            if (numberOfChapters == 0)
            {
                return Encoding.UTF8.GetBytes("Does not exist");
            }
            if (chapterNumber >= numberOfChapters)
            {
                chapterNumber = numberOfChapters - 1;
            }

            if (chapterNumber < 0)
            {
                chapterNumber = 0;
            }*/

            ChapterPosition versesForChapterPositions = chapterPositions[absoluteChapterNumber];
            long bookStartPos = versesForChapterPositions.BookStartPosition;
            long blockStartPos = versesForChapterPositions.ChapterStartPosition;
            long blockLen = versesForChapterPositions.Length;
            Stream fs;
            var lastBookInOldTestament = canon.OldTestBooks[canon.OldTestBooks.Count() - 1];
            //TODO: condition into IsChapterInOldTestament method
            string fileName = (absoluteChapterNumber < (lastBookInOldTestament.VersesInChapterStartIndex + lastBookInOldTestament.NumberOfChapters)) ? "ot." : "nt.";
            try
            {
                //Windows.Storage.ApplicationData appData = Windows.Storage.ApplicationData.Current;
                //var folder = await appData.LocalFolder.GetFolderAsync(Serial.Path.Replace("/", "\\"));
                string filenameComplete = bookPath + fileName + ((char)IndexingBlockType.Book) + "zz";
                fs =
                    await
                    ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filenameComplete.Replace("/", "\\"));
                //fs = await file.OpenStreamForReadAsync();
            }
            catch (Exception ee)
            {
                // does not exist
                return Encoding.UTF8.GetBytes("File does not exist");
            }

            // adjust the start postion of the stream to where this book begins.
            // we must read the entire book up to the chapter we want even though we just want one chapter.
            fs.Position = bookStartPos;
            //ZInputStream zipStream;
            //zipStream = string.IsNullOrEmpty(this.Serial.CipherKey) ? new ZInputStream(fs) : new ZInputStream(new SapphireStream(fs, this.Serial.CipherKey));
            ZlibStream zipStream;
            zipStream = new ZlibStream(fs, Ionic.Zlib.CompressionMode.Decompress);

            var chapterBuffer = new byte[blockLen];
            int totalBytesRead = 0;
            int totalBytesCopied = 0;
            int len = 0;
            try
            {
                var buffer = new byte[10000];
                while (true)
                {
                    try
                    {
                        len = zipStream.Read(buffer, 0, 10000);
                    }
                    catch (Exception ee)
                    {
                        Debug.WriteLine("caught a unzip crash 4.2" + ee);
                    }

                    if (len <= 0)
                    {
                        // we should never come to this point.  Just here as a safety procaution
                        break;
                    }

                    totalBytesRead += len;
                    if (totalBytesRead >= blockStartPos)
                    {
                        // we are now inside of where the chapter we want is so we need to start saving it.
                        int startOffset = 0;
                        if (totalBytesCopied == 0)
                        {
                            // but our actual chapter might begin in the middle of the buffer.  Find the offset from the
                            // beginning of the buffer.
                            startOffset = len - (totalBytesRead - (int)blockStartPos);
                        }
                        
                        for (int i = totalBytesCopied; i < blockLen && (i - totalBytesCopied) < (len - startOffset); i++)
                        {
                            chapterBuffer[i] = buffer[i - totalBytesCopied + startOffset];
                        }

                        totalBytesCopied += len - startOffset;
                        if (totalBytesCopied >= blockLen)
                        {
                            // we are done. no more reason to read anymore of this book stream, just get out.
                            break;
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine("BibleZtextReader.getChapterBytes crash; " + ee.Message);
            }

            fs.Dispose();
            zipStream.Dispose();
            return chapterBuffer;
        }
    }
}
