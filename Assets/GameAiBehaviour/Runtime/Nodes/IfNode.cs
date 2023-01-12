﻿using System.Linq;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// 条件判定ノード
    /// </summary>
    public sealed class IfNode : DecoratorNode {
        private class Logic : Logic<IfNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeController controller, IfNode node) : base(controller, node) {
            }

            /// <summary>
            /// 実行処理
            /// </summary>
            protected override State OnUpdate() {
                if (Node.child == null) {
                    return State.Failure;
                }
                
                // 条件判定
                if (!Node.conditions.Check(Controller.Blackboard)) {
                    return State.Failure;
                }

                // 接続先ノードの実行
                UpdateNode(Node.child);

                return State;
            }

            /// <summary>
            /// 子要素の更新結果通知
            /// </summary>
            protected override State OnUpdatedChild(ILogic childNodeLogic) {
                return childNodeLogic.State;
            }
        }
        
        [Tooltip("条件")]
        public ConditionGroup conditions = new ConditionGroup();

        public override string Description => string.Join("\n", conditions.conditions.Select(x => x.ConditionTitle));

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeController controller) {
            return new Logic(controller, this);
        }
    }
}