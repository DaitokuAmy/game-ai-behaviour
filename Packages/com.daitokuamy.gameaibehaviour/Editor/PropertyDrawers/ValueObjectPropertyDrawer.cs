using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameAiBehaviour.Editor {
    /// <summary>
    /// ValueObjectのエディタ拡張
    /// </summary>
    [CustomPropertyDrawer(typeof(ValueObject<>), true)]
    public class ValueObjectPropertyDrawer : PropertyDrawer {
        private SerializedProperty _usePropertyProp;
        private SerializedProperty _constValueProp;
        private SerializedProperty _propertyNameProp;
        private Property.Type[] _propertyTypeFilters;

        /// <summary>
        /// GUI拡張
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            Initialize(property);

            var height = EditorGUIUtility.singleLineHeight;
            var padding = EditorGUIUtility.standardVerticalSpacing;
            var r = position;
            r.height = height;
            r.y += padding;
            EditorGUI.LabelField(r, label);

            r.xMin += EditorGUIUtility.labelWidth + 2;
            var buttonLabel = _usePropertyProp.boolValue ? "P" : "C";
            var buttonRect = r;
            buttonRect.width = 24;
            _usePropertyProp.boolValue ^= GUI.Button(buttonRect, buttonLabel);
            r.xMin += buttonRect.width + 2;
            if (_usePropertyProp.boolValue) {
                _propertyNameProp.stringValue =
                    BehaviourTreeEditorGUI.PropertyNameField(r, _propertyNameProp.stringValue, _propertyTypeFilters);
            }
            else {
                EditorGUI.PropertyField(r, _constValueProp, GUIContent.none);
            }
        }

        /// <summary>
        /// GUIの高さ計算
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            Initialize(property);
            var height = 0.0f;
            var padding = EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + padding;
            return height;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Initialize(SerializedProperty property) {
            _usePropertyProp = property.FindPropertyRelative("useProperty");
            _constValueProp = property.FindPropertyRelative("constValue");
            _propertyNameProp = property.FindPropertyRelative("propertyName");

            var type = fieldInfo.FieldType.BaseType?.GetGenericArguments().FirstOrDefault() ?? typeof(int);
            if (type == typeof(int)) {
                _propertyTypeFilters = new[] { Property.Type.Integer };
            }
            else if (type == typeof(float)) {
                _propertyTypeFilters = new[] { Property.Type.Float };
            }
            else if (type == typeof(string)) {
                _propertyTypeFilters = new[] { Property.Type.String };
            }
            else if (type == typeof(bool)) {
                _propertyTypeFilters = new[] { Property.Type.Boolean };
            }
        }
    }
}