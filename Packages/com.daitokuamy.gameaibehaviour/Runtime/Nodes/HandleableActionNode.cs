using System.Collections;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// ハンドリング拡張可能な実行ノード（アプリケーション依存度の高いActionNodeはこちらを使う）
    /// </summary>
    public abstract class HandleableActionNode : ActionNode {
        private class Logic : Logic<HandleableActionNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, HandleableActionNode node) : base(runner, node) {
            }

            /// <summary>
            /// 実行ルーチン
            /// </summary>
            protected sealed override IEnumerator ExecuteActionRoutineInternal() {
                var handler = Controller.GetActionHandler(Node);

                // Handlerが無ければ、ログを出して終了
                if (handler == null) {
                    Debug.Log($"Invoke ActionNode[{GetType().Name}]");
                    SetState(State.Success);
                    yield break;
                }

                // 開始処理実行、Enterで失敗したらその時点でエラー
                if (!handler.OnEnter(Node)) {
                    SetState(State.Failure);
                    handler.OnExit(Node);
                    yield break;
                }
                
                // サブルーチン実行
                if (Node.subRoutine != null) {
                    Controller.SetSubRoutine(this, Node.subRoutine);
                }

                // 更新処理実行
                while (true) {
                    var state = handler.OnUpdate(Node);
                    if (state != IActionNodeHandler.State.Running) {
                        SetState(state == IActionNodeHandler.State.Success ? State.Success : State.Failure);
                        break;
                    }

                    yield return this;
                }
                
                // サブルーチン停止
                if (Node.subRoutine != null) {
                    Controller.ResetSubRoutine(this);
                }

                // 終了処理実行
                handler.OnExit(Node);
            }
        }

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeRunner runner) {
            return new Logic(runner, this);
        }
    }
}