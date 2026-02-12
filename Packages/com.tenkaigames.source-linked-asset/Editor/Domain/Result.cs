using System;
using System.Collections.Generic;

namespace Tenkai.SourceLinkedAsset.Domain {

    #region Extension
    /// <summary>
    /// Extension methods for Result types.
    /// Note: These methods assume the source results are initialized (non-default).
    /// </summary>
    public static class ResultExtensions {
        public static Result<T> ToOk<T>(this T value)
            => Result<T>.Ok(value);

        public static Result WithoutValue<T>(this Result<T> result) =>
            result.Match(
                ok: _ => Result.Ok,
                fail: fail => Result.Fail(fail));

        // === Flatten
        // Result<Result> -> Result
        public static Result Flatten(this Result<Result> source) =>
            source.Match(
                ok: inner => inner,
                fail: err => Result.Fail(err)
            );

        // Result<Result<T>> -> Result<T>
        public static Result<T> Flatten<T>(this Result<Result<T>> source) =>
            source.Match(
                ok: inner => inner,
                fail: err => Result<T>.Fail(err)
            );
    }
    #endregion



    #region NonGeneric
    // ============================================================
    // Result (non-generic) : Uninitialized / Ok / Fail(string)
    // ============================================================

    /// <summary>
    /// Represents the result of an operation: Ok or Fail (with a string reason).
    ///
    /// IMPORTANT:
    /// This is a <c>readonly struct</c>. The default value (<c>default(Result)</c>) is NOT a valid state.
    /// If <c>default</c> is used (uninitialized state), most operations throw <see cref="InvalidOperationException"/>.
    /// Always create instances via <see cref="Ok"/> or <see cref="Fail(string)"/>.
    /// </summary>
    public readonly struct Result {
        private const byte StateUninitialized = 0;
        private const byte StateOk = 1;
        private const byte StateFail = 2;

        private readonly byte _state;
        private readonly string _failReason;

        public readonly string FailReason => _failReason;

        public bool IsInitialized => _state != StateUninitialized;
        public bool IsOk => _state == StateOk;
        public bool IsFail => _state == StateFail;

        private Result(byte state, string failReason) {
            _state = state;
            _failReason = failReason;
        }

        public static Result Ok => new(StateOk, null);

        public static Result Fail(string failReason) =>
            failReason != null
                ? new(StateFail, failReason)
                : throw new ArgumentNullException(nameof(failReason));

        /// <summary>
        /// Tries to get the fail reason. Throws if uninitialized.
        /// </summary>
        public bool TryGetFailReason(out string failReason) {
            EnsureInitialized();
            failReason = IsFail ? _failReason : default;
            return IsFail;
        }

        public string GetFailOrThrow() {
            EnsureInitialized();

            return IsFail
                ? _failReason
                : throw new InvalidOperationException("Result is Ok");
        }

        public U Match<U>(Func<U> ok, Func<string, U> fail) {
            EnsureInitialized();
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (fail == null) throw new ArgumentNullException(nameof(fail));

            return IsOk
                ? ok()
                : fail(_failReason);
        }

        public Result Inspect(Action ok, Action<string> fail) {
            EnsureInitialized();

            if (IsOk)
                ok?.Invoke();
            else
                fail?.Invoke(_failReason);

            return this;
        }

        public void TapFail(Action<string> fail) {
            EnsureInitialized();

            if (IsFail)
                fail?.Invoke(_failReason);
        }

        public Result Bind(Func<Result> next) {
            EnsureInitialized();
            if (next == null) throw new ArgumentNullException(nameof(next));

            return IsOk
                ? next()
                : Fail(_failReason);
        }

        public Result<T> Bind<T>(Func<Result<T>> next) {
            EnsureInitialized();
            if (next == null) throw new ArgumentNullException(nameof(next));

            return IsOk
                ? next()
                : Result<T>.Fail(_failReason);
        }

        public override string ToString() =>
            _state switch {
                StateUninitialized => "Uninitialized",
                StateOk => "Ok",
                StateFail => $"Fail({_failReason})",
                _ => "InvalidState"
            };

        private void EnsureInitialized() {
            if (!IsInitialized)
                throw new InvalidOperationException("Result is uninitialized (default(Result) was used).");
        }

        /// <summary>
        /// Throws if this result is uninitialized (i.e., default was used).
        /// Use this to assert invariants at API boundaries.
        /// </summary>
        public void AssertInitialized() => EnsureInitialized();
    }
    #endregion



    #region Result<T>
    // ============================================================
    // Result<T> : Uninitialized / Ok(T) / Fail(string)
    // ============================================================

    /// <summary>
    /// Represents the result of an operation that may succeed with a value of type <typeparamref name="T"/>,
    /// or fail with a string reason.
    ///
    /// IMPORTANT:
    /// This is a <c>readonly struct</c>. The default value (<c>default(Result&lt;T&gt;)</c>) is NOT a valid state.
    /// If <c>default</c> is used (uninitialized state), most operations throw <see cref="InvalidOperationException"/>.
    /// Always create instances via <see cref="Ok(T)"/> or <see cref="Fail(string)"/>.
    /// </summary>
    public readonly struct Result<T> {
        private const byte StateUninitialized = 0;
        private const byte StateOk = 1;
        private const byte StateFail = 2;

        private readonly byte _state;
        private readonly T _value;
        private readonly string _failReason;

        public readonly string FailReason => _failReason;

        public bool IsInitialized => _state != StateUninitialized;
        public bool IsOk => _state == StateOk;
        public bool IsFail => _state == StateFail;

        private Result(byte state, T value, string failReason) {
            _state = state;
            _value = value;
            _failReason = failReason;
        }

        public static Result<T> Ok(T value) => new(StateOk, value, null);

        public static Result<T> Fail(string failReason) =>
            failReason != null
                ? new(StateFail, default, failReason)
                : throw new ArgumentNullException(nameof(failReason));

        /// <summary>
        /// Tries to get the Ok value. Throws if uninitialized.
        /// </summary>
        public bool TryGetOk(out T value) {
            EnsureInitialized();
            value = IsOk ? _value : default;
            return IsOk;
        }

        /// <summary>
        /// Tries to get the fail reason. Throws if uninitialized.
        /// </summary>
        public bool TryGetFailReason(out string failReason) {
            EnsureInitialized();
            failReason = IsFail ? _failReason : default;
            return IsFail;
        }

        public T GetOr(T defaultValue) {
            EnsureInitialized();
            return IsOk ? _value : defaultValue;
        }

        public T GetOrThrow() {
            EnsureInitialized();

            return IsOk
                ? _value
                : throw new InvalidOperationException($"Result is Fail: {_failReason}");
        }

        public bool Contains(T value) {
            EnsureInitialized();
            return IsOk && EqualityComparer<T>.Default.Equals(_value, value);
        }

        public bool Contains(T value, IEqualityComparer<T> comparer) {
            EnsureInitialized();
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            return IsOk && comparer.Equals(_value, value);
        }

        public U Match<U>(Func<T, U> ok, Func<string, U> fail) {
            EnsureInitialized();
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (fail == null) throw new ArgumentNullException(nameof(fail));

            return IsOk
                ? ok(_value)
                : fail(_failReason);
        }

        public Result<T> Inspect(Action<T> ok, Action<string> fail) {
            EnsureInitialized();
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (fail == null) throw new ArgumentNullException(nameof(fail));

            if (IsOk)
                ok(_value);
            else
                fail(_failReason);

            return this;
        }

        public Result<T> Tap(Action<T> ok) {
            EnsureInitialized();

            if (IsOk) ok?.Invoke(_value);
            return this;
        }

        public Result<T> TapFail(Action<string> fail) {
            EnsureInitialized();

            if (IsFail) fail?.Invoke(_failReason);
            return this;
        }

        public Result<U> Map<U>(Func<T, U> selector) {
            EnsureInitialized();
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return IsOk
                ? Result<U>.Ok(selector(_value))
                : Result<U>.Fail(_failReason);
        }

        public Result<U> Bind<U>(Func<T, Result<U>> binder) {
            EnsureInitialized();
            if (binder == null) throw new ArgumentNullException(nameof(binder));

            return IsOk
                ? binder(_value)
                : Result<U>.Fail(_failReason);
        }

        public override string ToString() =>
            _state switch {
                StateUninitialized => "Uninitialized",
                StateOk => $"Ok({_value})",
                StateFail => $"Fail({_failReason})",
                _ => "InvalidState"
            };

        private void EnsureInitialized() {
            if (!IsInitialized)
                throw new InvalidOperationException("Result<T> is uninitialized (default(Result<T>) was used).");
        }

        /// <summary>
        /// Throws if this result is uninitialized (i.e., default was used).
        /// Use this to assert invariants at API boundaries.
        /// </summary>
        public void AssertInitialized() => EnsureInitialized();
    }
    #endregion
}
