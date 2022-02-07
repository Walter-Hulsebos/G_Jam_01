// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System;
using System.Text;

namespace Weaver
{
    /// <summary>
    /// Encapsulates a string to treat it as a variable substring without the memory allocation and garbage collection
    /// costs of <see cref="string.Substring(int, int)"/>.
    /// </summary>
    public sealed class Substring : IComparable<Substring>
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The original encapsulated string.</summary>
        public string rawString;

        /// <summary>The character index in the <see cref="rawString"/> of the start of this substring.</summary>
        public int startIndex;

        /// <summary>The character index in the <see cref="rawString"/> of the character immediately after the end of this substring.</summary>
        public int endIndex;

        /************************************************************************************************************************/

        /// <summary>The number of characters in the current substring.</summary>
        public int Length
        {
            get { return endIndex - startIndex; }
            set { endIndex = startIndex + value; }
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the <see cref="startIndex"/> and <see cref="endIndex"/> denote a valid substring within the <see cref="rawString"/>.</summary>
        public bool IsValid
        {
            get
            {
                return
                    startIndex >= 0 &&
                    endIndex <= rawString.Length &&
                    startIndex <= endIndex;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns true if the end index is at or past the end of the <see cref="rawString"/>.</summary>
        public bool IsAtEnd
        {
            get
            {
                return endIndex >= rawString.Length;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns <see cref="rawString"/>[<see cref="startIndex"/> + i]</summary>
        public char this[int i]
        {
            get
            {
                return rawString[startIndex + i];
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="Substring"/> without assigning the encapsulated string or indices.</summary>
        public Substring() { }

        /// <summary>Creates a new <see cref="Substring"/> with the <see cref="startIndex"/> at 0 and the <see cref="endIndex"/> equal to rawString.Length.</summary>
        public Substring(string rawString)
        {
            Set(rawString);
        }

        /// <summary>Creates a new <see cref="Substring"/> with the specified <see cref="startIndex"/> and the <see cref="endIndex"/> equal to rawString.Length.</summary>
        public Substring(string rawString, int startIndex)
        {
            Set(rawString, startIndex);
        }

        /// <summary>Creates a new <see cref="Substring"/> with the specified parameters.</summary>
        public Substring(string rawString, int startIndex, int endIndex)
        {
            Set(rawString, startIndex, endIndex);
        }

        /// <summary>Creates a new <see cref="Substring"/> as a copy of the specified `original`.</summary>
        public Substring(Substring original)
            : this(original.rawString, original.startIndex, original.endIndex)
        { }

        /************************************************************************************************************************/

        /// <summary>Assigns the specified <see cref="rawString"/>, sets the <see cref="startIndex"/> to 0, and the <see cref="endIndex"/> equal to rawString.Length.</summary>
        public void Set(string rawString)
        {
            this.rawString = rawString;
            startIndex = 0;
            endIndex = rawString.Length;
        }

        /// <summary>Assigns the specified <see cref="rawString"/>, sets the specified <see cref="startIndex"/>, and the <see cref="endIndex"/> equal to rawString.Length.</summary>
        public void Set(string rawString, int startIndex)
        {
            this.rawString = rawString;
            this.startIndex = startIndex;
            endIndex = rawString.Length;
        }

        /// <summary>Assigns the specified parameters to this <see cref="Substring"/>.</summary>
        public void Set(string rawString, int startIndex, int endIndex)
        {
            this.rawString = rawString;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The parameters denote a substring within the `fullString`.
        /// This method returns true if this <see cref="Substring"/> starts with the same characters contained in that substring.
        /// </summary>
        public bool StartsWith(string fullString, int startIndex, int endIndex)
        {
            if (Length < endIndex - startIndex || !IsValid || startIndex < 0 || endIndex > fullString.Length || startIndex > endIndex)
                return false;

            endIndex -= startIndex;
            for (int i = 0; i < endIndex; i++)
            {
                if (this[i] != fullString[startIndex + i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if this <see cref="Substring"/> starts with the characters in `other`.
        /// </summary>
        public bool StartsWith(Substring other)
        {
            return StartsWith(other.rawString, other.startIndex, other.endIndex);
        }

        /// <summary>
        /// Returns true if this <see cref="Substring"/> starts with the characters in `fullString`.
        /// </summary>
        public bool StartsWith(string fullString)
        {
            return StartsWith(fullString, 0, fullString.Length);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the first index of the specified `value` within this substring, or -1 if it isn't found.
        /// <para></para>
        /// The returned value is relative to the start of the <see cref="rawString"/>.
        /// </summary>
        public int IndexOf(char value, int startIndex)
        {
            if (!IsValid) return -1;

            return rawString.IndexOf(value, startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Returns the first index of any of the characters in `anyOf` within this substring, or -1 if none are found.
        /// <para></para>
        /// The returned value is relative to the start of the <see cref="rawString"/>.
        /// </summary>
        public int IndexOfAny(char[] anyOf, int startIndex)
        {
            if (!IsValid) return -1;

            for (int i = startIndex; i < endIndex; i++)
            {
                var c = rawString[i];
                for (int j = 0; j < anyOf.Length; j++)
                {
                    if (c == anyOf[j]) return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the last index of the specified `value` within this substring, or -1 if it isn't found.
        /// <para></para>
        /// The returned value is relative to the start of the <see cref="rawString"/>.
        /// </summary>
        public int LastIndexOf(char value)
        {
            if (!IsValid) return -1;

            return rawString.LastIndexOf(value, endIndex - 1, Length);
        }

        /// <summary>
        /// Returns the last index of any of the characters in `anyOf` within this substring, or -1 if none are found.
        /// <para></para>
        /// The returned value is relative to the start of the <see cref="rawString"/>.
        /// </summary>
        public int LastIndexOfAny(char[] anyOf)
        {
            if (!IsValid) return -1;

            for (int i = endIndex - 1; i >= startIndex; i--)
            {
                var c = rawString[i];
                for (int j = 0; j < anyOf.Length; j++)
                {
                    if (c == anyOf[j]) return i;
                }
            }

            return -1;
        }

        /************************************************************************************************************************/

        /// <summary>Copies the <see cref="rawString"/>, <see cref="startIndex"/>, and <see cref="endIndex"/> from `other`.</summary>
        public void CopyFrom(Substring other)
        {
            rawString = other.rawString;
            startIndex = other.startIndex;
            endIndex = other.endIndex;
        }

        /************************************************************************************************************************/
        #region Equality
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified substring within `a` contains the same characters as the specified substring within `b`.
        /// </summary>
        public static bool Equals(string a, int aStartIndex, int aEndIndex, string b, int bStartIndex, int bEndIndex)
        {
            if (aStartIndex < 0 || bStartIndex < 0 ||
                aEndIndex > a.Length || bEndIndex > b.Length ||
                aStartIndex > aEndIndex || bStartIndex > bEndIndex ||
                aEndIndex - aStartIndex != bEndIndex - bStartIndex)
                return false;

            aEndIndex -= aStartIndex;
            for (int i = 0; i < aEndIndex; i++)
            {
                if (a[aStartIndex + i] != b[bStartIndex + i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the specified substring within `a` contains the same characters as the specified substring within `b`.
        /// </summary>
        public static bool Equals(string a, string b, int bStartIndex, int bEndIndex)
        {
            return Equals(a, 0, a.Length, b, bStartIndex, bEndIndex);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if `a` and `b` contain the same characters.
        /// </summary>
        public static bool operator ==(Substring a, Substring b)
        {
            if (a is null)
                return b is null;
            else if (b is null)
                return false;

            return Equals(a.rawString, a.startIndex, a.endIndex, b.rawString, b.startIndex, b.endIndex);
        }

        /// <summary>
        /// Returns true if `a` and `b` contain different characters.
        /// </summary>
        public static bool operator !=(Substring a, Substring b)
        {
            return !(a == b);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if `a` and `b` contain the same characters.
        /// </summary>
        public static bool operator ==(Substring a, string b)
        {
            if (a is null)
                return b == null;
            else if (b == null)
                return false;

            return Equals(a.rawString, a.startIndex, a.endIndex, b, 0, b.Length);
        }

        /// <summary>
        /// Returns true if `a` and `b` contain different characters.
        /// </summary>
        public static bool operator !=(Substring a, string b)
        {
            return !(a == b);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if `this` contains the same characters as `obj` (as a string or <see cref="Substring"/>).
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (obj is string str) return this == str;
            else if (obj is Substring sub) return this == sub;
            else return false;
        }

        /// <summary>
        /// Returns the hash code of the current value of this <see cref="Substring"/>.
        /// </summary>
        public override int GetHashCode()
        {
            if (IsValid)
                return ToString().GetHashCode();
            else
                return 0;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Compares the characters in `this` to `other`.
        /// </summary>
        public int CompareTo(Substring other)
        {
            return string.Compare(rawString, startIndex, other.rawString, other.startIndex, Length);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region ToString
        /************************************************************************************************************************/

        /// <summary>
        /// Returns a new string containing the current value of this <see cref="Substring"/>.
        /// </summary>
        public static implicit operator string(Substring substring)
        {
            if (substring != null)
                return substring.ToString();
            else
                return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a new string containing the current value of this <see cref="Substring"/>.
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
                return rawString.Substring(startIndex, Length);
            else
                return "";
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a description of the current state of this <see cref="Substring"/>.
        /// </summary>
        public string ToDetailedString()
        {
            return rawString + "[" + startIndex + "->" + endIndex + "->" + ToString() + "]";
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the characters of this <see cref="Substring"/> to the specified `text`.
        /// <para></para>
        /// If this <see cref="Substring"/> is currently invalid, it appends '\0' (the NUL char).
        /// </summary>
        public void AppendTo(StringBuilder text)
        {
            if (IsValid)
            {
                if (text.Capacity < text.Length + Length)
                    text.Capacity = text.Length + Length;

                text.Append(rawString, startIndex, endIndex - startIndex);
            }
            else
            {
                text.Append('\0');
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region File Paths
        /************************************************************************************************************************/

        /// <summary>
        /// Returns a new <see cref="Substring"/> encapsulating the file name within the specified `path`, without its file extension.
        /// </summary>
        public static Substring GetFileNameWithoutExtension(string path)
        {
            var name = new Substring(path, path.LastIndexOf('/') + 1, 0);

            name.endIndex = path.LastIndexOf('.', path.Length - 1, path.Length - name.startIndex);
            if (name.endIndex < 0)
                name.endIndex = path.Length;

            return name;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adjusts the <see cref="endIndex"/> to remove the file extension from the end of this <see cref="Substring"/> (if it has one).
        /// </summary>
        public void RemoveFileExtension()
        {
            var lastSlash = LastIndexOf('/') + 1;
            var lastDot = rawString.LastIndexOf('.', endIndex - 1, endIndex - 1 - lastSlash);
            if (lastDot >= 0)
                endIndex = lastDot;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adjusts the <see cref="startIndex"/> and <see cref="endIndex"/> to encapsulate the name of the next directory within the <see cref="rawString"/>.
        /// </summary>
        public bool MoveToNextDirectory()
        {
            startIndex = endIndex + 1;
            if (startIndex >= rawString.Length) return false;

            endIndex = rawString.IndexOf('/', startIndex);
            if (endIndex < 0) endIndex = rawString.Length;

            return true;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

