namespace GameAiBehaviour {
    /// <summary>
    /// 実行ノードハンドリング用インターフェース
    /// </summary>
    public interface IActionNodeHandler {
        /// <summary>
        /// Action状態
        /// </summary>
        public enum State {
            Success,
            Failure,
            Running,
        }

        bool OnEnter(HandleableActionNode node);
        State OnUpdate(HandleableActionNode node);
        void OnExit(HandleableActionNode node);
        void OnReset(HandleableActionNode node);
    }

    /// <summary>
    /// 実行ノードハンドリング用基底クラス
    /// </summary>
    public abstract class ActionNodeHandler<TNode> : IActionNodeHandler
        where TNode : HandleableActionNode {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ActionNodeHandler() {
        }

        bool IActionNodeHandler.OnEnter(HandleableActionNode node) {
            return OnEnterInternal((TNode)node);
        }

        IActionNodeHandler.State IActionNodeHandler.OnUpdate(HandleableActionNode node) {
            return OnUpdateInternal((TNode)node);
        }

        void IActionNodeHandler.OnExit(HandleableActionNode node) {
            OnExitInternal((TNode)node);
        }

        void IActionNodeHandler.OnReset(HandleableActionNode node) {
            OnResetInternal((TNode)node);
        }

        /// <summary>
        /// 実行ノードの開始処理
        /// </summary>
        /// <returns>開始に成功したか</returns>
        protected virtual bool OnEnterInternal(TNode node) {
            return true;
        }

        /// <summary>
        /// 実行ノードの更新処理
        /// </summary>
        /// <returns>更新結果</returns>
        protected virtual IActionNodeHandler.State OnUpdateInternal(TNode node) {
            return IActionNodeHandler.State.Success;
        }

        /// <summary>
        /// 実行ノードの終了処理
        /// </summary>
        protected virtual void OnExitInternal(TNode node) {
        }

        /// <summary>
        /// 思考リセット時の処理
        /// </summary>
        protected virtual void OnResetInternal(TNode node) {
        }
    }
}