﻿using System;
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
            public Logic(IBehaviourTreeController controller, WhileNode node) : base(controller, node) {
            }

            /// <summary>
            /// 実行処理
            /// </summary>
            protected override State OnUpdate(float deltaTime, bool back) {
                if (Node.child == null) {
                    return State.Failure;
                }

                // 戻り実行の際は実行中として終わる
                if (back) {
                    return State.Running;
                }
                
                // 条件判定
                if (!Node.conditions.Check(Controller.Blackboard)) {
                    return State.Failure;
                }

                // 接続先ノードの実行
                var state = UpdateNode(Node.child, deltaTime);
                
                // 接続先が失敗していたらNodeを失敗とする
                if (state == State.Failure) {
                    return State.Failure;
                }

                return State.Running;
            }
        }
        
        [Tooltip("条件")]
        public ConditionGroup conditions;

        public override string Description => string.Join("\n", conditions.conditions.Select(x => x.ConditionTitle));

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeController controller) {
            return new Logic(controller, this);
        }
    }
}