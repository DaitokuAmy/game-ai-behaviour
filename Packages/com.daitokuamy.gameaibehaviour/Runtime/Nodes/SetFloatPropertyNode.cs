namespace GameAiBehaviour {
    /// <summary>
    /// Floatプロパティの値を変更するノード
    /// </summary>
    [BehaviourTreeNode("Property/Set Float")]
    public sealed class SetFloatPropertyNode : SetPropertyNode<float, FloatValueObject, FloatPropertyName> {
        /// <summary>
        /// ロジック基底
        /// </summary>
        private sealed class Logic : LogicBase<SetFloatPropertyNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, SetFloatPropertyNode node) : base(runner, node) {
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected override void SetPropertyValue(Blackboard blackboard, string propertyName, float value) {
                blackboard.SetFloat(propertyName, value);
            }

            /// <summary>
            /// プロパティの取得
            /// </summary>
            protected override float GetPropertyValue(Blackboard blackboard, string propertyName, float defaultValue) {
                return blackboard.GetFloat(propertyName, defaultValue);
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