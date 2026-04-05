#if UNITY_EDITOR
using Infrastructure;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LevelSequence))]
public class LevelSequenceEditor : Editor
{
    private ReorderableList m_list;
    private SerializedProperty m_stepsProp;

    private const float ROW = 22f;
    private const float OBJ_ROW = 26f;
    private const float SPACING = 8f;
    private const float SECTION_GAP = 12f;
    private const float TOP_PAD = 10f;
    private const float BOTTOM_PAD = 14f;
    private const float INSET = 10f;

    private static readonly Color DialogColor = new Color(0.95f, 0.85f, 0.55f, 0.3f);
    private static readonly Color MiniGameColor = new Color(0.45f, 0.85f, 0.75f, 0.3f);
    private static readonly Color AnimationColor = new Color(0.85f, 0.55f, 0.45f, 0.3f);

    private void OnEnable()
    {
        m_stepsProp = serializedObject.FindProperty("m_steps");

        m_list = new ReorderableList(serializedObject, m_stepsProp, true, true, true, true)
        {
            drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Шаги уровня ({m_stepsProp.arraySize})", EditorStyles.boldLabel),
            drawElementCallback = DrawElement,
            elementHeightCallback = CalcHeight,
            drawElementBackgroundCallback = DrawBg
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space(8);
        m_list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBg(Rect rect, int index, bool active, bool focused)
    {
        if (index < 0 || index >= m_stepsProp.arraySize) return;
        var el = m_stepsProp.GetArrayElementAtIndex(index);
        var t = (LevelStepType)el.FindPropertyRelative("type").enumValueIndex;
        Color c = t switch
        {
            LevelStepType.Dialog => DialogColor,
            LevelStepType.MiniGame => MiniGameColor,
            LevelStepType.Animation => AnimationColor,
            _ => Color.clear
        };
        if (active) c.a += 0.15f;
        EditorGUI.DrawRect(rect, c);
    }

    private float CalcHeight(int index)
    {
        if (index < 0 || index >= m_stepsProp.arraySize) return ROW * 3;
        var el = m_stepsProp.GetArrayElementAtIndex(index);
        var t = (LevelStepType)el.FindPropertyRelative("type").enumValueIndex;

        float h = TOP_PAD;
        h += ROW + SPACING;           // header
        h += ROW + SPACING;           // label
        h += ROW + SPACING;           // type
        h += SECTION_GAP;

        switch (t)
        {
            case LevelStepType.Dialog:
                h += OBJ_ROW + SPACING;
                break;
            case LevelStepType.MiniGame:
                h += OBJ_ROW + SPACING;
                break;
            case LevelStepType.Animation:
                h += (OBJ_ROW + SPACING) * 4;
                break;
        }

        h += OBJ_ROW + SPACING;       // delayBeforeNext
        h += BOTTOM_PAD;
        return h;
    }

    private void DrawElement(Rect rect, int index, bool active, bool focused)
    {
        var el = m_stepsProp.GetArrayElementAtIndex(index);
        var labelProp = el.FindPropertyRelative("label");
        var typeProp = el.FindPropertyRelative("type");
        var t = (LevelStepType)typeProp.enumValueIndex;

        float x = rect.x + INSET;
        float w = rect.width - INSET * 2;
        float y = rect.y + TOP_PAD;

        string tag = t switch
        {
            LevelStepType.Dialog => "[Dialog]",
            LevelStepType.MiniGame => "[MiniGame]",
            LevelStepType.Animation => "[Animation]",
            _ => ""
        };

        EditorGUI.LabelField(Row(ref y, ROW), $"Step {index}  {tag}", EditorStyles.boldLabel);
        EditorGUI.PropertyField(Row(ref y, ROW), labelProp, new GUIContent("Название"));
        EditorGUI.PropertyField(Row(ref y, ROW), typeProp, new GUIContent("Тип шага"));

        y += SECTION_GAP;

        switch (t)
        {
            case LevelStepType.Dialog:
                EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
                    el.FindPropertyRelative("dialogObject"), new GUIContent("Объект диалога"));
                break;
            case LevelStepType.MiniGame:
                EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
                    el.FindPropertyRelative("miniGameObject"), new GUIContent("Объект мини-игры"));
                break;
            case LevelStepType.Animation:
                EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
                    el.FindPropertyRelative("characterAnimator"), new GUIContent("Animator"));
                EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
                    el.FindPropertyRelative("animationTrigger"), new GUIContent("Триггер"));
                EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
                    el.FindPropertyRelative("idleBoolParam"), new GUIContent("Idle параметр"));
                EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
                    el.FindPropertyRelative("animationDuration"), new GUIContent("Длительность"));
                break;
        }

        // Задержка перед следующим шагом (для всех типов)
        y += SECTION_GAP;
        EditorGUI.PropertyField(Row(ref y, OBJ_ROW),
            el.FindPropertyRelative("delayBeforeNext"), new GUIContent("Задержка (сек)"));

        Rect Row(ref float cy, float h)
        {
            var r = new Rect(x, cy, w, h);
            cy += h + SPACING;
            return r;
        }
    }
}
#endif