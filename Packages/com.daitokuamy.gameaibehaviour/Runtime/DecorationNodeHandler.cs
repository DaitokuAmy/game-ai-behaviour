namespace GameAiBehaviour
{
    /// <summary>
    /// デコレーションノードハンドリング用インタフェース
    /// </summary>
    public interface IDecorationNodeHandler
    {
        bool Check(Blackboard blackboard, HandleableDecorationNode node);
    }
    
    /// <summary>
    /// デコレーションノードハンドリング用基底クラス
    /// </summary>
    public abstract class DecorationNodeHandler<TNode> : IDecorationNodeHandler
        where TNode : HandleableDecorationNode
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public DecorationNodeHandler()
        {
        }

        bool IDecorationNodeHandler.Check(Blackboard blackboard, HandleableDecorationNode node)
        {
            return CheckInternal(blackboard, (TNode)node);
        }

        /// <summary>
        /// 条件のチェック処理
        /// </summary>
        protected virtual bool CheckInternal(Blackboard blackboard, TNode node) => true;
    }
}