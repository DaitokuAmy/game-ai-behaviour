﻿namespace GameAiBehaviour {
    /// <summary>
    /// BehaviourTree制御用のインターフェース
    /// </summary>
    public interface IBehaviourTreeController {
        /// <summary>
        /// 思考実行してからの経過時間
        /// </summary>
        float ThinkTime { get; }

        /// <summary>
        /// Blackboard
        /// </summary>
        Blackboard Blackboard { get; }

        /// <summary>
        /// ActionNodeHandlerの生成
        /// </summary>
        IActionNodeHandler CreateActionNodeHandler(HandleableActionNode node);

        /// <summary>
        /// ノードにBindされているLinkNodeHandlerの取得
        /// </summary>
        ILinkNodeHandler GetLinkNodeHandler(HandleableLinkNode node);

        /// <summary>
        /// ノードにBindされているConditionHandlerの取得
        /// </summary>
        IConditionHandler GetConditionHandler(HandleableCondition condition);

        /// <summary>
        /// 思考リセット
        /// </summary>
        void ResetThink();

        /// <summary>
        /// TickTimerのリセット
        /// </summary>
        void ResetTickTimer();

        /// <summary>
        /// サブルーチンの設定
        /// </summary>
        void SetSubRoutine(Node.ILogic parent, Node startNode);

        /// <summary>
        /// サブルーチンの削除
        /// </summary>
        void ResetSubRoutine(Node.ILogic parent);
    }
}