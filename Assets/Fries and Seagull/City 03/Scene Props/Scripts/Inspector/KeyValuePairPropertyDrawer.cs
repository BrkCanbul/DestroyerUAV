

namespace Seagull.City_03.Inspector {
# if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(KiiValuePair), true)]
    public class KeyValuePairPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // 使用反射获取键和值的类型和名称
            var keyProp = property.FindPropertyRelative("key");
            var valueProp = property.FindPropertyRelative("value");
            
            // 开始属性绘制
            EditorGUI.BeginProperty(position, label, property);

            // 去除缩进
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // 计算每个字段的区域
            float labelWidth = EditorGUIUtility.labelWidth;
            float fieldWidth = (position.width - labelWidth) / 2 - 5;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect stringRect = new Rect(position.x + labelWidth, position.y, fieldWidth, position.height);
            Rect glowLightRect = new Rect(position.x + labelWidth + fieldWidth + 10, position.y, fieldWidth, position.height);

            // 绘制标签
            EditorGUI.LabelField(labelRect, label);

            // 绘制字段
            EditorGUI.PropertyField(stringRect, keyProp, GUIContent.none);
            EditorGUI.PropertyField(glowLightRect, valueProp, GUIContent.none);

            // 恢复缩进
            EditorGUI.indentLevel = indent;

            // 结束属性绘制
            EditorGUI.EndProperty();
        }
    }
# endif
}