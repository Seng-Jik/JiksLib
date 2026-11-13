using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    public sealed class DisposableStack : IDisposable
    {
        public DisposableStack()
        {
            stack = new();
        }

        public DisposableStack(int capacity)
        {
            stack = new(capacity);
        }

        public void Add(IDisposable disposable)
        {
            stack.Push(new(disposable));
        }

        public void Add(Action disposeAction)
        {
            stack.Push(new(disposeAction));
        }

        public IDisposable AsDisposable() => this;

        public void Dispose()
        {
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node.Disposable != null)
                {
                    node.DisposeAction?.Invoke();
                }
                else
                {
                    node.Disposable?.Dispose();
                }
            }
        }

        #region Details

        readonly Stack<Node> stack;

        private readonly struct Node
        {
            public readonly IDisposable? Disposable;
            public readonly Action? DisposeAction;

            public Node(IDisposable disposable)
            {
                Disposable = disposable;
                DisposeAction = null;
            }

            public Node(Action disposeAction)
            {
                DisposeAction = disposeAction;
                Disposable = null;
            }
        }

        #endregion
    }
}
