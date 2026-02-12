using System;

namespace Tenkai.SourceLinkedAsset.Domain {

    public static class ResultLinqExtensions {

        private const string DefaultWhereFailReason = "Where predicate returned false.";

        /// <summary>
        /// LINQ の select に対応する。
        /// </summary>
        public static Result<U> Select<T, U>(this Result<T> source, Func<T, U> selector) {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            return source.Map(selector);
        }

        /// <summary>
        /// LINQ の from ... from ... select に対応する。
        /// C# のクエリ式はこのシグネチャの SelectMany を探す。
        /// </summary>
        public static Result<V> SelectMany<T, U, V>(
            this Result<T> source,
            Func<T, Result<U>> binder,
            Func<T, U, V> projector
        ) {
            if (binder == null) throw new ArgumentNullException(nameof(binder));
            if (projector == null) throw new ArgumentNullException(nameof(projector));

            return source.Bind(t =>
                binder(t).Map(u =>
                    projector(t, u)));
        }

        /// <summary>
        /// LINQ の where に対応する。
        /// predicate が false の場合は Fail に落とす。
        /// </summary>
        public static Result<T> Where<T>(this Result<T> source, Func<T, bool> predicate) {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return source.Match(
                ok: v => predicate(v)
                    ? Result<T>.Ok(v)
                    : Result<T>.Fail(DefaultWhereFailReason),
                fail: err => Result<T>.Fail(err)
            );
        }

        /// <summary>
        /// LINQ の where に対応する（Fail 理由を指定）。
        /// </summary>
        public static Result<T> Where<T>(this Result<T> source, Func<T, bool> predicate, Func<T, string> failReasonFactory) {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (failReasonFactory == null) throw new ArgumentNullException(nameof(failReasonFactory));

            return source.Match(
                ok: v => predicate(v)
                    ? Result<T>.Ok(v)
                    : Result<T>.Fail(failReasonFactory(v) ?? DefaultWhereFailReason),
                fail: err => Result<T>.Fail(err)
            );
        }
    }
}
