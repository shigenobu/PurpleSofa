using System;

namespace PurpleSofa
{
    public abstract class PsHandler<T> where T : PsState
    {
        protected bool GetState(IAsyncResult result, out T? state)
        {
            state = default;
            if (result.AsyncState != null) state = (T)result.AsyncState;
            return state != null;
        }

        public abstract void Prepare(T state);
        
        public abstract void Complete(IAsyncResult result);

        public abstract void Failed(T state);
        
        public abstract void Shutdown();

    }
}