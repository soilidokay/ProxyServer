using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Core.ProxyServer.DisposePattern
{
    public class DerivedClass : BaseClass
    {
        // Flag: Has Dispose already been called?
        public bool disposed { get; set; } = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        protected virtual void DisposeManaged() { }
        protected virtual void DisposeUnManaged() { }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
                DisposeManaged();
            }

            // Free any unmanaged objects here.
            //
            DisposeUnManaged();
            disposed = true;
            // Call base class implementation.

            base.Dispose(disposing);
        }
    }
}
