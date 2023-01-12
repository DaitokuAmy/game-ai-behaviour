﻿using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// ログ出力ノード
    /// </summary>
    public sealed class LogNode : ActionNode {
        private class Logic : Logic<LogNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeController controller, LogNode node) : base(controller, node) {
            }
            
            /// <summary>
            /// 実行処理
            /// </summary>
            protected override State OnUpdate() {
                Debug.unityLogger.Log(Node.logType, Node.text);
                return State.Success;
            }

            /// <summary>
            /// 子要素実行通知
            /// </summary>
            protected override State OnUpdatedChild(ILogic childNodeLogic) {
                return State.Failure;
            }
        }

        [Tooltip("ログモード")]
        public LogType logType = LogType.Log;
        [Tooltip("出力内容のログ")]
        public string text = "";

        public override string Description => $"[{logType}]{text}";

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeController controller) {
            return new Logic(controller, this);
        }
    }
}