namespace GameAiBehaviour {
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
        /// ノードにBindされているHandlerの取得
        /// </summary>
        IActionNodeHandler GetActionHandler(HandleableActionNode node);

        /// <summary>
        /// ノードにBindされているHandlerの取得
        /// </summary>
        IDecorationNodeHandler GetDecoratorHandler(HandleableDecorationNode node);

        /// <summary>
        /// ノードにBindされているHandlerの取得
        /// </summary>
        ILinkNodeHandler GetLinkHandler(HandleableLinkNode node);

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