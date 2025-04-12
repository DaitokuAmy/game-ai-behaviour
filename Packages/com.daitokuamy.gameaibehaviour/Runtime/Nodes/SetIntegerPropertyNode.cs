namespace GameAiBehaviour {
    /// <summary>
    /// Integerプロパティの値を変更するノード
    /// </summary>
    [BehaviourTreeNode("Set Property/Set Integer")]
    public sealed class SetIntegerPropertyNode : SetPropertyNode<int, IntegerValueObject, IntegerPropertyName> {
        /// <summary>
        /// ロジック基底
        /// </summary>
        private sealed class Logic : LogicBase<SetIntegerPropertyNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, SetIntegerPropertyNode node) : base(runner, node) {
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected override void SetPropertyValue(Blackboard blackboard, string propertyName, int value) {
                blackboard.SetInteger(propertyName, value);
            }

            /// <summary>
            /// プロパティの取得
            /// </summary>
            protected override int GetPropertyValue(Blackboard blackboard, string propertyName, int defaultValue) {
                return blackboard.GetInteger(propertyName, defaultValue);
            }
        }

        /// <summary>
        /// ロジックの生成
        /// </summary>
        public override ILogic CreateLogic(IBehaviourTreeRunner runner) {
            return new Logic(runner, this);
        }
    }
}