using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Logging;

namespace TopshelfHang
{
    class TestWorker : ServiceControl
    {
        private static readonly LogWriter logger = HostLogger.Get<TestWorker>();
        public static bool ShouldStop { get; private set; }
        private ManualResetEvent handle;

        public bool Start(HostControl hostControl)
        {
            logger.Info("Starting test worker...");

            handle = new ManualResetEvent(false);
            logger.Info("Starting worker threads...");

            // start the listenening thread
            _ = DoWork(true, handle);
            return true;
        }

        private static async Task DoWork(bool fail, ManualResetEvent waitEvent)
        {
            await Task.Delay(1000);

            if (fail)
            {
                throw new Exception();
            }
            logger.InfoFormat("Releasing the handle");
            waitEvent.Set();
        }

        public bool Stop(HostControl hostControl)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            ShouldStop = true;
            logger.Info("Stopping test worker...");
            // wait for all threads to finish
            handle.WaitOne();

            return true;
        }
    }

    class Program
    {
        static void Main()
        {
            var config = new LoggingConfiguration();
            config.AddRule(LogLevel.Info, LogLevel.Fatal, new ConsoleTarget(), "TopshelfHang.TestWorker");

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            HostFactory.Run(hc => {
                hc.UseNLog(new LogFactory(config));
                // service is constructed using its default constructor
                hc.Service<TestWorker>();
                // sets service properties
                hc.SetServiceName(typeof(TestWorker).Namespace);
                hc.SetDisplayName(typeof(TestWorker).Namespace);
                hc.SetDescription("Test worker");
            });
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine($"{nameof(TaskScheduler_UnobservedTaskException)}: {e.Exception}");
        }
    }
}
