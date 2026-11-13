using System;

namespace JiksLib.Control
{
    public static class Disposable
    {
        public static IDisposable Empty => empty ??= new();

        public static IDisposable FromAction(Action disposeAction) =>
            new ActionDisposable(disposeAction);

        #region Details

        static EmptyDisposable? empty = null;

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private sealed class ActionDisposable : IDisposable
        {
            public ActionDisposable(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                disposeAction();
            }

            readonly Action disposeAction;
        }

        #endregion
    }
}