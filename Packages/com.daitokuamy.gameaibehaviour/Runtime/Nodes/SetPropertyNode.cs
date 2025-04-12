using System.Collections;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// プロパティの値を変更するノード
    /// </summary>
    public abstract class SetPropertyNode<T, TValueObject, TPropertyName> : DecoratorNode
        where TValueObject : ValueObject<T>
        where TPropertyName : PropertyName<T> {
        [Tooltip("プロパティ名")]
        public TPropertyName propertyName;
        [Tooltip("変更する値")]
        public TValueObject value;
        
        /// <summary>
        /// ロジック基底
        /// </summary>
        protected abstract class LogicBase<TNode> : Logic<TNode>
            where TNode : SetPropertyNode<T, TValueObject, TPropertyName> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            protected LogicBase(IBehaviourTreeRunner runner, TNode node) : base(runner, node) {
            }

            /// <summary>
            /// 実行ルーチン
            /// </summary>
            protected sealed override IEnumerator ExecuteRoutineInternal() {
                // プロパティの設定
                SetPropertyValue(Controller.Blackboard, Node.propertyName, GetValue(Controller.Blackboard, Node.value));
                
                // 子がいない場合は失敗
                if (Node.child == null) {
                    SetState(State.Failure);
                    yield break;
                }

                // 接続先ノードの実行
                yield return ExecuteNodeRoutine(Node.child, SetState);
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected abstract void SetPropertyValue(Blackboard blackboard, string propertyName, T value);

            /// <summary>
            /// プロパティの取得
            /// </summary>
            protected abstract T GetPropertyValue(Blackboard blackboard, string propertyName, T defaultValue);

            /// <summary>
            /// 値の取得
            /// </summary>
            private T GetValue(Blackboard blackboard, TValueObject valueObject) {
                if (string.IsNullOrEmpty(valueObject.propertyName) || !valueObject.useProperty) {
                    return valueObject.constValue;
                }

                return GetPropertyValue(blackboard, valueObject.propertyName, valueObject.constValue);
            }
        }
    }
}