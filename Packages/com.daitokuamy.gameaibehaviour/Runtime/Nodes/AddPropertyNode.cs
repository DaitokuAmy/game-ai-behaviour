using System.Collections;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// プロパティの値を加算するノード
    /// </summary>
    public abstract class AddPropertyNode<T, TPropertyName> : DecoratorNode
        where TPropertyName : PropertyName<T> {
        [Tooltip("プロパティ名")]
        public TPropertyName propertyName;
        [Tooltip("加算する値")]
        public T value;
        
        /// <summary>説明</summary>
        public override string Description => $"{propertyName} += {value}";
        
        /// <summary>
        /// ロジック基底
        /// </summary>
        protected abstract class LogicBase<TNode> : Logic<TNode>
            where TNode : AddPropertyNode<T, TPropertyName> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            protected LogicBase(IBehaviourTreeRunner runner, TNode node) : base(runner, node) {
            }

            /// <summary>
            /// 実行ルーチン
            /// </summary>
            protected sealed override IEnumerator ExecuteRoutineInternal() {
                // プロパティの加算
                AddPropertyValue(Controller.Blackboard, Node.propertyName, Node.value);
                
                // 子がいない場合は失敗
                if (Node.child == null) {
                    SetState(State.Failure);
                    yield break;
                }

                // 接続先ノードの実行
                yield return ExecuteNodeRoutine(Node.child, SetState);
            }
            
            /// <summary>
            /// プロパティの加算
            /// </summary>
            protected abstract void AddPropertyValue(Blackboard blackboard, string propertyName, T addValue);
        }
    }
}