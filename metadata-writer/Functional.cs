namespace Functional
{
    public static class Functional
    {
        /// <summary>
        /// Passes the value on the left of the `.PipeTo` to the function on the right
        /// </summary>
        /// <typeparam name="T">Type of the value to pass to the function</typeparam>
        /// <typeparam name="R">Type of the value returned from the function</typeparam>
        /// <param name="value">The value to pass to the function</param>
        /// <param name="f">The function</param>
        /// <returns></returns>
        public static R PipeTo<T, R>(this T value, Func<T, R> f) =>
            f(value);

        /// <summary>
        /// Passes the value on the left of the `.PipeTo` to the action on the right
        /// </summary>
        /// <typeparam name="T">Type of the value to pass to the action</typeparam>
        /// <param name="value">The value to pass to the action</param>
        /// <param name="f">The action</param>
        public static void PipeTo<T>(this T value, Action<T> f) =>
            f(value);
    }
}