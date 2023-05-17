using System.Collections;
using UnityEngine;

namespace GameAiBehaviour
{
    /// <summary>
    /// ハンドリング拡張可能なリンクノード
    /// </summary>
    public class HandleableLinkNode : LinkNode
    {
        private class Logic : Logic<HandleableLinkNode>
        {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, HandleableLinkNode node) : base(runner, node)
            {
            }

            /// <summary>
            /// 実行ルーチン
            /// </summary>
            protected override IEnumerator ExecuteRoutineInternal()
            {
                var handler = Controller.GetLinkHandler(Node);
                
                // Handlerが無ければ、ログを出して終了
                if (handler == null) {
                    Debug.Log($"Invoke LinkNode[{GetType().Name}]");
                    SetState(State.Success);
                    yield break;
                }
                
                // 接続先がない場合は失敗
                var connectTree = handler.GetConnectTree(Node);
                if (connectTree == null)
                {
                    SetState(State.Failure);
                    yield break;
                }
                
                // 接続先の開始前の処理
                handler.OnEnter(Node);

                // 接続先ノードの実行
                yield return ExecuteNodeRoutine(connectTree.rootNode, SetState);
                
                // 接続先の完了後の処理
                handler.OnExit(Node);
            }

            /// <summary>
            /// 思考リセット時の処理(Override用)
            /// </summary>
            protected override void ResetInternal()
            {
                var handler = Controller.GetLinkHandler(Node);
                handler?.OnReset(Node);
            }
        }

        public override ILogic CreateLogic(IBehaviourTreeRunner runner)
        {
            return new Logic(runner, this);
        }
    }
}