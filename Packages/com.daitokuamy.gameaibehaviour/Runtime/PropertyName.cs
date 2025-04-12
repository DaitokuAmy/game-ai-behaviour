using System;

namespace GameAiBehaviour {
    /// <summary>
    /// プロパティ名格納用クラス
    /// </summary>
    [Serializable]
    public abstract class PropertyName<T> {
        public string propertyName = "";

        /// <summary>
        /// 文字列化
        /// </summary>
        public override string ToString() {
            return propertyName;
        }

        /// <summary>
        /// 暗黙型変換
        /// </summary>
        public static implicit operator string(PropertyName<T> propertyName) {
            return propertyName.propertyName;
        }
    }
}