#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

namespace GameAiBehaviour {
    /// <summary>
    /// 値比較用の条件基底
    /// </summary>
    public abstract class ValueCondition<T, TValueObject> : Condition
        where TValueObject : ValueObject<T> {
        public TValueObject leftValue;
        public TValueObject rightValue;

        // GUI描画の際に使用するPropertyの使用可能型フィルタ
        protected abstract Property.Type[] PropertyTypeFilters { get; }

#if UNITY_EDITOR
        /// <summary>
        /// インスペクタ描画
        /// </summary>
        public override void OnInspectorGUI(Rect position, SerializedObject serializedObject) {
            var leftProp = serializedObject.FindProperty("leftValue");
            var rightProp = serializedObject.FindProperty("rightValue");
            var operatorProp = serializedObject.FindProperty("operatorType");
            var padding = EditorGUIUtility.standardVerticalSpacing;
            var operatorWidth = 30;
            var valueWidth = (position.width - operatorWidth - padding * 2) / 2 - padding * 2;
            var height = EditorGUIUtility.singleLineHeight;
            var rect = position;

            // プロパティ描画
            void DrawValueObject(Rect pos, SerializedProperty prop) {
                var useProperty = prop.FindPropertyRelative("useProperty");
                var constValue = prop.FindPropertyRelative("constValue");
                var propertyName = prop.FindPropertyRelative("propertyName");

                var r = rect;
                r.height = height;
                r.y += padding;
                var buttonLabel = useProperty.boolValue ? "Property Mode" : "Const Mode";
                useProperty.boolValue ^= GUI.Button(r, buttonLabel);
                r.y += height + padding * 2;
                if (useProperty.boolValue) {
                    propertyName.stringValue =
                        BehaviourTreeEditorGUI.PropertyNameField(r, propertyName.stringValue, PropertyTypeFilters);
                }
                else {
                    EditorGUI.PropertyField(r, constValue, GUIContent.none);
                }
            }

            // 左辺
            rect.width = valueWidth;
            rect.x += padding;
            DrawValueObject(rect, leftProp);

            // 演算子
            rect.width = operatorWidth;
            rect.x += valueWidth + padding * 2;
            {
                var r = rect;
                r.height = height;
                r.y += (height + padding * 2) * 0.5f + padding;
                var labels = GetOperatorTypeLabels();
                operatorProp.enumValueIndex =
                    EditorGUI.Popup(r, operatorProp.enumValueIndex, labels, EditorStyles.miniButton);
            }

            // 右辺
            rect.width = valueWidth;
            rect.x += operatorWidth + padding * 2;
            DrawValueObject(rect, rightProp);
        }

        /// <summary>
        /// インスペクタ描画時の高さ取得
        /// </summary>
        public override float GetInspectorGUIHeight(SerializedObject serializedObject) {
            var height = 0.0f;
            var padding = EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + padding * 2;
            height += EditorGUIUtility.singleLineHeight + padding * 2;
            return height;
        }
#endif

        /// <summary>
        /// 判定
        /// </summary>
        public sealed override bool Check(IBehaviourTreeController controller) {
            return CheckInternal(GetLeftValue(controller.Blackboard), GetRightValue(controller.Blackboard));
        }

        /// <summary>
        /// 値の判定
        /// </summary>
        protected abstract bool CheckInternal(T left, T right);

        /// <summary>
        /// プロパティの取得
        /// </summary>
        protected abstract T GetPropertyValue(Blackboard blackboard, string propertyName, T defaultValue);

        /// <summary>
        /// オペレータタイプの表示名
        /// </summary>
        protected abstract string[] GetOperatorTypeLabels();

        /// <summary>
        /// 左辺値の取得
        /// </summary>
        private T GetLeftValue(Blackboard blackboard) {
            return GetValue(blackboard, leftValue);
        }

        /// <summary>
        /// 右辺値の取得
        /// </summary>
        private T GetRightValue(Blackboard blackboard) {
            return GetValue(blackboard, rightValue);
        }

        /// <summary>
        /// 値の取得
        /// </summary>
        private T GetValue(Blackboard blackboard, TValueObject valueObject) {
            if (string.IsNullOrEmpty(valueObject.propertyName) || !valueObject.useProperty) {
                return valueObject.constValue;
            }

            return GetPropertyValue(blackboard, valueObject.propertyName, valueObject.constValue);
        }
    }
}