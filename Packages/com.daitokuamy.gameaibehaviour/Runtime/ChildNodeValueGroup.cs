using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameAiBehaviour {
    /// <summary>
    /// 子ノードに値を付与するための基底クラス
    /// </summary>
    [Serializable]
    public abstract class ChildNodeValueGroup<T> : IEnumerable<T> {
        [SerializeField]
        private List<T> _values = new();

        /// <summary>インデクサ</summary>
        public T this[int index] {
            get {
                if (index < 0 || index >= _values.Count) {
                    return default;
                }

                return _values[index];
            }
        }

        /// <summary>要素数</summary>
        public int Count => _values.Count;
        
        /// <summary>
        /// IEnumeratorの返却
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            for (var i = 0; i < _values.Count; i++) {
                yield return _values[i];
            }
        }

        /// <summary>
        /// IEnumeratorの返却
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() {
            for (var i = 0; i < _values.Count; i++) {
                yield return _values[i];
            }
        }

        /// <summary>
        /// 値の設定
        /// </summary>
        public void Set(int index, T value) {
            while (_values.Count <= index) {
                _values.Add(default);
            }

            _values[index] = value;
        }

        /// <summary>
        /// 要素数を変更する
        /// </summary>
        public void SetCount(int count) {
            while (_values.Count < count) {
                _values.Add(default);
            }

            if (_values.Count > count) {
                _values.RemoveRange(count, _values.Count - count);
            }
        }
    }

    [Serializable]
    public class FloatChildNodeValueGroup : ChildNodeValueGroup<float> {
    }
}