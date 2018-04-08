using GUI.BibleParser.DataObjects;
using GUI.BibleParser.DataObjects.Interfaces;
using GUI.BibleReader;
using Sword.versification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GUI.BibleParser
{
	class BibleParser
	{
		const string XmlPrefix = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><versee>";
		const string XmlPrefixIso = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><versee>";
		const string XmlSuffix = "</versee>";

		BibleLoader bibleLoader;
		List<ChapterPosition> chapterPositions;

		public BibleParser(BibleLoader bibleLoader, List<ChapterPosition> chapterPositions)
		{
			this.bibleLoader = bibleLoader;
			this.chapterPositions = chapterPositions;
		}

		public async Task<Chapter> GetChapterAsync(string bookShortName, int chapterNumber)
		{
			var chapter = new Chapter
			{
				Number = chapterNumber,
				Verses = new List<Verse>()
			};

			CanonBookDef book = bibleLoader.Canon.BookByShortName[bookShortName];
			ChapterPosition versesPositionsForChapter = chapterPositions[book.VersesInChapterStartIndex + chapterNumber];

			byte[] chapterBytes = await bibleLoader.GetChapterBytesAsync(book.VersesInChapterStartIndex + chapterNumber, chapterPositions);
			var usedVersePositions = new List<long>();
			int verseNumber = 1;
			foreach (VersePosition versePosition in versesPositionsForChapter.Verses)
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

					Stack<IVerseElement> elements = new Stack<IVerseElement>();

					try
					{
						// Parse the file and get each of the nodes.
						while (reader.Read())
						{
							switch (reader.NodeType)
							{
								case XmlNodeType.SignificantWhitespace:
									if (elements.Any())
										verse.Content.Add(elements.Pop());
									elements.Push(new TextElement { Text = reader.Value });
									break;

								case XmlNodeType.Whitespace:
									if (elements.Any())
										verse.Content.Add(elements.Pop());
									elements.Push(new TextElement { Text = reader.Value });
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
											elements.Push(new Title());
											break;

										case "rf":
										case "note":
											verse.Content.Add(elements.Pop());
											elements.Push(new Note
											{
												Identifier = reader.GetAttribute("n")
											});
											break;

										case "hi":
											elements.Push(new Highlight
											{
												Type = reader.GetAttribute("type")
											});
											break;

										case "br":
											if (elements.Any())
												verse.Content.Add(elements.Pop());
											verse.Content.Add(new LineBreak());
											break;

										case "p":
											if (elements.Any())
												verse.Content.Add(elements.Pop());
											var paragraphSeparator = new ParagraphSeparator();
											if (reader.IsEmptyElement)
												verse.Content.Add(paragraphSeparator);
											else
												elements.Push(paragraphSeparator);
											break;

										case "transchange":
											if (elements.Any())
												verse.Content.Add(elements.Pop());
											elements.Push(new ChangeInTranslation {
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
									var textElement = new TextElement { Text = reader.Value };
									if (elements.Any())
									{
										if (elements.Peek() is IElementWithContent)
											(elements.Peek() as IElementWithContent).Content.Add(textElement);
										else
											(elements.Peek() as IElementWithText).Text += reader.Value;
									}
									else
										elements.Push(textElement);
									break;

								case XmlNodeType.EndElement:
									switch (reader.Name.ToLower())
									{
										case "transchange":
										case "title":
										case "note":
										case "hi":
										case "p":
											var endingElement = elements.Pop();
											if (elements.Any() && elements.Peek() is IElementWithContent)
												(elements.Peek() as IElementWithContent).Content.Add(endingElement);
											else
												verse.Content.Add(endingElement);
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
						verse.Content.Add(elements.Pop());
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

		// TODO: Use it in some HTML renderer
		byte[] FixEtcetera(Stream stream, byte[] chapterBytes)
		{
			//unfortunately "&c." means "etcetera" in old english
			//and "&c." really messes up html since & is a reserved word.
			string beforeFix = Encoding.UTF8.GetString(chapterBytes);
			string afterFix = beforeFix.Replace("&c.", "&amp;c.");
			return Encoding.UTF8.GetBytes(afterFix);
		}

		protected static string convertNoteNumToId(int noteIdentifier)
		{
			string base26 = string.Empty;
			do
			{
				base26 += (char)(noteIdentifier % 26 + 'a');// IntToBase24[noteIdentifier % 26];
				noteIdentifier = noteIdentifier / 26;
			}
			while (noteIdentifier > 0);

			return "(" + Reverse(base26) + ")";
		}
		public static string Reverse(string input)
		{
			char[] output = new char[input.Length];

			int forwards = 0;
			int backwards = input.Length - 1;

			do
			{
				output[forwards] = input[backwards];
				output[backwards] = input[forwards];
			} while (++forwards <= --backwards);

			return new String(output);
		}
	}
}
