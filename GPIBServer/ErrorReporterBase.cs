using System;

namespace GPIBServer
{
    public abstract class ErrorReporterBase
    {
        public event EventHandler<ExceptionEventArgs> ErrorOccured;

        protected void RaiseError(object sender, ExceptionEventArgs e)
        {
            ErrorOccured?.Invoke(sender, e);
        }
        protected void RaiseError(object sender, Exception ex, object data = null)
        {
            RaiseError(sender, new ExceptionEventArgs(ex, data));
        }
    }
}
