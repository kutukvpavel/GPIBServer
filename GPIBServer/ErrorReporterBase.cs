using System;
using System.Collections.Generic;
using System.Text;

namespace GPIBServer
{
    public abstract class ErrorReporterBase
    {
        public event EventHandler<ExceptionEventArgs> ErrorOccured;

        protected void RaiseError(Exception ex, object data = null)
        {
            ErrorOccured?.Invoke(this, new ExceptionEventArgs(ex, data));
        }
    }
}
