using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Example.Model.Domain.Hotel;
using FluentAssertions;
using TechTalk.SpecFlow;
using Xunit;

namespace EventDrivenThinking.Tests.Common
{
    [Binding]
    public class GenericSteps
    {
        private static readonly EventDictionary Dictionary = new EventDictionary(typeof(HotelAggregate).Assembly);


        private readonly ISpecificationExecutor _specificationExecutor;
        private readonly FeatureContext _featureContext;
        private readonly Dictionary<string, Guid> _defaultCatalogIdentifiers;

        public GenericSteps(FeatureContext featureContext, SpecificationContext specContext)
        {
            _featureContext = featureContext;
            _defaultCatalogIdentifiers = new Dictionary<string, Guid>();
            _specificationExecutor = specContext.Executor;

            _specificationExecutor.Init(Dictionary.AggregateSchemaRegister)
                .Init(Dictionary.ProjectionSchemaRegister)
                .Init(Dictionary.QuerySchemaRegister);
        }
        [Then(@"I get query results:")]
        public async Task ThenIGetQueryResults(Table table)
        {
            Debug.WriteLine("Waiting.......");
            
            var lastResult = _specificationExecutor.GetQueryResults().Last();
            var resultType = lastResult.Result.GetType();
            var deserialized = table.Deserialize(resultType);
            DateTime deadline = DateTime.Now.AddSeconds(10);
            Exception inner = null;
            while (DateTime.Now < deadline)
            {
                try
                {
                    lastResult.Result.Should().BeEquivalentTo(deserialized);
                    break;
                }
                catch (Exception ex)
                {
                    inner = ex;
                    await Task.Delay(200);
                }
            }

            if (inner != null) throw inner;
        }


        [Given(@"The fact: (?!.*\bof\b)(.+):")]
        [Given(@"Fact happened: (?!.*\bof\b)(.+):")]
        public async Task GivenFactHappened(string eventName, Table propertyTable)
        {
            string category = _featureContext.FeatureInfo.Title;
            var evType = Dictionary.FindEvent($"{category} {eventName}");
            var ev = (IEvent)propertyTable.Deserialize(evType);

            var metadata = Dictionary.AggregateSchemaRegister.FindAggregateByEvent(evType);
            var aggregateId = GetAggregateId(metadata);

            await _specificationExecutor.AppendFact(aggregateId, ev);
        }
        [Given(@"The fact: (.+) of the (.+):")]
        [Given(@"The fact: (.+) of a (.+):")]
        public async Task GivenFactOfAnAggregateHappened(string eventName, string newId, Table propertyTable)
        {
            string category = _featureContext.FeatureInfo.Title;
            var evType = Dictionary.FindEvent($"{category} {eventName}");
            var ev = (IEvent)propertyTable.Deserialize(evType);
            var id = newId.ToGuid();

            await _specificationExecutor.AppendFact(id, ev);
        }
        [Given(@"The context of a catalog (.*) named (.*)")]
        public void GivenTheContextOfACatalogNamed(string catalog, string id)
        {
            _defaultCatalogIdentifiers.Add(catalog, id.ToGuid());
        }

        private Guid GetAggregateId(IAggregateSchema aggregateType)
        {
            if (!_defaultCatalogIdentifiers.TryGetValue(aggregateType.Category, out Guid aggregateId))
            {
                aggregateId = Guid.NewGuid();
                _defaultCatalogIdentifiers.Add(aggregateType.Category, aggregateId);
            }

            return aggregateId;
        }
        private Guid GetAggregateId(IClientCommandSchema commandSchema)
        {
            if (!_defaultCatalogIdentifiers.TryGetValue(commandSchema.Category, out Guid aggregateId))
            {
                aggregateId = Guid.NewGuid();
                _defaultCatalogIdentifiers.Add(commandSchema.Category, aggregateId);
            }

            return aggregateId;
        }

        [When(@"I demand: (?!.*\bof\b)(.+):")]
        [When(@"I (?!.*\bof\b)(.+):")]
        public async Task WhenIDemand(string commandType, Table propertyTable)
        {
            string category = _featureContext.FeatureInfo.Title;
            var (cmdType, metadata) = Dictionary.FindCommand($"{category} {commandType}");
            var cmd = (ICommand) propertyTable.Deserialize(cmdType);

            var aggregateId = GetAggregateId(metadata);

            await _specificationExecutor.ExecuteCommand(metadata, aggregateId, cmd);
        }

        [When(@"I query for (.+):")]
        public async Task WhenIQuery(string queryTypeName, Table propertyTable)
        {
            string category = _featureContext.FeatureInfo.Title;
            var queryType = Dictionary.FindQuery($"{category} {queryTypeName}");
            var query = (IQuery)propertyTable.Deserialize(queryType);

            await _specificationExecutor.ExecuteQuery(query);
        }
        //[When(@"I (.*) of (.*):")]
        [When(@"I (?!.*\bquery\b)(.+) of (.*):")]
        [When(@"I (.*) with (.*):")]
        public async Task WhenI(string commandType, string id, Table propertyTable)
        {
            
            string category = _featureContext.FeatureInfo.Title;
            var (cmdType, metadata) = Dictionary.FindCommand($"{category} {commandType}");
            var cmd = (ICommand)propertyTable.Deserialize(cmdType);

            var aggregateId = id.ToGuid();

            await _specificationExecutor.ExecuteCommand(metadata, aggregateId, cmd);
        }

        //[When(@"I (.*) of (.*)")]
        [When(@"I (?!.*\bquery\b)(.+) of (.*)")]
        [When(@"I (.*) with (.*)")]
        public async Task WhenI(string commandType, string id)
        {
            string category = _featureContext.FeatureInfo.Title;
            var (cmdType, metadata) = Dictionary.FindCommand($"{category} {commandType}");
            var cmd = (ICommand)Activator.CreateInstance(cmdType);

            var aggregateId = id.ToGuid();

            await  _specificationExecutor.ExecuteCommand(metadata, aggregateId, cmd);
        }
        //[When(@"I use '(.*)' to '(.*)'")]
        public async Task WhenUseTo(string id, string commandType)
        {
            string category = _featureContext.FeatureInfo.Title;
            var (cmdType, metadata) = Dictionary.FindCommand($"{category} {commandType}");
            var cmd = (ICommand)Activator.CreateInstance(cmdType);

            var aggregateId = id.ToGuid();

            await _specificationExecutor.ExecuteCommand(metadata, aggregateId, cmd);
        }

        private T GetArgument<T>(Type argType, Guid? id, Table propertyTable)
        {
            T result = default(T);
            if (propertyTable != null && propertyTable.Rows.Count > 0)
                result = (T)propertyTable.Deserialize(argType, id);
            else
                result = (T)Activator.CreateInstance(argType);
            return result;
        }

        [When(@"I use (.*) to (.*)")]
        [When(@"I use (.*) to (.*):")]
        public async Task WhenUseToWithCommand(string id, string commandType, Table propertyTable)
        {
            string category = _featureContext.FeatureInfo.Title;
            var (cmdType, metadata) = Dictionary.FindCommand($"{category} {commandType}");
            ICommand cmd = GetArgument<ICommand>(cmdType,null, propertyTable);

            var aggregateId = id.ToGuid();

            await _specificationExecutor.ExecuteCommand(metadata, aggregateId, cmd);

        }
        
        [Then(@"I expect that: (.*):")]
        [Then(@"I expect that (.*):")]
        [Then(@"I expect that (.*)")]
        [Then(@"I expect (.*):")]
        [Then(@"I expect (.*)")]
        public async Task ThenIExpectThat(string eventName, Table propertyTable)
        {
            await Task.Delay(1000);
            var evType = Dictionary.FindEvent(eventName);
            var (lastAggregateId, lastEvent) = await _specificationExecutor.FindLestEvent(evType);
            var ev = GetArgument<IEvent>(evType, lastEvent.Id, propertyTable);

            lastEvent.BeEquivalentTo(ev);
        }
    }
}
