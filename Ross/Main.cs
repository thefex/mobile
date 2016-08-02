using System.Diagnostics;
using Toggl.Phoebe.Analytics;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross
{
    public class Application
    {
        private static Stopwatch startTimeMeasure;

        static void Main(string[] args)
        {
            startTimeMeasure = Stopwatch.StartNew();

            UIApplication.Main(args, null, "AppDelegate");
        }

        public static void MarkLaunched()
        {
            if (!startTimeMeasure.IsRunning) return;

            startTimeMeasure.Stop();
            ServiceContainer.Resolve<ITracker> ().SendAppInitTime(startTimeMeasure.Elapsed);
        }
    }
}
