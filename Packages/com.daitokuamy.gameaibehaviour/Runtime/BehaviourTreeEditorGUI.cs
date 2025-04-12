#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTreeのエディタGUI補助クラス
    /// </summary>
    public static class BehaviourTreeEditorGUI {
        // 現在描画中のツリー
        public static BehaviourTree CurrentTree { get; set; }

        /// <summary>
        /// 特定ノードの参照を設定するためのフィールド
        /// </summary>
        public static T NodeField<T>(Rect rect, GUIContent label, T current)
            where T : Node {
            // Treeがない場合は非アクティブなObjectField
            if (CurrentTree == null) {
                using (new EditorGUI.DisabledScope()) {
                    return EditorGUI.ObjectField(rect, label, null, typeof(T), false) as T;
                }
            }

            // Treeがある場合は、Treeに含まれている該当Nodeの参照を使ったPopup
            var targetNodes = CurrentTree.nodes
                .OfType<T>()
                .ToList();
            var targetNodeLabels = new[] { "None" }
                .Concat(targetNodes.Select(BehaviourTreeEditorUtility.GetNodeDisplayTitle))
                .ToArray();
            var currentIndex = targetNodes.IndexOf(current) + 1;
            currentIndex = EditorGUI.Popup(rect, label.text, currentIndex, targetNodeLabels);

            if (currentIndex - 1 >= 0) {
                return targetNodes[currentIndex - 1];
            }

            return null;
        }

        /// <summary>
        /// プロパティ名のGUI描画
        /// </summary>
        public static string PropertyNameField(Rect rect, string propertyName, Property.Type[] typeFilters, bool ignoreConstProperties = false) {
            // Treeがない場合はただのTextField
            if (CurrentTree == null) {
                return EditorGUI.TextField(rect, propertyName);
            }

            // Treeがある場合は、BlackboardのPropertyを使ったPopup
            var propertyNames = Array.Empty<string>();
            if (CurrentTree.blackboardAsset != null) {
                propertyNames = CurrentTree.blackboardAsset.properties
                    .Concat(ignoreConstProperties ? Array.Empty<Property>() : CurrentTree.blackboardAsset.constProperties)
                    .Where(x => typeFilters?.Contains(x.propertyType) ?? true)
                    .Select(x => x.propertyName)
                    .ToArray();
            }

            if (propertyNames.Length <= 0) {
                propertyNames = new[] { "Empty" };
            }

            var index = Mathf.Max(0, propertyNames.ToList().IndexOf(propertyName));
            index = EditorGUI.Popup(rect, index, propertyNames);
            return propertyNames[index];
        }
    }
}
#endif