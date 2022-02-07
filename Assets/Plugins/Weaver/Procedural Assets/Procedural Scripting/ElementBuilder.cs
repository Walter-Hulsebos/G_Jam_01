// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

//#define LOG_NAMING_CONFLICT_DETAILS

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only] Base class for building any element in a procedural script such as namespaces, types, fields, etc.</summary>
    public abstract class ElementBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="Scripting.ScriptBuilder"/> in which this element is currently being used.
        /// </summary>
        public ScriptBuilder ScriptBuilder { get; internal set; }

        /// <summary>The builder of the type in which this element will be declared.</summary>
        public IElementBuilderGroup Parent { get; private set; }

        /// <summary>The source string which will be used to determine the actual <see cref="Name"/> of this element.</summary>
        public string NameSource { get; private set; }

        /// <summary>
        /// The actual <see cref="Name"/> of this element.
        /// This value is derived from <see cref="NameSource"/> during <see cref="PrepareToBuild(bool, ref bool)"/>.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Indicates whether the current <see cref="Name"/> of this element is the primary name derived from the
        /// <see cref="NameSource"/>. A value of false means that there was a name conflict between this element and
        /// another, and the <see cref="Name"/> was re-derived using <see cref="ScriptBuilder.GetFallbackMemberName(string, string)"/>.
        /// </summary>
        public bool IsFallbackName { get; private set; }

        /// <summary>
        /// The index in <see cref="ScriptBuilder.CompilationSymbols"/> of the symbol in which this element will be declared, I.E. #if SYMBOL.
        /// </summary>
        public int CompilationSymbolIndex { get; set; } = -1;

        /// <summary>
        /// The index in <see cref="ScriptBuilder.Regions"/> of the region in which this element will be declared, I.E. #region Region Name.
        /// </summary>
        public int RegionIndex { get; set; } = -1;

        /************************************************************************************************************************/

        /// <summary>
        /// This delegate is used to append the XML comment for this element. By default it will simply append the <see cref="NameSource"/>.
        /// </summary>
        public Action<StringBuilder> CommentBuilder { get; set; }

        /// <summary>
        /// The default delegate to use to build the XML comment for this element.
        /// Assigned using the return value of <see cref="GetDefaultCommentBuilder"/>.
        /// </summary>
        public readonly Action<StringBuilder> DefaultCommentBuilder;

        /// <summary>
        /// Returns the default method to use to build XML comments for this element. Called once by the constructor.
        /// </summary>
        protected virtual Action<StringBuilder> GetDefaultCommentBuilder()
        {
            return (comment) => comment.Append(NameSource);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The type of member which this element builds.
        /// </summary>
        public abstract MemberTypes MemberType { get; }

        /************************************************************************************************************************/

        /// <summary>The name of the <see cref="NamespaceBuilder"/> containing this type (or null if there isn't one).</summary>
        public virtual string Namespace => Parent?.Namespace;

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the full name of this element, including its <see cref="Parent"/> (and any types and namespaces it
        /// is nested inside).
        /// </summary>
        public string FullName
        {
            get
            {
                var text = WeaverUtilities.GetStringBuilder();
                AppendFullName(text);
                return text.ReleaseToString();
            }
        }

        /// <summary>
        /// Appends the full name of this element, including its <see cref="Parent"/> (and any types and namespaces it
        /// is nested inside).
        /// </summary>
        public void AppendFullName(StringBuilder text)
        {
            if (Parent != null)
            {
                Parent.AppendFullName(text);
                text.Append('.');
            }

            text.Append(Name);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if this element is associated with an existing <see cref="MemberInfo"/>.
        /// </summary>
        public virtual bool HasExistingMember => false;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialisation and Pooling
        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="MemberBuilder"/> with the default values.
        /// <para></para>
        /// Consider using one of the overloads of Get instead, in order to utilise object pooling to minimise memory
        /// allocation and garbage collection.
        /// </summary>
        protected ElementBuilder()
        {
            CommentBuilder = DefaultCommentBuilder = GetDefaultCommentBuilder();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Assigns the specified parameters to this element and determines the <see cref="Name"/>.
        /// </summary>
        public void Initialize(IElementBuilderGroup parent, string nameSource)
        {
            ScriptBuilder = parent.ScriptBuilder;
            Parent = parent;
            NameSource = nameSource;
            DetermineMemberName(parent.ScriptBuilder);
        }

        /************************************************************************************************************************/

        internal void InitializeRootType(ScriptBuilder scriptBuilder, string nameSource)
        {
            ScriptBuilder = scriptBuilder;
            NameSource = nameSource;
            DetermineMemberName(scriptBuilder);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Uses the specified <see cref="ScriptBuilder.GetMemberName(string, string, out bool)"/> to determine the <see cref="Name"/> of this element.
        /// </summary>
        /// <param name="scriptBuilder"></param>
        protected void DetermineMemberName(ScriptBuilder scriptBuilder)
        {
            Name = scriptBuilder.GetMemberName(NameSource, Parent?.Name, out var isFallbackName);
            IsFallbackName = isFallbackName;
        }

        /************************************************************************************************************************/

        /// <summary>Checks if the `existingMember` corresponds to this element.</summary>
        public abstract bool IsExistingMember(MemberInfo existingMember, ref bool shouldRebuild);

        /************************************************************************************************************************/

        /// <summary>Resets all of the fields and properties of this element to their default values.</summary>
        protected virtual void Reset()
        {
            Parent = null;
            CompilationSymbolIndex = -1;
            RegionIndex = -1;
            CommentBuilder = DefaultCommentBuilder;
        }

        /// <summary>Resets this element and adds it to its object pool to be reused later.</summary>
        public abstract void ReleaseToPool();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Building
        /************************************************************************************************************************/

        internal void ResolveNamingConflicts(Dictionary<string, ElementBuilder> nameToElement)
        {
            if (nameToElement.TryGetValue(Name, out var otherElement))
            {
#if LOG_NAMING_CONFLICT_DETAILS
                Debug.Log("Naming Conflict Detected: " + Name + "\n" + this + "\n" + otherMember);
#endif

                if (otherElement != null)
                {
                    if (!otherElement.IsFallbackName)
                    {
                        nameToElement[Name] = null;
                        otherElement.ApplyFallbackName(nameToElement);
                    }

                    if (!IsFallbackName)
                        ApplyFallbackName(nameToElement);

                    if (Name == otherElement.Name)
                    {
                        RegisterNameConflict("Unable to resolve member name conflict:", this, otherElement);
                    }
                }
                else
                {
                    if (!IsFallbackName)
                        ApplyFallbackName(nameToElement);

                    if (nameToElement.TryGetValue(Name, out otherElement))
                    {
                        RegisterNameConflict("Unable to resolve member name conflict:", this, otherElement);
                    }
                }

#if LOG_NAMING_CONFLICT_DETAILS
                Debug.Log("Naming Conflict Resolved: " + Name + "\n" + this + "\n" + otherMember);
#endif
            }
            else
            {
                nameToElement.Add(Name, this);
            }
        }

        /************************************************************************************************************************/

        private void ApplyFallbackName(Dictionary<string, ElementBuilder> nameToElement)
        {
            var previousName = Name;
            Name = Parent.ScriptBuilder.GetFallbackMemberName(NameSource, Parent.Name);
            IsFallbackName = true;

            if (nameToElement.TryGetValue(Name, out var otherElement))
            {
                if (Name != previousName)
                {
                    RegisterNameConflict("Fallback name created another member name conflict:", this, otherElement);
                }
            }
            else nameToElement.Add(Name, this);
        }

        /************************************************************************************************************************/

        private static void RegisterNameConflict(string message, ElementBuilder a, ElementBuilder b)
        {
            ScriptBuilder.BuildErrors.AppendLineConst(message)
                .Indent(1).AppendLineConst(a)
                .Indent(1).AppendLineConst(b);
        }

        /************************************************************************************************************************/

        internal abstract void PrepareToBuild(bool retainObsoleteMembers, ref bool shouldRebuild);

        /************************************************************************************************************************/

        /// <summary>Appends the declaration of this element in C# code to the specified `text`.</summary>
        public abstract void AppendScript(StringBuilder text, int indent);

        /************************************************************************************************************************/

        /// <summary>Appends a C# XML comment using the <see cref="CommentBuilder"/>.</summary>
        protected virtual void AppendHeader(StringBuilder text, int indent)
        {
            if (CommentBuilder != null)
            {
                text.Indent(indent);
                text.Append("/// <summary>");

                var start = text.Length;
                CommentBuilder(text);

                var hasPassedVisibleCharacter = false;
                var isMultiLine = false;

                var i = start;
                for (; i < text.Length; i++)
                {
                    var c = text[i];

                    if (c != '\n')
                    {
                        if (!hasPassedVisibleCharacter && !char.IsWhiteSpace(c))
                            hasPassedVisibleCharacter = true;

                        continue;
                    }

                    // If the first new line has visible characters before it, add an extra new line back at the start.
                    if (!isMultiLine && hasPassedVisibleCharacter)
                    {
                        var index = start;
                        InsertAt(text, ref index, WeaverUtilities.NewLine);
                        InsertIndentedComment(text, indent, ref index);
                        i += index - start;
                    }

                    isMultiLine = true;

                    i++;
                    InsertIndentedComment(text, indent, ref i);
                    i--;
                }

                if (isMultiLine)
                {
                    if (text[text.Length - 1] != '\n')
                        text.AppendLineConst();

                    text.Indent(indent).Append("/// ");
                }

                text.AppendLineConst("</summary>");
            }
        }

        /************************************************************************************************************************/

        private static void InsertIndentedComment(StringBuilder text, int indent, ref int insertAt)
        {
            for (int i = 0; i < indent; i++)
            {
                InsertAt(text, ref insertAt, WeaverUtilities.Tab);
            }

            InsertAt(text, ref insertAt, "/// ");
        }

        private static void InsertAt(StringBuilder text, ref int insertAt, string str)
        {
            text.Insert(insertAt, str);
            insertAt += str.Length;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Misc
        /************************************************************************************************************************/

        /// <summary>Sets the <see cref="Name"/> and <see cref="NameSource"/>.</summary>
        public void SetName(string name)
        {
            Name = NameSource = name;
            IsFallbackName = false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a summary of this element including its type, <see cref="NameSource"/>, <see cref="Name"/>, and <see cref="FullName"/>.
        /// </summary>
        public override string ToString()
        {
            var text = WeaverUtilities.GetStringBuilder();
            text.Append(GetType().GetNameCS(CSharp.NameVerbosity.Basic));
            text.Append(" -> " + nameof(NameSource) + "=");
            text.Append(NameSource);
            text.Append(", " + nameof(Name) + "=");
            text.Append(Name);
            text.Append(", " + nameof(FullName) + "=");
            AppendFullName(text);
            return text.ReleaseToString();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets a description of this element by calling <see cref="ToString"/> on it and any sub-members.
        /// </summary>
        public string GetDescription()
        {
            var text = WeaverUtilities.GetStringBuilder();
            AppendDescription(text, 0);
            return text.ReleaseToString();
        }

        /// <summary>
        /// Appends a description of this element by calling <see cref="ToString"/> on it.
        /// </summary>
        public virtual void AppendDescription(StringBuilder text, int indent)
        {
            text.Indent(indent).AppendLineConst(this);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

