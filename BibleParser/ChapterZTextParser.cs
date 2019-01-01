using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using BibleLoader.DTO;
using BibleParser.VerseElements;
using BibleParser.VerseElements.Interfaces;

namespace BibleParser
{
    class ChapterZTextParser : IChapterParser
    {
        const string XmlPrefix = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><versee>";
        const string XmlPrefixIso = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><versee>";
        const string XmlSuffix = "</versee>";

        public Chapter ParseChapterAsync(int chapterNumber, byte[] chapterBytes, ChapterPosition versePositionsInChapter)
        {
            var chapter = new Chapter
            {
                Number = chapterNumber,
                Verses = new List<Verse>()
            };

            var usedVersePositions = new List<long>();
            int verseNumber = 1;
            foreach (VersePosition versePosition in versePositionsInChapter.Verses)
            {
                // in some commentaries, the verses repeat. Stop these repeats from comming in!
                if (!usedVersePositions.Contains(versePosition.StartPosition))
                {
                    usedVersePositions.Add(versePosition.StartPosition);
                    var verse = ParseOsisVerseSimplified(chapterBytes, (int)versePosition.StartPosition, versePosition.Length, false);
                    verse.Number = verseNumber++;
                    chapter.Verses.Add(verse);
                }
            }
            return chapter;
        }

        Verse ParseOsisVerseSimplified(
            byte[] chapterBytes,
            int verseStartPosition,
            int verseLength,
            bool isIsoText)
        {
            // Some indexes are bad. make sure the startpos and length are not bad
            if (verseLength == 0)
            {
                throw new Exception("Verse is empty");
            }

            if (verseStartPosition >= chapterBytes.Length)
            {
                throw new Exception("The verse is out of chapter");
            }

            // Remove space at the end of every verse
            verseLength = verseLength - Encoding.UTF8.GetBytes(" ").Length;

            if (verseStartPosition + verseLength > chapterBytes.Length)
            {
                // we can fix this
                verseLength = chapterBytes.Length - verseStartPosition - 1;
                if (verseLength == 0)
                    throw new Exception("Verse overlapped the chapter, could not fix");
            }
            
            // try to remove null characters at the end
            for (int i = verseLength - 1; i > verseStartPosition; i--)
            {
                if (chapterBytes[verseStartPosition + i] == 0)
                    verseLength--;
                else
                    break;
            }

            using (var verseXml = new MemoryStream())
            {
                InsertXmlPrefixIntoStream(verseXml, isIsoText);
                verseXml.Write(chapterBytes, verseStartPosition, verseLength);
                InsertXmlSuffixIntoStream(verseXml);
                verseXml.Position = 0;

                var settings = new XmlReaderSettings
                {
                    IgnoreWhitespace = false
                };
                using (XmlReader reader = XmlReader.Create(verseXml, settings))
                {
                    var verse = new Verse
                    {
                        Content = new List<IVerseElement>()
                    };

                    Stack<IVerseElement> elementsToAdd = new Stack<IVerseElement>();

                    try
                    {
                        // Parse the file and get each of the nodes.
                        while (reader.Read())
                        {
                            switch (reader.NodeType)
                            {
                                case XmlNodeType.SignificantWhitespace:
                                case XmlNodeType.Whitespace:
                                    if (elementsToAdd.Any())
                                        verse.Content.Add(elementsToAdd.Pop());
                                    elementsToAdd.Push(new TextElement { Text = reader.Value });
                                    break;

                                case XmlNodeType.Element:
                                    switch (reader.Name.ToLower())
                                    {
                                        case "img":
                                        case "figure":
                                            // TODO
                                            break;

                                        case "title":
                                            //Should be always in the beginning of a verse
                                            elementsToAdd.Push(new Title());
                                            break;

                                        case "rf":
                                        case "note":
                                            if(elementsToAdd.Any())
                                                verse.Content.Add(elementsToAdd.Pop());
                                            elementsToAdd.Push(new Note
                                            {
                                                Identifier = reader.GetAttribute("n")
                                            });
                                            break;

                                        case "hi":
                                            elementsToAdd.Push(new Highlight
                                            {
                                                Type = reader.GetAttribute("type")
                                            });
                                            break;

                                        case "br":
                                            if (elementsToAdd.Any())
                                                verse.Content.Add(elementsToAdd.Pop());
                                            verse.Content.Add(new LineBreak());
                                            break;

                                        case "p":
                                            if (elementsToAdd.Any())
                                                verse.Content.Add(elementsToAdd.Pop());
                                            var paragraphSeparator = new ParagraphSeparator();
                                            if (reader.IsEmptyElement)
                                                verse.Content.Add(paragraphSeparator);
                                            else
                                                elementsToAdd.Push(paragraphSeparator);
                                            break;

                                        case "transchange":
                                            if (elementsToAdd.Any())
                                                verse.Content.Add(elementsToAdd.Pop());
                                            elementsToAdd.Push(new ChangeInTranslation {
                                                Type = reader.GetAttribute("type")
                                            });
                                            break;

                                        case "cm":
                                        case "lb":
                                        case "scripref":
                                        case "reference":
                                        case "lg":
                                        case "l":
                                        case "fi":
                                        case "q":
                                        case "w":// <w lemma="strong:G1078" morph="robinson:N-GSF">γενεσεως</w>
                                            // Don't know what is this for
                                            // TODO: Deal with this, since it is causing multiple TextElements being inserted in a row
                                            break;
                                    }
                                    break;

                                case XmlNodeType.Text:
                                    var newTextElement = new TextElement { Text = reader.Value };
                                    if (elementsToAdd.Any())
                                    {
                                        if (elementsToAdd.Peek() is IContentVerseElement contentElementToAdd)
                                            contentElementToAdd.Content.Add(newTextElement);
                                        else if (elementsToAdd.Peek() is ITextVerseElement textElementToAdd)
                                            textElementToAdd.Text += reader.Value;
                                    }
                                    else
                                        elementsToAdd.Push(newTextElement);
                                    break;

                                case XmlNodeType.EndElement:
                                    switch (reader.Name.ToLower())
                                    {
                                        case "transchange":
                                        case "title":
                                        case "note":
                                        case "hi":
                                        case "p":
                                            var finishedElement = elementsToAdd.Pop();
                                            if (elementsToAdd.Any() && elementsToAdd.Peek() is IContentVerseElement currentContentElement)
                                                currentContentElement.Content.Add(finishedElement);
                                            else
                                                verse.Content.Add(finishedElement);
                                            break;

                                        case "reference":
                                        case "q":
                                        case "w": // <w lemma="strong:G1078" morph="robinson:N-GSF">γενεσεως</w>
                                        case "lg":
                                        case "l":
                                        case "scripref":
                                            // Don't know what is this for
                                            break;

                                        case "img":
                                        case "figure":
                                            // nothing needed; already "handled"
                                            break;
                                    }
                                    break;
                            }
                        }
                        verse.Content.Add(elementsToAdd.Pop());
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("BibleZtextReader.parseOsisText " + e.Message);
                    }
                    return verse;
                }
            }

            // TODO: this replace fixes a character translation problem for slanted apostrophy
            //return new string[] { plainText.ToString().Replace('\x92', '\''), isInPoetry.ToString(), noteIdentifier.ToString() };
        }

        void InsertXmlPrefixIntoStream(Stream stream, bool isIso)
        {
            var xmlPrefix = isIso ? Encoding.UTF8.GetBytes(XmlPrefixIso) : Encoding.UTF8.GetBytes(XmlPrefix);
            stream.Write(xmlPrefix, 0, xmlPrefix.Length);
        }
        void InsertXmlSuffixIntoStream(Stream stream)
        {
            var xmlPrefix = Encoding.UTF8.GetBytes(XmlSuffix);
            stream.Write(xmlPrefix, 0, xmlPrefix.Length);
        }

        string convertNoteNumToId(int noteIdentifier)
        {
            string base26 = string.Empty;
            do
            {
                base26 += (char)(noteIdentifier % 26 + 'a');// IntToBase24[noteIdentifier % 26];
                noteIdentifier = noteIdentifier / 26;
            }
            while (noteIdentifier > 0);

            return "(" + base26.Reverse() + ")";
        }
    }
}
