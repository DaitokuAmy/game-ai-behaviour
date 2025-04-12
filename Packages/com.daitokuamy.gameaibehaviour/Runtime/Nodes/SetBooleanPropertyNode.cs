namespace GameAiBehaviour {
    /// <summary>
    /// Booleanプロパティの値を変更するノード
    /// </summary>
    [BehaviourTreeNode("Property/Set Boolean")]
    public sealed class SetBooleanPropertyNode : SetPropertyNode<bool, BooleanValueObject, BooleanPropertyName> {
        /// <summary>
        /// ロジック基底
        /// </summary>
        private sealed class Logic : LogicBase<SetBooleanPropertyNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, SetBooleanPropertyNode node) : base(runner, node) {
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected override void SetPropertyValue(Blackboard blackboard, string propertyName, bool value) {
                blackboard.SetBoolean(propertyName, value);
            }

            /// <summary>
            /// プロパティの取得
            /// </summary>
            protected override bool GetPropertyValue(Blackboard blackboard, string propertyName, bool defaultValue) {
                return blackboard.GetBoolean(propertyName, defaultValue);
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