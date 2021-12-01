using System;

namespace PurpleSofa
{
    /// <summary>
    ///     Handler.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PsHandler<T> where T : PsState
    {
        /// <summary>
        ///     Get state.
        /// </summary>
        /// <param name="result">async result</param>
        /// <param name="state">state</param>
        /// <returns>if cast is success, return true</returns>
        protected bool GetState(IAsyncResult result, out T? state)
        {
            state = default;
            if (result.AsyncState != null) state = (T)result.AsyncState;
            return state != null;
        }

        /// <summary>
        ///     Prepare (Accept, Connect or Read)
        /// </summary>
        /// <param name="state">state</param>
        public abstract void Prepare(T state);
        
        /// <summary>
        ///     Complete (Accept, Connect or Read)
        /// </summary>
        /// <param name="result"></param>
        public abstract void Complete(IAsyncResult result);

        /// <summary>
        ///     Failed.
        /// </summary>
        /// <param name="state">state</param>
        public abstract void Failed(T state);
        
        /// <summary>
        ///     Shutdown.
        /// </summary>
        public abstract void Shutdown();
    }
}