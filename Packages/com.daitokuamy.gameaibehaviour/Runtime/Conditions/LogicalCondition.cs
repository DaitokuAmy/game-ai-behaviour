﻿using System;
using System.Linq;

namespace GameAiBehaviour {
    /// <summary>
    /// 論理比較用条件
    /// </summary>
    public abstract class LogicalCondition<T, TValueObject> : ValueCondition<T, TValueObject>
        where T : IComparable
        where TValueObject : ValueObject<T> {
        // 演算子
        public OperatorType operatorType = OperatorType.Equal;

        public override string ConditionTitle => $"{leftValue} {operatorType.GetMark()} {rightValue}";

        /// <summary>
        /// 値の判定
        /// </summary>
        protected sealed override bool CheckInternal(T left, T right) {
            return operatorType.Check(left, right);
        }

        /// <summary>
        /// オペレータタイプの表示名
        /// </summary>
        protected override string[] GetOperatorTypeLabels() {
            return ((OperatorType[])Enum.GetValues(typeof(OperatorType)))
                .Select(x => x.GetMark()).ToArray();
        }
    }
}