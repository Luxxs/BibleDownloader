#region Header

// <copyright file="BibleZtextReader.cs" company="Thomas Dilts">
// CrossConnect Bible and Bible Commentary Reader for CrossWire.org
// Copyright (C) 2011 Thomas Dilts
// This program is free software: you can redistribute it and/or modify
// it under the +terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.
// </copyright>
// <summary>
// Email: thomas@cross-connect.se
// </summary>
// <author>Thomas Dilts</author>

#endregion Header

namespace Sword.reader
{
    using Sword.versification;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    [DataContract]
    public class BiblePlaceMarker
    {
        #region Fields

        [DataMember(Name = "BookShortName")]
        public string BookShortName = string.Empty;

        [DataMember(Name = "chapterNum")]
        public int ChapterNum = 1;

        [DataMember(Name = "note")]
        public string Note = string.Empty;

        [DataMember(Name = "verseNum")]
        public int VerseNum = 1;

        [DataMember(Name = "when")]
        public DateTime When;

        #endregion Fields

        #region Constructors and Destructors

        public BiblePlaceMarker(string bookShortName, int chapterNum, int verseNum, DateTime when)
        {
            this.BookShortName = bookShortName;
            this.ChapterNum = chapterNum;
            this.VerseNum = verseNum;
            this.When = when;
        }

        #endregion Constructors and Destructors

        #region Public Methods and Operators

        public override string ToString()
        {
            return this.ChapterNum + ";" + this.VerseNum;
        }

        public static BiblePlaceMarker Clone(BiblePlaceMarker toClone)
        {
            var newMarker = new BiblePlaceMarker(toClone.BookShortName, toClone.ChapterNum, toClone.VerseNum, toClone.When)
            {
                Note =
                    toClone
                    .Note
            };
            return newMarker;
        }

        public static List<BiblePlaceMarker> Clone(List<BiblePlaceMarker> toClone)
        {
            var returnedClone = new List<BiblePlaceMarker>();
            foreach (var item in toClone)
            {
                returnedClone.Add(BiblePlaceMarker.Clone(item));
            }
            return returnedClone;
        }

        #endregion Public Methods and Operators
    }

    /// <summary>
    ///     Load from a file all the book and verse pointers to the bzz file so that
    ///     we can later read the bzz file quickly and efficiently.
    /// </summary>
    [DataContract]
    public class BibleZtextReader
    {
        #region Constants

        /// <summary>

        ///     * The configuration directory
        /// </summary>
        public const string DirConf = "mods.d";

        /// <summary>
        ///     * The data directory
        /// </summary>
        public const string DirData = "modules";

        /// <summary>
        ///     * Extension for config files
        /// </summary>
        public const string ExtensionConf = ".conf";

        /// <summary>
        ///     * Extension for data files
        /// </summary>
        public const string ExtensionData = ".dat";

        /// <summary>
        ///     * Extension for index files
        /// </summary>
        public const string ExtensionIndex = ".idx";

        /// <summary>
        ///     * Index file extensions
        /// </summary>
        public const string ExtensionVss = ".vss";

        /// <summary>
        ///     * New testament data files
        /// </summary>
        public const string FileNt = "nt";

        /// <summary>
        ///     * Old testament data files
        /// </summary>
        public const string FileOt = "ot";

        /// <summary>
        ///     Constant for the number of verses in the Bible
        /// </summary>
        //internal const short VersesInBible = 31102;

        protected const long SkipBookFlag = 68;

        #endregion Constants
    }
}