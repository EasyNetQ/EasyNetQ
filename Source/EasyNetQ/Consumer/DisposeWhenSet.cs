using System;

namespace EasyNetQ.Consumer
{
    class DisposeWhenSet
    {
        private bool _disposed = false;
        private IDisposable _disposable;
        
        public IDisposable Disposable
        {
            private get { return _disposable; }
            set { 
                _disposable = value; 
                if (_disposed)
                {
                    _disposable.Dispose();
                }
            }
        }

        public void DisposeObject()
        {
            if (Disposable != null)
            {
                Disposable.Dispose();
            }
            _disposed = false;
        }
    }
}
