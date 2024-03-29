﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTreeのエディタ用ユーティリティ
    /// </summary>
    public static class BehaviourTreeEditorUtility {
        /// <summary>
        /// ノードの表示名を取得
        /// </summary>
        public static string GetNodeDisplayTitle(Node node) {
            if (node == null) {
                return "Null";
            }

            if (string.IsNullOrEmpty(node.title)) {
                var type = node.GetType();
                var attr =
                    type.GetCustomAttributes(typeof(BehaviourTreeNodeAttribute), false).FirstOrDefault() as
                        BehaviourTreeNodeAttribute;
                var name = ObjectNames.NicifyVariableName(type.Name);
                if (attr != null) {
                    var displayName = attr.DisplayName;
                    if (!string.IsNullOrEmpty(displayName)) {
                        name = displayName;
                    }
                }

                return name;
            }

            return node.title;
        }

        /// <summary>
        /// ノードの生成
        /// </summary>
        public static Node CreateNode(BehaviourTree tree, Type type) {
            if (tree == null) {
                return null;
            }

            var serializedObj = new SerializedObject(tree);
            var nodes = serializedObj.FindProperty("nodes");

            // アセットの生成
            var node = ScriptableObject.CreateInstance(type) as Node;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            Undo.RegisterCreatedObjectUndo(node, "CreateNode");
            AssetDatabase.AddObjectToAsset(node, tree);

            // Treeへの追加
            serializedObj.Update();
            nodes.arraySize++;
            var element = nodes.GetArrayElementAtIndex(nodes.arraySize - 1);
            element.objectReferenceValue = node;
            serializedObj.ApplyModifiedProperties();

            EditorUtility.SetDirty(tree);
            return node;
        }

        /// <summary>
        /// ノードの削除
        /// </summary>
        public static void DeleteNode(BehaviourTree tree, Node node) {
            if (tree == null || node == null) {
                return;
            }

            var serializedObj = new SerializedObject(tree);
            var nodesProp = serializedObj.FindProperty("nodes");

            // 接続情報の切断
            for (var i = 0; i < nodesProp.arraySize; i++) {
                var element = nodesProp.GetArrayElementAtIndex(i);
                var parent = element.objectReferenceValue as Node;
                if (parent == node) {
                    continue;
                }

                DisconnectNode(parent, node);
            }

            // リストから除外
            serializedObj.Update();

            for (var i = nodesProp.arraySize - 1; i >= 0; i--) {
                var element = nodesProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != node) {
                    continue;
                }

                element.objectReferenceValue = null;
                nodesProp.DeleteArrayElementAtIndex(i);
            }

            serializedObj.ApplyModifiedProperties();

            // 依存条件の削除
            DeleteConditions(node);

            // アセットの削除
            Undo.DestroyObjectImmediate(node);

            EditorUtility.SetDirty(tree);
        }

        /// <summary>
        /// ノードの複製
        /// </summary>
        public static Node[] DuplicateNodes(BehaviourTree tree, Node[] baseNodes) {
            if (tree == null || baseNodes == null) {
                return Array.Empty<Node>();
            }

            // 全Nodeを複製
            var dict = new Dictionary<Node, Node>();
            foreach (var baseNode in baseNodes) {
                var node = DuplicateNode(tree, baseNode);
                if (node == null) {
                    continue;
                }

                dict[baseNode] = node;
            }

            // 参照先をつなぎなおし（参照先がないものはクリア）
            foreach (var node in dict.Values) {
                var nodeObj = new SerializedObject(node);
                var childProp = nodeObj.FindProperty("child");
                var childrenProp = nodeObj.FindProperty("children");

                nodeObj.Update();

                if (childProp != null) {
                    var key = childProp.objectReferenceValue as Node;
                    if (key != null && dict.TryGetValue(key, out var res)) {
                        childProp.objectReferenceValue = res;
                    }
                    else {
                        childProp.objectReferenceValue = null;
                    }
                }

                if (childrenProp != null) {
                    for (var i = childrenProp.arraySize - 1; i >= 0; i--) {
                        var element = childrenProp.GetArrayElementAtIndex(i);
                        var key = element.objectReferenceValue as Node;
                        if (key != null && dict.TryGetValue(key, out var res)) {
                            element.objectReferenceValue = res;
                        }
                        else {
                            element.objectReferenceValue = null;
                            childrenProp.DeleteArrayElementAtIndex(i);
                        }
                    }
                }

                nodeObj.ApplyModifiedProperties();
            }

            return dict.Values.ToArray();
        }

        /// <summary>
        /// ノードの接続
        /// </summary>
        public static void ConnectNode(Node parent, Node child) {
            if (parent == null || child == null) {
                return;
            }

            var parentObj = new SerializedObject(parent);
            var childProp = parentObj.FindProperty("child");
            var childrenProp = parentObj.FindProperty("children");

            // リストに追加
            parentObj.Update();

            if (childProp != null) {
                childProp.objectReferenceValue = child;
            }

            if (childrenProp != null) {
                childrenProp.arraySize++;
                childrenProp.GetArrayElementAtIndex(childrenProp.arraySize - 1).objectReferenceValue = child;
            }

            parentObj.ApplyModifiedProperties();

            EditorUtility.SetDirty(parent);
        }

        /// <summary>
        /// ノードの接続解除
        /// </summary>
        public static void DisconnectNode(Node parent, Node child) {
            if (parent == null || child == null) {
                return;
            }

            var parentObj = new SerializedObject(parent);
            var childProp = parentObj.FindProperty("child");
            var childrenProp = parentObj.FindProperty("children");

            // リストから除外
            parentObj.Update();

            if (childProp != null) {
                if (childProp.objectReferenceValue == child) {
                    childProp.objectReferenceValue = null;
                }
            }

            if (childrenProp != null) {
                for (var i = childrenProp.arraySize - 1; i >= 0; i--) {
                    var element = childrenProp.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue != child) {
                        continue;
                    }

                    element.objectReferenceValue = null;
                    childrenProp.DeleteArrayElementAtIndex(i);
                }
            }

            parentObj.ApplyModifiedProperties();

            EditorUtility.SetDirty(parent);
        }

        /// <summary>
        /// 子ノードのソート
        /// </summary>
        public static void SortChildren(Node parent) {
            if (parent == null) {
                return;
            }

            var parentObj = new SerializedObject(parent);
            var childrenProp = parentObj.FindProperty("children");

            parentObj.Update();

            // 複数子要素を持つ値をソート
            if (childrenProp != null) {
                var list = new List<Node>();
                for (var i = 0; i < childrenProp.arraySize; i++) {
                    list.Add(childrenProp.GetArrayElementAtIndex(i).objectReferenceValue as Node);
                }

                list.Sort((a, b) => a.position.x.CompareTo(b.position.x));

                for (var i = 0; i < childrenProp.arraySize; i++) {
                    childrenProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                }
            }

            parentObj.ApplyModifiedProperties();

            EditorUtility.SetDirty(parent);
        }

        /// <summary>
        /// 全条件の削除
        /// </summary>
        public static void DeleteConditions(Node node) {
            if (node == null) {
                return;
            }

            var nodeObj = new SerializedObject(node);
            var conditionGroup = nodeObj.FindProperty("conditions");
            if (conditionGroup == null) {
                return;
            }

            var conditions = conditionGroup.FindPropertyRelative("conditions");
            for (var i = conditions.arraySize - 1; i >= 0; i--) {
                DeleteCondition(conditions, i);
            }

            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// 条件リストへの条件追加
        /// </summary>
        public static Condition CreateCondition(SerializedProperty listProperty, Type type) {
            var serializedObject = listProperty.serializedObject;
            var targetObject = serializedObject.targetObject;

            var condition = ScriptableObject.CreateInstance(type) as Condition;
            Undo.RegisterCreatedObjectUndo(condition, "CreateCondition");

            if (condition == null) {
                return null;
            }

            condition.name = type.Name;
            AssetDatabase.AddObjectToAsset(condition, targetObject);

            serializedObject.Update();
            listProperty.arraySize++;
            listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1).objectReferenceValue = condition;
            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(targetObject);
            return condition;
        }

        /// <summary>
        /// 条件リストから条件の削除
        /// </summary>
        public static void DeleteCondition(SerializedProperty listProperty, int index) {
            var serializedObject = listProperty.serializedObject;

            // リストから除外
            serializedObject.Update();

            var condition = listProperty.GetArrayElementAtIndex(index).objectReferenceValue;
            listProperty.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            // アセットの削除
            Undo.DestroyObjectImmediate(condition);
        }

        /// <summary>
        /// MissingしたSubAssetsを削除する
        /// </summary>
        public static void RemoveMissingSubAssets(Object targetAsset) {
            // 退避用のInstanceの生成
            var targetName = targetAsset.name;
            var newInstance = ScriptableObject.CreateInstance(targetAsset.GetType());
            EditorUtility.CopySerialized(targetAsset, newInstance);

            var oldPath = AssetDatabase.GetAssetPath(targetAsset);
            var newPath = oldPath.Replace(".asset", "CLONE.asset");
            AssetDatabase.CreateAsset(newInstance, newPath);
            AssetDatabase.ImportAsset(newPath);

            // SubAssetsをクローンした物の子に退避
            var assets = AssetDatabase.LoadAllAssetsAtPath(oldPath);
            for (var i = 0; i < assets.Length; i++) {
                // 破損した物
                if (assets[i] == null) {
                    continue;
                }

                // MainAsset
                if (assets[i] == targetAsset) {
                    continue;
                }

                // サブアセットを移動させる
                AssetDatabase.RemoveObjectFromAsset(assets[i]);
                AssetDatabase.AddObjectToAsset(assets[i], newInstance);
            }

            // 名前を直す
            newInstance.name = targetName;

            EditorUtility.SetDirty(newInstance);
            AssetDatabase.SaveAssets();

            AssetDatabase.ImportAsset(oldPath);
            AssetDatabase.ImportAsset(newPath);

            // metaを残しつつ、新しいインスタンスに差し替える
            var directoryName = System.IO.Path.GetDirectoryName(Application.dataPath) ?? "";
            var globalOldPath = System.IO.Path.Combine(directoryName, oldPath);
            var globalNewPath = System.IO.Path.Combine(directoryName, newPath);

            System.IO.File.Delete(globalOldPath);
            System.IO.File.Delete(globalNewPath + ".meta");
            System.IO.File.Move(globalNewPath, globalOldPath);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// ノードの複製
        /// </summary>
        private static Node DuplicateNode(BehaviourTree tree, Node baseNode) {
            if (tree == null || baseNode == null) {
                return null;
            }

            var serializedObj = new SerializedObject(tree);
            var nodes = serializedObj.FindProperty("nodes");

            // アセットの複製
            var node = Object.Instantiate(baseNode);
            node.name = baseNode.name;
            node.guid = GUID.Generate().ToString();
            node.position += Vector2.one * 50;
            Undo.RegisterCreatedObjectUndo(node, "DuplicateNode");
            AssetDatabase.AddObjectToAsset(node, tree);

            var nodeObj = new SerializedObject(node);
            var conditionsProp = nodeObj.FindProperty("conditions");

            nodeObj.Update();

            // 条件の複製
            if (conditionsProp != null) {
                var list = conditionsProp.FindPropertyRelative("conditions");
                for (var i = list.arraySize - 1; i >= 0; i--) {
                    var conditionElement = list.GetArrayElementAtIndex(i);
                    var condition = conditionElement.objectReferenceValue;
                    if (condition == null) {
                        list.arraySize--;
                        continue;
                    }

                    var clonedCondition = Object.Instantiate(condition) as Condition;
                    Undo.RegisterCreatedObjectUndo(clonedCondition, "DuplicateCondition");
                    AssetDatabase.AddObjectToAsset(clonedCondition, node);
                    conditionElement.objectReferenceValue = clonedCondition;
                }
            }

            nodeObj.ApplyModifiedProperties();

            // Treeへの追加
            serializedObj.Update();
            nodes.arraySize++;
            var element = nodes.GetArrayElementAtIndex(nodes.arraySize - 1);
            element.objectReferenceValue = node;
            serializedObj.ApplyModifiedProperties();

            EditorUtility.SetDirty(tree);
            return node;
        }
    }
}
#endif