namespace GameAiBehaviour {
    /// <summary>
    /// Floatプロパティの値を加算するノード
    /// </summary>
    [BehaviourTreeNode("Property/Add Float")]
    public sealed class AddFloatPropertyNode : AddPropertyNode<float, FloatPropertyName> {
        /// <summary>
        /// ロジック基底
        /// </summary>
        private sealed class Logic : LogicBase<AddFloatPropertyNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, AddFloatPropertyNode node) : base(runner, node) {
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected override void AddPropertyValue(Blackboard blackboard, string propertyName, float addValue) {
                blackboard.SetFloat(propertyName, blackboard.GetFloat(propertyName) + addValue);
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