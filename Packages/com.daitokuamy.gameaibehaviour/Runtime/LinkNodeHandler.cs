namespace GameAiBehaviour
{
    /// <summary>
    /// リンクノードハンドリング用インタフェース
    /// </summary>
    public interface ILinkNodeHandler
    {
        BehaviourTree GetConnectTree(HandleableLinkNode node);
        void OnEnter(HandleableLinkNode node);
        void OnExit(HandleableLinkNode node);
        void OnReset(HandleableLinkNode node);
    }

    /// <summary>
    /// リンクノードハンドリング用基底クラス
    /// </summary>
    public abstract class LinkNodeHandler<TNode> : ILinkNodeHandler where TNode : HandleableLinkNode
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LinkNodeHandler()
        {
        }

        BehaviourTree ILinkNodeHandler.GetConnectTree(HandleableLinkNode node)
        {
            return GetConnectTreeInternal((TNode)node);
        }
        
        /// <summary>
        /// 接続先のBehaviourTreeを取得する
        /// </summary>
        protected virtual BehaviourTree GetConnectTreeInternal(TNode node) => null;

        void ILinkNodeHandler.OnEnter(HandleableLinkNode node)
        {
            OnEnterInternal((TNode)node);
        }

        void ILinkNodeHandler.OnExit(HandleableLinkNode node)
        {
            OnExitInternal((TNode)node);
        }

        void ILinkNodeHandler.OnReset(HandleableLinkNode node) {
            OnResetInternal((TNode)node);
        }

        /// <summary>
        /// 接続先のBehaviourTreeに入る前の処理
        /// </summary>
        protected virtual void OnEnterInternal(TNode node)
        {
        }

        /// <summary>
        /// 接続先のBehaviourTreeから抜けた後の処理
        /// </summary>
        protected virtual void OnExitInternal(TNode node)
        {
        }

        /// <summary>
        /// 思考リセット時の処理
        /// </summary>
        protected virtual void OnResetInternal(TNode node)
        {
        }
    }
}