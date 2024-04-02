using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline.Editor
{
    public abstract class EditorGizmoEffect : PostProcessStack.IEditorGizmoEffect
    {
        public static void Register(EditorGizmoEffect effect)
        {
            if (effect != null && !PostProcessStack.editorGizmoEffects.Contains(effect))
            {
                PostProcessStack.editorGizmoEffects.Add(effect);
            }
        }
        public static void Unregister(EditorGizmoEffect effect)
        {
            if (effect != null)
            {
                PostProcessStack.editorGizmoEffects.Remove(effect);
            }
        }


        public virtual bool Enabled => true;

        public virtual int Priority => 0;

        internal const string DefaultEffectName = "CustomEditorGizmoEffect";
        public virtual string EffectName => DefaultEffectName;

        public abstract void Execute(CommandBuffer buffer, RenderTargetIdentifier color, RenderTargetIdentifier depth, GraphicsFormat colorFormat);
    }
}