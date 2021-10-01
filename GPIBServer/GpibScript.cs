using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GPIBServer
{
    public class GpibScript : ErrorReporterBase
    {
        public GpibScript()
        { }

        public GpibThread[] Threads { get; set; }

        public string Name { get; set; }

        public bool TerminateAllThreadsOnError { get; set; } = false;

        [JsonIgnore]
        public Task<bool>[] ExecutingTasks { get; private set; }

        public bool Execute(Dictionary<string, GpibController> controllers, Dictionary<string, GpibInstrumentCommandSet> instruments,
            CancellationToken cancel)
        {
            //Instantinate tasks
            CancellationTokenSource src = new CancellationTokenSource();
            cancel.Register(() => src.Cancel());
            ExecutingTasks = new Task<bool>[Threads.Length];
            try
            {
                for (int i = 0; i < Threads.Length; i++)
                {
                    ExecutingTasks[i] = new Task<bool>(() => Threads[i].Execute(controllers, instruments), src.Token);
                    Threads[i].ErrorOccured += (o, e) => RaiseError(o, e.Exception, $"{e.Data}, original thread = {Threads[i].Name}");
                }
            }
            catch (Exception ex)
            {
                RaiseError(this, ex, "Can't initialize script tasks.");
                return false;
            }

            //Execute
            try
            {
                for (int i = 0; i < ExecutingTasks.Length; i++)
                {
                    if (ExecutingTasks[i] == null) continue;
                    Thread.Sleep(Threads[i].StartDelay);
                    ExecutingTasks[i].Start();
                }
            }
            catch (Exception ex)
            {
                RaiseError(this, ex, "Can't start all threads.");
                return false;
            }
            while (ExecutingTasks.Any(x => !x.IsCompleted))
            {
                try
                {
                    int i = Task.WaitAny(ExecutingTasks);
                    var t = ExecutingTasks[i];
                    if (!t.IsCompletedSuccessfully || !t.Result)
                    {
                        if (t.Exception != null) RaiseError(this, t.Exception, $"Thread = {Threads[i].Name}.");
                        if (TerminateAllThreadsOnError && !t.IsCanceled) src.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    RaiseError(this, ex, "Error during waiting for all threads.");
                    if (TerminateAllThreadsOnError && !src.IsCancellationRequested) src.Cancel();
                }
            }

            //Clean up
            foreach (var item in ExecutingTasks)
            {
                try
                {
                    item.Dispose();
                }
                catch (Exception)
                { }
            }
            return true;
        }
    }
}
