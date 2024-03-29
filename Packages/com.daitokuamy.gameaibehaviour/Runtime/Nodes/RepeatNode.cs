﻿using System.Collections;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// 指定回数繰り返しノード
    /// </summary>
    public sealed class RepeatNode : DecoratorNode {
        private class Logic : Logic<RepeatNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, RepeatNode node) : base(runner, node) {
            }

            /// <summary>
            /// 実行ルーチン
            /// </summary>
            protected override IEnumerator ExecuteRoutineInternal() {
                // 子がいない場合は失敗
                if (Node.child == null) {
                    SetState(State.Failure);
                    yield break;
                }

                // 指定回数繰り返す
                for (var i = 0; i < Node.count; i++) {
                    // 接続先ノードの実行
                    yield return ExecuteNodeRoutine(Node.child, SetState);

                    // 成功していたら継続
                    if (State == State.Success) {
                        // 最後だったら終わる
                        if (i >= Node.count - 1) {
                            break;
                        }

                        yield return this;
                    }
                    // 失敗していたら完了
                    else if (State == State.Failure) {
                        yield break;
                    }
                }

                // 一度も実行されない場合を見越して成功にしておく
                SetState(State.Success);
            }
        }

        [Tooltip("繰り返し回数")]
        public int count = 1;

        public override string Description => $"x {count}";

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeRunner runner) {
            return new Logic(runner, this);
        }
    }
}