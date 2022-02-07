// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using System.Text;
using UnityEngine;

namespace Weaver.Editor.Procedural.Scripting
{
    /// <summary>[Editor-Only]
    /// Determines the naming conventions for a procedural C# script, as well as for #region names, #if symbols, and
    /// the messages used for obsolete members.
    /// </summary>
    public class ScriptBuilder : NamespaceBuilder
    {
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ScriptBuilder"/>.</summary>
        public ScriptBuilder()
        {
            ScriptBuilder = this;
        }

        /************************************************************************************************************************/

        /// <summary>Logs the `reason` that this script should be rebuilt.</summary>
        public virtual void LogRebuildReason(string reason)
        {
            Debug.Log("Rebuilding script because " + reason);
        }

        /************************************************************************************************************************/
        #region Member Naming
        /************************************************************************************************************************/

        /// <summary>
        /// Converts the `nameSource` into a valid member name using <see cref="GetPrimaryMemberName"/>. If the name
        /// is the same as the `declaringTypeName`, <see cref="GetFallbackMemberName"/> will be used instead.
        /// </summary>
        public string GetMemberName(string nameSource, string declaringTypeName, out bool isFallback)
        {
            var memberName = GetPrimaryMemberName(nameSource);

            if (memberName == declaringTypeName)
            {
                memberName = GetFallbackMemberName(nameSource, declaringTypeName);
                isFallback = true;
            }
            else isFallback = false;

            return memberName;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Converts the `nameSource` into a valid member name according to the desired naming convention.
        /// <para></para>
        /// By default, this method uses <see cref="CSharpProcedural.ValidateMemberName(string, bool)"/>.
        /// </summary>
        public virtual string GetPrimaryMemberName(string nameSource)
        {
            return CSharpProcedural.ValidateMemberName(nameSource);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Converts the `nameSource` into a valid member name according to the desired naming convention when the
        /// primary name returned by <see cref="GetPrimaryMemberName"/> caused a naming conflict.
        /// <para></para>
        /// By default, this method uses <see cref="CSharpProcedural.ValidateMemberName(string, bool)"/> with the
        /// `replaceWithUnderscores` parameter set to false.
        /// </summary>
        public virtual string GetFallbackMemberName(string nameSource, string declaringTypeName)
        {
            var fallbackName = CSharpProcedural.ValidateMemberName(nameSource, true);

            if (fallbackName == declaringTypeName)
                fallbackName = '_' + fallbackName;

            return fallbackName;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Obsolete Members
        /************************************************************************************************************************/

        /// <summary>
        /// The message given to the [<see cref="System.ObsoleteAttribute"/>] constructor to be displayed whenever an
        /// obsolete member is referenced.
        /// </summary>
        public virtual string ObsoleteAttributeMessage => "Remove any references to it.";

        /************************************************************************************************************************/

        /// <summary>
        /// If true, obsolete members will be contained in a #if UNITY_EDITOR region to ensure that the user removes
        /// all references to them prior to compiling a build (because otherwise they would get compile errors). By
        /// default this property returns true.
        /// </summary>
        public virtual bool ObsoleteMembersAreEditorOnly => true;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Compilation Symbols and Regions
        /************************************************************************************************************************/

        /// <summary>
        /// The conditional compilation symbols used in #if regions in the procedural script.
        /// The indices of these values are referenced by <see cref="ElementBuilder.CompilationSymbolIndex"/>.
        /// </summary>
        public string[] CompilationSymbols { get; protected set; }

        /// <summary>
        /// The names used in #regions in the procedural script.
        /// The indices of these values are referenced by <see cref="ElementBuilder.RegionIndex"/>.
        /// </summary>
        public string[] Regions { get; protected set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the number of <see cref="CompilationSymbols"/> .
        /// </summary>
        public int GetCompilationSymbolCount()
        {
            if (CompilationSymbols == null)
                return 0;
            else
                return CompilationSymbols.Length;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the number of <see cref="Regions"/> .
        /// </summary>
        public int GetRegionCount()
        {
            if (Regions == null)
                return 0;
            else
                return Regions.Length;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Build Errors
        /************************************************************************************************************************/

        internal static readonly StringBuilder BuildErrors = new StringBuilder();

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if any errors occurred during the last call to <see cref="ElementBuilder.PrepareToBuild"/>.
        /// </summary>
        public static bool HasBuildErrors => BuildErrors.Length > 0;

        /************************************************************************************************************************/

        /// <summary>
        /// Determines the <see cref="ElementBuilder.Name"/> of this type's members, attempts to resolve any naming
        /// conflicts, matches existing <see cref="System.Reflection.MemberInfo"/>s with their appropriate members, and
        /// returns true if the script should be rebuilt for any reason (such as a member being added, removed, or renamed).
        /// </summary>
        public bool PrepareToBuild(bool retainObsoleteMembers, bool logBuildErrors)
        {
            BuildErrors.Length = 0;

            var shouldRebuild = false;

            PrepareToBuild(retainObsoleteMembers, ref shouldRebuild);

            if (BuildErrors.Length > 0)
            {
                if (logBuildErrors)
                {
                    var errorMessage = WeaverUtilities.GetStringBuilder();
                    errorMessage.Append("The following errors occurred while building the type: ");
                    AppendFullName(errorMessage);
                    errorMessage.AppendLineConst();
                    errorMessage.Append(BuildErrors);
                    Debug.LogError(errorMessage.ReleaseToString());
                }

                return false;
            }

            return shouldRebuild;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

