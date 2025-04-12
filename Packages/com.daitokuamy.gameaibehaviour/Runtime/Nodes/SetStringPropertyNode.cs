namespace GameAiBehaviour {
    /// <summary>
    /// Stringプロパティの値を変更するノード
    /// </summary>
    [BehaviourTreeNode("Set Property/Set String")]
    public sealed class SetStringPropertyNode : SetPropertyNode<string, StringValueObject, StringPropertyName> {
        /// <summary>
        /// ロジック基底
        /// </summary>
        private sealed class Logic : LogicBase<SetStringPropertyNode> {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Logic(IBehaviourTreeRunner runner, SetStringPropertyNode node) : base(runner, node) {
            }

            /// <summary>
            /// プロパティの設定
            /// </summary>
            protected override void SetPropertyValue(Blackboard blackboard, string propertyName, string value) {
                blackboard.SetString(propertyName, value);
            }

            /// <summary>
            /// プロパティの取得
            /// </summary>
            protected override string GetPropertyValue(Blackboard blackboard, string propertyName, string defaultValue) {
                return blackboard.GetString(propertyName, defaultValue);
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