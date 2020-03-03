using System.Linq;
using TechTalk.SpecFlow;

namespace EventDrivenThinking.Tests.Common
{
    [Binding]
    public class SpecificationContext
    {
        public ISpecificationExecutor Executor { get; }

        public SpecificationContext(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            if (featureContext.FeatureInfo.Tags.Contains("app") || scenarioContext.ScenarioInfo.Tags.Contains("app"))
            {
                // setup executor to startup the app
                Executor = new AppSpecificationExecutor();
            }
            else 
                Executor = new InMemorySpecificationExecutor();
        }

        [AfterScenario()]
        public void Cleanup()
        {
            Executor.Dispose();
        }
    }
}