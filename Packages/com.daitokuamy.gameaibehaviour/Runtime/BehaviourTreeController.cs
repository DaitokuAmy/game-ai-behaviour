using System;
using System.Collections.Generic;
using System.Linq;

namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTree制御クラス
    /// </summary>
    public class BehaviourTreeController : IBehaviourTreeController, IDisposable {
        /// <summary>
        /// ActionHandler情報
        /// </summary>
        private class ActionHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
        }

        /// <summary>
        /// DecorationHandler情報
        /// </summary>
        private class DecorationHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
        }

        /// <summary>
        /// LinkHandler情報
        /// </summary>
        private class LinkHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
        }

        // 廃棄済みフラグ
        private bool _disposed;
        // 実行データ
        private BehaviourTree _data;
        // 思考リセットフラグ
        private bool _thinkResetFlag;
        // 思考タイミング用タイマー
        private float _tickTimer;
        // アクションハンドラ情報
        private readonly Dictionary<Type, ActionHandlerInfo> _actionHandlerInfos = new();
        // アクションノードハンドラ
        private readonly Dictionary<Node, IActionNodeHandler> _actionNodeHandlers = new();
        // デコレーションハンドラ情報
        private readonly Dictionary<Type, DecorationHandlerInfo> _decorationHandlerInfos = new();
        // デコレーションノードハンドラ
        private readonly Dictionary<Node, IDecorationNodeHandler> _decorationNodeHandlers = new();
        // リンクハンドラ情報
        private readonly Dictionary<Type, LinkHandlerInfo> _linkHandlerInfos = new();
        // リンクノードハンドラ
        private readonly Dictionary<Node, ILinkNodeHandler> _linkNodeHandlers = new();

        // ベースとなるTreeのRunner
        private BehaviourTreeRunner _baseRunner;
        // サブルーチン用のRunner
        private Dictionary<Node.ILogic, BehaviourTreeRunner> _subRoutineRunners =
            new Dictionary<Node.ILogic, BehaviourTreeRunner>();

        // 思考開始からの経過時間
        public float ThinkTime { get; private set; }
        // プロパティ管理用Blackboard
        public Blackboard Blackboard { get; private set; } = new Blackboard();
        // 思考頻度
        public float TickInterval { get; set; }
        // 実行履歴パス
        public IReadOnlyList<BehaviourTreeRunner.Path> ExecutedPaths => _baseRunner != null ? _baseRunner.ExecutedPaths : new List<BehaviourTreeRunner.Path>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BehaviourTreeController() {
        }

        /// <summary>
        /// 廃棄処理
        /// </summary>
        public void Dispose() {
            if (!_disposed) {
                return;
            }
            
            ResetActionNodeHandlers();
            ResetDecorationNodeHandlers();
            ResetLinkNodeHandlers();
            Cleanup();
            
            // このフラグによってEarlyReturnしている処理が含まれているので、一番最後でフラグを立てる
            _disposed = true;
        }

        /// <summary>
        /// ActionNodeHandlerのBind
        /// </summary>
        /// <param name="onInit">Handlerの初期化関数</param>
        public void BindActionNodeHandler<TNode, THandler>(Action<THandler> onInit)
            where TNode : HandleableActionNode
            where THandler : ActionNodeHandler<TNode>, new() {
            if (_disposed) {
                return;
            }
            
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
        /// DecorationNodeHandlerのBind
        /// </summary>
        /// <param name="onInit">Handlerの初期化関数</param>
        public void BindDecorationNodeHandler<TNode, THandler>(Action<THandler> onInit)
            where TNode : HandleableDecorationNode
            where THandler : DecorationNodeHandler<TNode>, new ()
        {
            if (_disposed) return;
            
            ResetDecorationNodeHandler<TNode>();

            _decorationHandlerInfos[typeof(TNode)] = new DecorationHandlerInfo
            {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }
        
        /// <summary>
        /// LinkNodeHandlerのBind
        /// </summary>
        /// <param name="onInit">Handlerの初期化関数</param>
        public void BindLinkNodeHandler<TNode, THandler>(Action<THandler> onInit)
            where TNode : HandleableLinkNode
            where THandler : LinkNodeHandler<TNode>, new ()
        {
            if (_disposed) return;
            
            ResetLinkNodeHandler<TNode>();

            _linkHandlerInfos[typeof(TNode)] = new LinkHandlerInfo
            {
                Type = typeof(THandler),
                InitAction = onInit != null ? obj => { onInit.Invoke(obj as THandler); } : null
            };
        }

        /// <summary>
        /// ActionNodeHandlerのBindを解除
        /// </summary>
        public void ResetActionNodeHandler<TNode>()
            where TNode : HandleableActionNode {
            if (_disposed) {
                return;
            }
            
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
        /// DecorationNodeHandlersのBindを解除
        /// </summary>
        public void ResetDecorationNodeHandler<TNode>()
            where TNode : HandleableDecorationNode
        {
            if (_disposed) return;

            _decorationHandlerInfos.Remove(typeof(TNode));
            
            // 既に登録済のHandlerがあった場合は削除する
            var removeKeys = _decorationNodeHandlers.Keys
                .Where(x => x.GetType() == typeof(TNode))
                .ToArray();
            foreach (var removeKey in removeKeys) {
                _decorationNodeHandlers.Remove(removeKey);
            }
        }

        /// <summary>
        /// LinkNodeHandlersのBindを解除
        /// </summary>
        public void ResetLinkNodeHandler<TNode>()
            where TNode : HandleableLinkNode
        {
            if (_disposed) return;

            _linkHandlerInfos.Remove(typeof(TNode));
            
            // 既に登録済のHandlerがあった場合は削除する
            var removeKeys = _linkNodeHandlers.Keys
                .Where(x => x.GetType() == typeof(TNode))
                .ToArray();
            foreach (var removeKey in removeKeys) {
                _linkNodeHandlers.Remove(removeKey);
            }
        }

        /// <summary>
        /// ActionNodeHandlerのBindを一括解除
        /// </summary>
        public void ResetActionNodeHandlers() {
            if (_disposed) {
                return;
            }
            
            _actionHandlerInfos.Clear();
            _actionNodeHandlers.Clear();
        }

        /// <summary>
        /// DecorationNodeHandlerのBindを一括解除
        /// </summary>
        public void ResetDecorationNodeHandlers()
        {
            if (_disposed) return;
            _decorationHandlerInfos.Clear();
            _decorationNodeHandlers.Clear();
        }

        /// <summary>
        /// LinkNodeHandlerのBindを一括解除
        /// </summary>
        public void ResetLinkNodeHandlers()
        {
            if (_disposed) return;
            _linkHandlerInfos.Clear();
            _linkNodeHandlers.Clear();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Setup(BehaviourTree data) {
            if (_disposed) {
                return;
            }
            
            Cleanup();

            _data = data;

            if (_data == null) {
                return;
            }

            _baseRunner = new BehaviourTreeRunner(this, _data.rootNode);

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
            if (_disposed) {
                return;
            }
            
            foreach (var runner in _subRoutineRunners.Values) {
                runner?.ResetThink();
            }
            _baseRunner?.ResetThink();

            _tickTimer = 0.0f;
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Cleanup() {
            if (_disposed) {
                return;
            }
            
            ResetThink();

            Blackboard.Clear();

            foreach (var runner in _subRoutineRunners.Values) {
                runner?.Cleanup();
            }
            _subRoutineRunners.Clear();
            _baseRunner?.Cleanup();
            _baseRunner = null;

            _data = null;
            ResetActionNodeHandlers();
            ResetDecorationNodeHandlers();
            ResetLinkNodeHandlers();
        }

        /// <summary>
        /// Tree更新
        /// </summary>
        public void Update(float deltaTime) {
            if (_disposed || _data == null || _baseRunner == null) {
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

            // 基本思考の実行
            _baseRunner.Tick(() => ThinkTime = 0.0f);

            // サブルーチンの実行
            foreach (var runner in _subRoutineRunners.Values) {
                runner.Tick();
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
        /// DecorationNodeのハンドリング用インスタンスを取得
        /// </summary>
        IDecorationNodeHandler IBehaviourTreeController.GetDecoratorHandler(HandleableDecorationNode node)
        {
            if (node == null) return null;

            if (_decorationNodeHandlers.TryGetValue(node, out var handler)) return handler;
            
            // 無ければ生成
            if (_decorationHandlerInfos.TryGetValue(node.GetType(), out var handlerInfo))
            {
                var constructorInfo = handlerInfo.Type.GetConstructor(Type.EmptyTypes);
                if (constructorInfo != null) {
                    handler = (IDecorationNodeHandler)constructorInfo.Invoke(Array.Empty<object>());
                    _decorationNodeHandlers[node] = handler;
                    handlerInfo.InitAction?.Invoke(handler);
                }
            }

            return handler;
        }

        /// <summary>
        /// LinkNodeのハンドリング用インスタンスを取得
        /// </summary>
        ILinkNodeHandler IBehaviourTreeController.GetLinkHandler(HandleableLinkNode node)
        {
            if (node == null) return null;

            if (_linkNodeHandlers.TryGetValue(node, out var handler)) return handler;
            
            // 無ければ生成
            if (_linkHandlerInfos.TryGetValue(node.GetType(), out var handlerInfo))
            {
                var constructorInfo = handlerInfo.Type.GetConstructor(Type.EmptyTypes);
                if (constructorInfo != null) {
                    handler = (ILinkNodeHandler)constructorInfo.Invoke(Array.Empty<object>());
                    _linkNodeHandlers[node] = handler;
                    handlerInfo.InitAction?.Invoke(handler);
                }
            }

            return handler;
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
        /// サブルーチンの設定
        /// </summary>
        void IBehaviourTreeController.SetSubRoutine(Node.ILogic parent, Node startNode) {
            ((IBehaviourTreeController)this).ResetSubRoutine(parent);

            if (startNode == null) {
                return;
            }

            var runner = new BehaviourTreeRunner(this, startNode);
            _subRoutineRunners[parent] = runner;
        }

        /// <summary>
        /// サブルーチンの削除
        /// </summary>
        void IBehaviourTreeController.ResetSubRoutine(Node.ILogic parent) {
            if (!_subRoutineRunners.TryGetValue(parent, out var runner) || runner == null) {
                return;
            }

            runner.Cleanup();
            _subRoutineRunners.Remove(parent);
        }
    }
}