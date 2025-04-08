using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GameAiBehaviour.Editor {
    /// <summary>
    /// Blackboard表示用のビュー
    /// </summary>
    public class BlackboardView : VisualElement {
        public new class UxmlFactory : UxmlFactory<BlackboardView, UxmlTraits> {
        }

        // GUIContainer
        private IMGUIContainer _container;
        // スクロール位置
        private Vector2 _scroll;
        // Treeを制御しているクラス
        private IBehaviourTreeController _controller;
        // 表示に使用するBehaviourTree
        private BehaviourTree _behaviourTree;
        // Blackboard用アセット
        private SerializedProperty _blackboardAsset;
        // Blackboard用のエディタ
        private UnityEditor.Editor _blackboardEditor;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BlackboardView() {
            _container = new IMGUIContainer(OnGUI);
            Add(_container);
        }

        /// <summary>
        /// Treeを制御しているControllerの設定
        /// </summary>
        public void SetController(IBehaviourTreeController controller) {
            _controller = controller;
        }

        /// <summary>
        /// BehaviourTreeのSerializedObjectを設定
        /// </summary>
        public void SetBehaviourTreeObject(SerializedObject behaviourTreeObject) {
            if (behaviourTreeObject != null) {
                _blackboardAsset = behaviourTreeObject.FindProperty("blackboardAsset");
            }
            else {
                _blackboardAsset = null;
            }
        }

        /// <summary>
        /// BlackboardAsset用のエディタを取得
        /// </summary>
        private UnityEditor.Editor GetEditor(BlackboardAsset blackboardAsset) {
            if (blackboardAsset == null) {
                DestroyEditor();
            }
            else {
                if (_blackboardEditor != null && _blackboardEditor.target != blackboardAsset) {
                    DestroyEditor();
                }

                if (_blackboardEditor == null) {
                    _blackboardEditor = UnityEditor.Editor.CreateEditor(blackboardAsset);
                }
            }

            return _blackboardEditor;
        }

        /// <summary>
        /// BlackboardAsset用のエディタの削除
        /// </summary>
        private void DestroyEditor() {
            if (_blackboardEditor != null) {
                Object.DestroyImmediate(_blackboardEditor);
                _blackboardEditor = null;
            }
        }

        /// <summary>
        /// GUI描画
        /// </summary>
        private void OnGUI() {
            if (_controller != null) {
                // Blackboardの中身を表示
                void DrawProperty<T>(string[] keys, Func<string, T> onGui, Action<string, T> onSet) {
                    foreach (var key in keys) {
                        using (var scope = new EditorGUI.ChangeCheckScope()) {
                            var result = onGui.Invoke(key);
                            if (scope.changed) {
                                onSet?.Invoke(key, result);
                            }
                        }
                    }
                }

                var blackboard = _controller.Blackboard;
                DrawProperty(blackboard.IntegerPropertyNames,
                    key => EditorGUILayout.IntField(key, blackboard.GetInteger(key)),
                    (key, result) => blackboard.SetInteger(key, result));
                DrawProperty(blackboard.FloatPropertyNames,
                    key => EditorGUILayout.FloatField(key, blackboard.GetFloat(key)),
                    (key, result) => blackboard.SetFloat(key, result));
                DrawProperty(blackboard.StringPropertyNames,
                    key => EditorGUILayout.TextField(key, blackboard.GetString(key)),
                    (key, result) => blackboard.SetString(key, result));
                DrawProperty(blackboard.BooleanPropertyNames,
                    key => EditorGUILayout.Toggle(key, blackboard.GetBoolean(key)),
                    (key, result) => blackboard.SetBoolean(key, result));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("[ConstProperties]", EditorStyles.miniBoldLabel);

                DrawProperty(blackboard.ConstIntegerPropertyNames,
                    key => EditorGUILayout.IntField(key, blackboard.GetInteger(key)),
                    null);
                DrawProperty(blackboard.ConstFloatPropertyNames,
                    key => EditorGUILayout.FloatField(key, blackboard.GetFloat(key)),
                    null);
                DrawProperty(blackboard.ConstStringPropertyNames,
                    key => EditorGUILayout.TextField(key, blackboard.GetString(key)),
                    null);
                DrawProperty(blackboard.ConstBooleanPropertyNames,
                    key => EditorGUILayout.Toggle(key, blackboard.GetBoolean(key)),
                    null);
            }
            else {
                if (_blackboardAsset != null) {
                    // BlackboardAssetの参照設定
                    _blackboardAsset.serializedObject.Update();
                    EditorGUILayout.PropertyField(_blackboardAsset);
                    _blackboardAsset.serializedObject.ApplyModifiedProperties();

                    // BlackboardAssetの中身表示
                    var asset = _blackboardAsset.objectReferenceValue as BlackboardAsset;
                    if (asset != null) {
                        using (var scope = new EditorGUILayout.ScrollViewScope(_scroll)) {
                            var editor = GetEditor(asset);
                            var serializedObj = new SerializedObject(asset);
                            serializedObj.Update();
                            editor.OnInspectorGUI();
                            serializedObj.ApplyModifiedProperties();
                            _scroll = scope.scrollPosition;
                        }
                    }
                    else {
                        DestroyEditor();
                    }
                }
                else {
                    _scroll = Vector2.zero;
                }
            }
        }
    }
}