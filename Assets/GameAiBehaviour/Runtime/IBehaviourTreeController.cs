﻿namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTree制御用のインターフェース
    /// </summary>
    public interface IBehaviourTreeController {
        /// <summary>
        /// Blackboard
        /// </summary>
        Blackboard Blackboard { get; }
        
        /// <summary>
        /// ノードの実行
        /// </summary>
        Node.State UpdateNode(Node.ILogic parentNodeLogic, Node node);

        /// <summary>
        /// ノードにBindされているHandlerの取得
        /// </summary>
        IActionNodeHandler GetActionHandler(HandleableActionNode node);
    }
}