using EventDrivenThinking.Logging;
using Serilog.Core;
using TechTalk.SpecFlow;

namespace EventDrivenThinking.Tests.Common
{
    [Binding]
    public class GlobalHooks
    {
        [BeforeTestRun]
        public static void Init()
        {
            LoggerFactory.Init(Logger.None);
        }
    }
}