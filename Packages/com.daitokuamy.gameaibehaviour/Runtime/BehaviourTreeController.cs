using System;
using System.Collections.Generic;

namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTree制御クラス
    /// </summary>
    public class BehaviourTreeController : IBehaviourTreeController, IDisposable {
        /// <summary>
        /// ActionHandler情報
        /// </summary>
        internal class ActionHandlerInfo {
            public Type Type;
            public Action<object> InitAction;
        }

        // 廃棄済みフラグ
        private bool _disposed;
        // 思考リセットフラグ
        private bool _thinkResetFlag;
        // 思考タイミング用タイマー
        private float _tickTimer;
        // アクションハンドラ情報
        private readonly Dictionary<Type, ActionHandlerInfo> _actionHandlerInfos = new();
        // リンクノードハンドラ
        private readonly Dictionary<Type, ILinkNodeHandler> _linkNodeHandlers = new();
        // コンディションハンドラ
        private readonly Dictionary<Type, IConditionHandler> _conditionHandlers = new();

        // ベースとなるTreeのRunner
        private BehaviourTreeRunner _baseRunner;
        // サブルーチン用のRunner
        private Dictionary<Node.ILogic, BehaviourTreeRunner> _subRoutineRunners = new();

        /// <summary>思考開始からの経過時間</summary>
        public float ThinkTime { get; private set; }
        /// <summary>プロパティ管理用Blackboard</summary>
        public Blackboard Blackboard { get; private set; } = new Blackboard();
        /// <summary>実行中ツリー</summary>
        public BehaviourTree Tree { get; private set; }
        /// <summary>思考頻度</summary>
        public float TickInterval { get; set; }
        /// <summary>実行履歴パス</summary>
        public IReadOnlyList<BehaviourTreeRunner.Path> ExecutedPaths =>
            _baseRunner != null ? _baseRunner.ExecutedPaths : new List<BehaviourTreeRunner.Path>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public BehaviourTreeController() {
        }

        /// <summary>
        /// 廃棄処理
        /// </summary>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            Cleanup();

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
        /// <param name="cancelAction">キャンセル関数</param>
        public void BindActionNodeHandler<TNode>(Func<TNode, IActionNodeHandler.State> updateFunc,
            Func<TNode, bool> enterAction = null, Action<TNode> exitAction = null, Action<TNode> cancelAction = null)
            where TNode : HandleableActionNode {
            BindActionNodeHandler<TNode, ObserveActionNodeHandler<TNode>>(handler => {
                handler.SetEnterAction(enterAction);
                handler.SetUpdateFunc(updateFunc);
                handler.SetExitAction(exitAction);
                handler.SetCancelAction(cancelAction);
            });
        }

        /// <summary>
        /// ActionNodeHandlerのBindを解除
        /// </summary>
        public void ResetActionNodeHandler<TNode>()
            where TNode : HandleableActionNode {
            if (_disposed) {
                return;
            }

            // 既に登録済のHandlerがあった場合は削除する
            _baseRunner?.RemoveActionNodeHandlers(typeof(TNode));
            foreach (var pair in _subRoutineRunners) {
                pair.Value.RemoveActionNodeHandlers(typeof(TNode));
            }

            // Handler情報を削除
            _actionHandlerInfos.Remove(typeof(TNode));
        }

        /// <summary>
        /// ActionNodeHandlerのBindを一括解除
        /// </summary>
        public void ResetActionNodeHandlers() {
            if (_disposed) {
                return;
            }

            // 既に登録済のHandlerがあった場合は削除する
            _baseRunner?.RemoveActionNodeHandlers();
            foreach (var pair in _subRoutineRunners) {
                pair.Value.RemoveActionNodeHandlers();
            }

            // Handler情報を削除
            _actionHandlerInfos.Clear();
        }

        /// <summary>
        /// LinkNodeHandlerのBind
        /// </summary>
        /// <param name="onInit">Handlerの初期化関数</param>
        public void BindLinkNodeHandler<TLinkNode, THandler>(Action<THandler> onInit)
            where TLinkNode : HandleableLinkNode
            where THandler : LinkNodeHandler<TLinkNode>, new() {
            if (_disposed) {
                return;
            }

            ResetLinkNodeHandler<TLinkNode>();

            var handler = new THandler();
            _linkNodeHandlers[typeof(TLinkNode)] = handler;
            onInit?.Invoke(handler);
        }

        /// <summary>
        /// LinkNodeHandlerのBind
        /// </summary>
        /// <param name="getConnectNodeFunc">接続先ノード取得関数</param>
        public void BindLinkNodeHandler<TLinkNode>(Func<TLinkNode, Node> getConnectNodeFunc)
            where TLinkNode : HandleableLinkNode {
            BindLinkNodeHandler<TLinkNode, ObserveLinkNodeHandler<TLinkNode>>(handler => {
                handler.SetGetConnectNodeFunc(getConnectNodeFunc);
            });
        }

        /// <summary>
        /// LinkNodeHandlerのBindを解除
        /// </summary>
        public void ResetLinkNodeHandler<TLinkNode>()
            where TLinkNode : HandleableLinkNode {
            if (_disposed) {
                return;
            }

            _linkNodeHandlers.Remove(typeof(TLinkNode));
        }

        /// <summary>
        /// LinkNodeHandlerのBindを一括解除
        /// </summary>
        public void ResetLinkNodeHandlers() {
            if (_disposed) {
                return;
            }

            _linkNodeHandlers.Clear();
        }

        /// <summary>
        /// ConditionHandlerのBind
        /// </summary>
        /// <param name="onInit">Handlerの初期化関数</param>
        public void BindConditionHandler<TCondition, THandler>(Action<THandler> onInit)
            where TCondition : HandleableCondition
            where THandler : ConditionHandler<TCondition>, new() {
            if (_disposed) {
                return;
            }

            ResetConditionHandler<TCondition>();

            var handler = new THandler();
            _conditionHandlers[typeof(TCondition)] = handler;
            onInit?.Invoke(handler);
        }

        /// <summary>
        /// ConditionHandlerのBind
        /// </summary>
        /// <param name="checkFunc">判定関数</param>
        public void BindConditionHandler<TCondition>(Func<TCondition, Blackboard, bool> checkFunc)
            where TCondition : HandleableCondition {
            BindConditionHandler<TCondition, ObserveConditionHandler<TCondition>>(handler => {
                handler.SetCheckFunc(checkFunc);
            });
        }

        /// <summary>
        /// ConditionHandlerのBindを解除
        /// </summary>
        public void ResetConditionHandler<TCondition>()
            where TCondition : HandleableCondition {
            if (_disposed) {
                return;
            }

            _conditionHandlers.Remove(typeof(TCondition));
        }

        /// <summary>
        /// ConditionHandlerのBindを一括解除
        /// </summary>
        public void ResetConditionHandlers() {
            if (_disposed) {
                return;
            }

            _conditionHandlers.Clear();
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Setup(BehaviourTree tree) {
            if (_disposed) {
                return;
            }

            Cleanup();

            Tree = tree;

            if (Tree == null) {
                return;
            }

            _baseRunner = new BehaviourTreeRunner(this, Tree.rootNode);

            // Blackboard初期化
            if (tree.blackboardAsset != null) {
                foreach (var property in tree.blackboardAsset.properties) {
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
                
                foreach (var property in tree.blackboardAsset.constProperties) {
                    switch (property.propertyType) {
                        case Property.Type.Integer:
                            Blackboard.SetConstInteger(property.propertyName, property.integerValue);
                            break;
                        case Property.Type.Float:
                            Blackboard.SetConstFloat(property.propertyName, property.floatValue);
                            break;
                        case Property.Type.String:
                            Blackboard.SetConstString(property.propertyName, property.stringValue);
                            break;
                        case Property.Type.Boolean:
                            Blackboard.SetConstBoolean(property.propertyName, property.booleanValue);
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
        /// 思考タイミングの遅延を設定
        /// </summary>
        public void SetTickDelay(float time) {
            _tickTimer = time;
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

            Tree = null;
            _actionHandlerInfos.Clear();
            _linkNodeHandlers.Clear();
            _conditionHandlers.Clear();
        }

        /// <summary>
        /// 乱数生成用のロジックを設定
        /// </summary>
        public void SetRandom(IRandom random) {
            if (random == null) {
                random = new UnityRandom();
            }
            
            _baseRunner.Random = random;
            foreach (var pair in _subRoutineRunners) {
                pair.Value.Random = random;
            }
        }

        /// <summary>
        /// Tree更新
        /// </summary>
        public Node.State Update(float deltaTime) {
            if (_disposed || Tree == null || _baseRunner == null) {
                return Node.State.Failure;
            }

            // 思考時間更新
            ThinkTime += deltaTime;

            // Tickタイマー更新
            if (_tickTimer > 0.0f) {
                _tickTimer -= deltaTime;
                return _baseRunner.CurrentState;
            }

            _tickTimer = TickInterval;

            // 基本思考の実行
            var result = _baseRunner.Tick(() => ThinkTime = 0.0f);

            // サブルーチンの実行
            foreach (var runner in _subRoutineRunners.Values) {
                runner.Tick();
            }

            // 思考リセットフラグが立っていたらリセットする
            if (_thinkResetFlag) {
                ResetThink();
                _thinkResetFlag = false;
            }

            return result;
        }

        /// <summary>
        /// ActionNodeHandlerの生成
        /// </summary>
        IActionNodeHandler IBehaviourTreeController.CreateActionNodeHandler(HandleableActionNode node) {
            var handler = default(IActionNodeHandler);
            if (_actionHandlerInfos.TryGetValue(node.GetType(), out var handlerInfo)) {
                var constructorInfo = handlerInfo.Type.GetConstructor(Type.EmptyTypes);
                if (constructorInfo != null) {
                    handler = (IActionNodeHandler)constructorInfo.Invoke(Array.Empty<object>());
                    handlerInfo.InitAction?.Invoke(handler);
                }
            }
            
            return handler;
        }

        /// <summary>
        /// LinkNodeのハンドリング用インスタンスを取得
        /// </summary>
        ILinkNodeHandler IBehaviourTreeController.GetLinkNodeHandler(HandleableLinkNode node) {
            if (node == null) {
                return null;
            }

            if (_linkNodeHandlers.TryGetValue(node.GetType(), out var handler)) {
                return handler;
            }

            return null;
        }

        /// <summary>
        /// Conditionのハンドリング用インスタンスを取得
        /// </summary>
        IConditionHandler IBehaviourTreeController.GetConditionHandler(HandleableCondition condition) {
            if (condition == null) {
                return null;
            }

            if (_conditionHandlers.TryGetValue(condition.GetType(), out var handler)) {
                return handler;
            }

            return null;
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

            var runner = new BehaviourTreeRunner(this, startNode, _baseRunner.Random);
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