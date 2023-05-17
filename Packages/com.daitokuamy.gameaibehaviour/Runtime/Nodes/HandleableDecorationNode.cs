using System.Collections;
using UnityEngine;

namespace GameAiBehaviour
{
    /// <summary>
    /// ハンドリング拡張可能なデコレーションノード
    /// </summary>
    public abstract class HandleableDecorationNode : DecoratorNode
    {
        private class Logic : Logic<HandleableDecorationNode>
        {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, HandleableDecorationNode node) : base(runner, node)
            {
            }

            /// <summary>
            /// 実行ルーチン
            /// </summary>
            protected sealed override IEnumerator ExecuteRoutineInternal()
            {
                var handler = Controller.GetDecoratorHandler(Node);
                
                // Handlerが無ければ、ログを出して終了
                if (handler == null) {
                    Debug.Log($"Invoke DecorationNode[{GetType().Name}]");
                    SetState(State.Success);
                    yield break;
                }
                
                // 子がいない場合は失敗
                if (Node.child == null)
                {
                    SetState(State.Failure);
                    yield break;
                }

                // 条件判定に失敗したら失敗
                if (!handler.Check(Controller.Blackboard, Node))
                {
                    SetState(State.Failure);
                    yield break;
                }

                // 接続先ノードの実行
                yield return ExecuteNodeRoutine(Node.child, SetState);
            }
        }

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeRunner runner)
        {
            return new Logic(runner, this);
        }
    }
}