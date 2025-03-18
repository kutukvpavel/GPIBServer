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
        #region Static

        public static string DevicePathDelimeter { get; set; }
        public static int ControllerPollInterval { get; set; }
        public static string DelayCommandPrefix { get; set; }
        public static string VariablePrefix { get; set; }

        #endregion

        public GpibScript()
        { }


        #region Properties

        public string Name { get; set; } = "ExampleScript";

        public bool TerminateAllThreadsOnError { get; set; } = false;
        public GpibThread[] Threads { get; set; }

        [JsonIgnore]
        public Task<bool>[] ExecutingTasks { get; private set; }

        #endregion

        #region Public Methods

        public bool ValidateNames()
        {
            return Threads.Select(x => x.Name).Distinct().Count() == Threads.Length;
        }

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
                    var r = Threads[i];
                    r.Cancel = src.Token;
                    ExecutingTasks[i] = new Task<bool>(() => r.Execute(controllers, instruments), src.Token);
                    r.ErrorOccured += (o, e) => RaiseError(o, e.Exception, $"{e.Data}, original thread = {r.Name}");
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
            bool success = true;
            while (ExecutingTasks.Any(x => !x.IsCompleted))
            {
                if (src.IsCancellationRequested)
                {
                    Thread.Sleep(10);
                    continue;
                }
                try
                {
                    int i = Task.WaitAny(ExecutingTasks, src.Token);
                    var t = ExecutingTasks[i];
                    if (!t.IsCompletedSuccessfully || !t.Result)
                    {
                        success = false;
                        if (t.Exception != null) RaiseError(this, t.Exception, $"Thread = {Threads[i].Name}.");
                        if (TerminateAllThreadsOnError && !t.IsCanceled) src.Cancel();
                    }
                }
                catch (OperationCanceledException)
                { }
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
            return success && !src.IsCancellationRequested;
        }

        public IEnumerable<string> GetRequiredControllerNames()
        {
            List<string> names = new List<string>();
            foreach (var item in Threads)
            {
                names.AddRange(item.Commands.Where(x => !(x.StartsWith(DelayCommandPrefix) || x.StartsWith(VariablePrefix)))
                    .Select(x => x.Split(DevicePathDelimeter))
                    .Where(x => x.Length > 1).Select(x => x[0]));
            }
            return names.Distinct();
        }

        #endregion
    }
}
