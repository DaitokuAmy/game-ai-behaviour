﻿using System.Collections;
using System.Linq;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// 繰り返しノード
    /// </summary>
    public sealed class WhileNode : DecoratorNode {
        private class Logic : Logic<WhileNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, WhileNode node) : base(runner, node) {
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

                // 条件判定に失敗するまで繰り返す
                while (Node.conditions.Check(Controller)) {
                    // 接続先ノードの実行
                    yield return ExecuteNodeRoutine(Node.child, SetState);

                    // 成功していたら処理継続
                    if (State == State.Success) {
                        yield return this;
                    }
                    // 失敗していたら失敗して抜け出す
                    else if (State == State.Failure) {
                        yield break;
                    }
                }

                // 一度も実行されない場合を見越して成功にしておく
                SetState(State.Success);
            }
        }

        [Tooltip("条件")]
        public ConditionGroup conditions;

        public override string Description => string.Join("\n", conditions.conditions.Select(x => x.ConditionTitle));
        public override float NodeWidth => 200.0f;

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeRunner runner) {
            return new Logic(runner, this);
        }
    }
}