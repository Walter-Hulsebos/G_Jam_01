// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

#if UNITY_EDITOR

using Weaver.Editor.Procedural.Scripting;
using System.Text;
using System;

namespace Weaver.Editor.Procedural
{
    /// <summary>[Editor-Only, Internal]
    /// Procedurally generates a script containing constants corresponding to the properties and symbols in a set of
    /// shaders chosen in the <see cref="Window.TagsPanel"/>.
    /// </summary>
    public sealed class TagsScriptBuilder : SimpleScriptBuilder
    {
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TagsScriptBuilder"/>.</summary>
        public TagsScriptBuilder(Action<StringBuilder> generatorMethod) : base(generatorMethod) { }

        /************************************************************************************************************************/

        /// <summary>Indicates whether this script should be generated.</summary>
        public override bool Enabled => WeaverSettings.Tags.enabled;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void GatherScriptDetails()
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;

            for (int i = 0; i < tags.Length; i++)
            {
                var tag = tags[i];
                var field = RootType.AddField(tag, tag);
                field.Modifiers = AccessModifiers.Public | AccessModifiers.Const;
                field.CommentBuilder = null;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

