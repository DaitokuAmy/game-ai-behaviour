namespace GameAiBehaviour {
    /// <summary>
    /// Integerプロパティの値を加算するノード
    /// </summary>
    [BehaviourTreeNode("Property/Add Integer")]
    public sealed class AddIntegerPropertyNode : AddPropertyNode<int, IntegerPropertyName> {
        /// <summary>
        /// ロジック基底
        /// </summary>
        private sealed class Logic : LogicBase<AddIntegerPropertyNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, AddIntegerPropertyNode node) : base(runner, node) {
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected override void AddPropertyValue(Blackboard blackboard, string propertyName, int addValue) {
                blackboard.SetInteger(propertyName, blackboard.GetInteger(propertyName) + addValue);
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