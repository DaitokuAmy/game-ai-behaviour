#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

namespace GameAiBehaviour {
    /// <summary>
    /// ハンドリング可能な条件
    /// </summary>
    public abstract class HandleableCondition : Condition {
#if UNITY_EDITOR
        /// <summary>
        /// インスペクタ描画
        /// </summary>
        public override void OnInspectorGUI(Rect position, SerializedObject serializedObject) {
            var rect = position;
            
            var iter = serializedObject.GetIterator();
            iter.NextVisible(true);
            while (iter.NextVisible(false)) {
                if (iter.name == "_property1") {
                    continue;
                }

                var height = EditorGUI.GetPropertyHeight(iter, true);
                rect.height = height;
                EditorGUI.PropertyField(rect, iter, true);
                rect.y += height;
            }
        }

        /// <summary>
        /// インスペクタ描画時の高さ取得
        /// </summary>
        public override float GetInspectorGUIHeight(SerializedObject serializedObject) {
            var height = 0.0f;
            var iter = serializedObject.GetIterator();
            iter.NextVisible(true);
            while (iter.NextVisible(false)) {
                if (iter.name == "_property1") {
                    continue;
                }

                height += EditorGUI.GetPropertyHeight(iter, true);
            }

            return height;
        }
#endif

        /// <summary>
        /// 判定
        /// </summary>
        public sealed override bool Check(IBehaviourTreeController controller) {
            var handler = controller.GetConditionHandler(this);

            // Handlerが無ければ、判定失敗
            if (handler == null) {
                return false;
            }

            // Handlerによる判定
            return handler.Check(this, controller.Blackboard);
        }
    }
}