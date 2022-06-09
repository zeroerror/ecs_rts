namespace ZeroHero.UI
{
    using UnityEditor;
    using UnityEditor.UI;
    using UnityEngine.UI;

    [CustomEditor(typeof(BloodImage), true)]
    [CanEditMultipleObjects]
    public class BloodImageEditor : RawImageEditor
    {
        SerializedProperty _fadeImage;
        SerializedProperty _fadeTime;
        protected override void OnEnable()
        {
            _fadeImage = serializedObject.FindProperty("_fadeImage");
            _fadeTime = serializedObject.FindProperty("_fadeTime");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_fadeImage);
            EditorGUILayout.PropertyField(_fadeTime);
            base.OnInspectorGUI();
        }

        public override bool HasPreviewGUI()
        {
            return base.HasPreviewGUI();
        }
    }
}

