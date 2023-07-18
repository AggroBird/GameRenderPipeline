using UnityEngine.Rendering;

namespace AggroBird.GameRenderPipeline.Editor
{
    public abstract class EditorGizmoEffect : PostProcessStack.IEditorGizmoEffect
    {
        public virtual bool Enabled => true;

        public virtual int Priority => 0;

        internal const string DefaultEffectName = "CustomEditorGizmoEffect";
        public virtual string EffectName => DefaultEffectName;

        public abstract void Execute(CommandBuffer buffer, RenderTargetIdentifier srcColor, RenderTargetIdentifier srcDepth, RenderTargetIdentifier dstColor);
    }

    public static class EditorGizmoEffects
    {
        public static void AddEditorGizmoEffect(EditorGizmoEffect effect)
        {
            if (effect != null && !PostProcessStack.editorGizmoEffects.Contains(effect))
            {
                PostProcessStack.editorGizmoEffects.Add(effect);
            }
        }
        public static void RemoveEditorGizmoEffect(EditorGizmoEffect effect)
        {
            if (effect != null)
            {
                PostProcessStack.editorGizmoEffects.Remove(effect);
            }
        }
    }
}