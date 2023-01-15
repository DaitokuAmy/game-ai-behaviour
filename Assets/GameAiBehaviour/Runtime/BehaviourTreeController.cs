﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTree制御クラス
    /// </summary>
    public class BehaviourTreeController : IBehaviourTreeController {
        /// <summary>
        /// 実行パス
        /// </summary>
        public struct Path {
            public Node.ILogic prev;
            public Node.ILogic next;
        }
        
        /// <summary>
        /// ActionHandler情報
        /// </summary>
        private class ActionHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
        }

        // 実行データ
        private BehaviourTree _data;
        // Rootノード
        private RootNode _rootNode;
        // 思考リセットフラグ
        private bool _thinkResetFlag;
        // 思考タイミング用タイマー
        private float _tickTimer;
        // ノードロジック
        private Dictionary<Node, Node.ILogic> _logics = new Dictionary<Node, Node.ILogic>();
        // アクションハンドラ情報
        private Dictionary<Type, ActionHandlerInfo> _actionHandlerInfos = new Dictionary<Type, ActionHandlerInfo>();
        // アクションノードハンドラ
        private readonly Dictionary<Node, IActionNodeHandler> _actionNodeHandlers =
            new Dictionary<Node, IActionNodeHandler>();
        // 実行用ルーチン
        private NodeLogicRoutine _nodeLogicRoutine;
        // 実行履歴パス
        private List<Path> _executedPaths = new List<Path>(); 

        // 思考開始からの経過時間
        public float ThinkTime { get; private set; }
        // プロパティ管理用Blackboard
        public Blackboard Blackboard { get; private set; } = new Blackboard();
        // 思考頻度
        public float TickInterval { get; set; } = 0;
        // 現在の実行状態
        public Node.State CurrentState => _nodeLogicRoutine?.Current?.State ?? Node.State.Inactive;
        // 実行中か
        public bool IsRunning => _nodeLogicRoutine != null;
        // 実行履歴のパス
        public IReadOnlyList<Path> ExecutedPaths => _executedPaths;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BehaviourTreeController() {
        }

        /// <summary>
        /// ActionNodeHandlerのBind
        /// </summary>
        /// <param name="onInit">Handlerの初期化関数</param>
        public void BindActionNodeHandler<TNode, THandler>(Action<THandler> onInit)
            where TNode : HandleableActionNode
            where THandler : ActionNodeHandler<TNode>, new() {
            ResetActionNodeHandler<TNode>();

            _actionHandlerInfos[typeof(TNode)] = new ActionHandlerInfo {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }

        /// <summary>
        /// ActionNodeHandlerのBind
        /// </summary>
        /// <param name="updateFunc">更新関数</param>
        /// <param name="enterAction">開始関数</param>
        /// <param name="exitAction">終了関数</param>
        public void BindActionNodeHandler<TNode>(Func<TNode, IActionNodeHandler.State> updateFunc,
            Func<TNode, bool> enterAction = null, Action<TNode> exitAction = null)
            where TNode : HandleableActionNode {
            BindActionNodeHandler<TNode, ObserveActionNodeHandler<TNode>>(handler => {
                handler.SetEnterAction(enterAction);
                handler.SetUpdateFunc(updateFunc);
                handler.SetExitAction(exitAction);
            });
        }

        /// <summary>
        /// ActionNodeHandlerのBindを解除
        /// </summary>
        public void ResetActionNodeHandler<TNode>()
            where TNode : HandleableActionNode {
            _actionHandlerInfos.Remove(typeof(TNode));

            // 既に登録済のHandlerがあった場合は削除する
            var removeKeys = _actionNodeHandlers.Keys
                .Where(x => x.GetType() == typeof(TNode))
                .ToArray();
            foreach (var removeKey in removeKeys) {
                _actionNodeHandlers.Remove(removeKey);
            }
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Setup(BehaviourTree data) {
            Cleanup();

            _data = data;

            if (_data == null) {
                return;
            }

            _rootNode = _data.rootNode;
            _logics = _data.nodes.ToDictionary(x => x, x => x.CreateLogic(this));
            foreach (var pair in _logics) {
                pair.Value.Initialize();
            }

            // Blackboard初期化
            if (data.blackboardAsset != null) {
                foreach (var property in data.blackboardAsset.properties) {
                    switch (property.propertyType) {
                        case Property.Type.Integer:
                            Blackboard.SetInteger(property.propertyName, property.integerValue);
                            break;
                        case Property.Type.Float:
                            Blackboard.SetFloat(property.propertyName, property.floatValue);
                            break;
                        case Property.Type.String:
                            Blackboard.SetString(property.propertyName, property.stringValue);
                            break;
                        case Property.Type.Boolean:
                            Blackboard.SetBoolean(property.propertyName, property.booleanValue);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 思考リセット
        /// </summary>
        public void ResetThink() {
            foreach (var logic in _logics) {
                logic.Value.Reset();
            }

            _nodeLogicRoutine = null;
            _executedPaths.Clear();
            _tickTimer = 0.0f;
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Cleanup() {
            ResetThink();

            Blackboard.Clear();

            _data = null;
            _rootNode = null;
            foreach (var pair in _logics) {
                pair.Value.Dispose();
            }

            _logics.Clear();
            _actionHandlerInfos.Clear();
            _actionNodeHandlers.Clear();
        }

        /// <summary>
        /// Tree更新
        /// </summary>
        public void Update(float deltaTime) {
            if (_data == null || _rootNode == null) {
                return;
            }

            // 思考時間更新
            ThinkTime += deltaTime;

            // Tickタイマー更新
            if (_tickTimer > 0.0f) {
                _tickTimer -= deltaTime;
                return;
            }

            _tickTimer = TickInterval;

            void UpdateRoutine() {
                if (!_nodeLogicRoutine.MoveNext()) {
                    _nodeLogicRoutine = null;
                }
            }

            if (_nodeLogicRoutine != null) {
                // ルーチン実行
                UpdateRoutine();
            }
            else {
                // 実行状態をリセット
                foreach (var logic in _logics) {
                    logic.Value.Reset();
                }
                
                _executedPaths.Clear();
                ThinkTime = 0.0f;

                // RootNode起点のRoutineを生成
                var rootLogic = ((IBehaviourTreeController)this).FindLogic(_rootNode);
                _nodeLogicRoutine = new NodeLogicRoutine(rootLogic.ExecuteRoutine());

                // ルーチン実行
                UpdateRoutine();
            }

            // 思考リセットフラグが立っていたらリセットする
            if (_thinkResetFlag) {
                ResetThink();
                _thinkResetFlag = false;
            }
        }

        /// <summary>
        /// ActionNodeのハンドリング用インスタンスを取得
        /// </summary>
        IActionNodeHandler IBehaviourTreeController.GetActionHandler(HandleableActionNode node) {
            if (node == null) {
                return null;
            }

            if (_actionNodeHandlers.TryGetValue(node, out var handler)) {
                return handler;
            }

            // 無ければ生成
            if (_actionHandlerInfos.TryGetValue(node.GetType(), out var handlerInfo)) {
                var constructorInfo = handlerInfo.Type.GetConstructor(Type.EmptyTypes);
                if (constructorInfo != null) {
                    handler = (IActionNodeHandler)constructorInfo.Invoke(Array.Empty<object>());
                    _actionNodeHandlers[node] = handler;
                    handlerInfo.InitAction?.Invoke(handler);
                }
            }

            return handler;
        }

        /// <summary>
        /// ノード用ロジックを検索
        /// </summary>
        Node.ILogic IBehaviourTreeController.FindLogic(Node node) {
            if (node == null) {
                return null;
            }

            if (_logics.TryGetValue(node, out var logic)) {
                return logic;
            }
            
            // なかった場合は作成
            logic = node.CreateLogic(this);
            logic.Initialize();
            _logics[node] = logic;

            return logic;
        }

        /// <summary>
        /// 思考リセット
        /// </summary>
        void IBehaviourTreeController.ResetThink() {
            _thinkResetFlag = true;
        }

        /// <summary>
        /// ThickTimerのリセット(次回の思考Intervalがなくなる)
        /// </summary>
        void IBehaviourTreeController.ResetTickTimer() {
            _tickTimer = 0.0f;
        }

        /// <summary>
        /// 実行パスの追加
        /// </summary>
        void IBehaviourTreeController.AddExecutedPath(Node.ILogic prev, Node.ILogic next) {
            _executedPaths.Add(new Path { prev = prev, next = next });
        }
    }
}