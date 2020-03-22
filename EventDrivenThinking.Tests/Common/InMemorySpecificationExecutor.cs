using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using AutoMapper;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.QueryProcessing;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Reflection;

#pragma warning disable 1998
namespace EventDrivenThinking.Tests.Common
{
    public interface ISpecificationExecutor : IDisposable
    {
        ISpecificationExecutor Init(IAggregateSchemaRegister aggregateSchemaRegister);
        ISpecificationExecutor Init(IProjectionSchemaRegister projectionSchemaRegister);
        ISpecificationExecutor Init(IQuerySchemaRegister querySchemaRegister);

        IAsyncEnumerable<(Guid, IEvent)> GetEmittedEvents();
        Task ExecuteCommand(IClientCommandSchema metadata, Guid aggregateId, ICommand cmd);
        Task AppendFact(Guid aggregateId, IEvent ev);
        Task<ILiveResult> ExecuteQuery(IQuery query);
        IEnumerable<ILiveResult> GetQueryResults();
    }

    
    public class InMemorySpecificationExecutor : ISpecificationExecutor
    {
        class LiveQuery
        {
            public IQuery Query;
            public IProjection Projection;
            public ILiveResult Result;
        }
        public InMemorySpecificationExecutor()
        {
            _eventsInPast = new List<(Guid, IEvent)>();
            _emittedEvents = new List<(Guid, IEvent)>();
            _queryResults = new List<LiveQuery>();
            _runningProjections = new List<(IProjection, IProjectionSchema)>();
        }
        private readonly List<(Guid, IEvent)> _eventsInPast;
        private readonly List<(Guid, IEvent)> _emittedEvents;
        private readonly List<(IProjection, IProjectionSchema)> _runningProjections;
        private readonly List<LiveQuery> _queryResults;

        private IAggregateSchemaRegister _aggregateSchemaRegister;
        private IProjectionSchemaRegister _projectionSchemaRegister;
        private IQuerySchemaRegister _querySchemaRegister;

        public ISpecificationExecutor Init(IAggregateSchemaRegister aggregateSchemaRegister)
        {
            this._aggregateSchemaRegister = aggregateSchemaRegister;
            return this;
        }
        public ISpecificationExecutor Init(IProjectionSchemaRegister projectionSchemaRegister)
        {
            this._projectionSchemaRegister = projectionSchemaRegister;
            return this;
        }

        public ISpecificationExecutor Init(IQuerySchemaRegister querySchemaRegister)
        {
            this._querySchemaRegister = querySchemaRegister;
            return this;
        }

        public async IAsyncEnumerable<(Guid, IEvent)> GetEmittedEvents()
        {
            foreach (var i in _emittedEvents)
                yield return i;
        }

        public async Task ExecuteCommand(IClientCommandSchema metadata, Guid aggregateId, ICommand cmd)
        {
            var aggregateType = _aggregateSchemaRegister.FindAggregateByCommand(metadata.Type);
            IAggregate aggregate = (IAggregate)Activator.CreateInstance(aggregateType.Type);
            aggregate.Id = aggregateId;
            aggregate.Rehydrate(_eventsInPast.Union(_emittedEvents)
                .Where(x => x.Item1 == aggregateId)
                .Select(x => x.Item2));

            var newEvents = aggregate.Execute(cmd)
                .Select(x => (aggregateId, x));

            _emittedEvents.AddRange(newEvents);
            await RunProjections(newEvents);
        }

        private async Task RunProjections(IEnumerable<(Guid aggregateId, IEvent x)> newEvents)
        {
            foreach ((Guid aggregateId, IEvent ev) i in newEvents)
            {
                foreach ((IProjection projection, IProjectionSchema schema) p in _runningProjections)
                {
                    if (p.schema.Events.Contains(i.ev.GetType()))
                    {
                        await p.projection.Execute(new (EventMetadata, IEvent)[]
                        {
                            (new EventMetadata(i.aggregateId, null, Guid.NewGuid(), -1),
                                i.ev)
                        });

                        foreach (var queryResult in _queryResults
                            .Where(x=>x.Projection.Model == p.projection.Model))
                        {
                            await UpdateQueryResultsForProjection(queryResult);
                        }
                    }
                }
            }
        }

        private async Task UpdateQueryResultsForProjection(LiveQuery query)
        {
            var queryType = query.Query.GetType();
            var args = queryType
                .FindOpenInterfaces(typeof(IQuery<,>))
                .Single()
                .GetGenericArguments();

            var task = (Task<ILiveResult>)typeof(InMemorySpecificationExecutor)
                .GetMethod(nameof(OnUpdateQueryResultsForProjection),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                .MakeGenericMethod(new[] { queryType, args[1], args[0] })
                .Invoke(this, new object[] { query });
        }


        private async Task<ILiveResult> OnUpdateQueryResultsForProjection<TQuery, TResult, TModel>(LiveQuery query)
            where TModel : IModel
            where TQuery : IQuery<TModel, TResult>
        {
            var querySchema = _querySchemaRegister.First(x => x.Type == typeof(TQuery));
            
            var queryHandler = (IQueryHandler<TQuery, TModel, TResult>)Activator.CreateInstance(querySchema.QueryHandlerType);
            var qr = queryHandler.Execute((TModel)query.Projection.Model, (TQuery)query.Query);

            QueryEngine<TModel>.LiveQuery<TQuery, TResult> l = (QueryEngine<TModel>.LiveQuery<TQuery, TResult>) query.Result;
            l.OnUpdate(qr);
            
            return query.Result;
        }

        public async Task AppendFact(Guid aggregateId, IEvent ev)
        {
            _eventsInPast.Add((aggregateId, ev));
        }

        public async Task<ILiveResult> ExecuteQuery(IQuery query)
        {
            var args = query.GetType()
                .FindOpenInterfaces(typeof(IQuery<,>))
                .Single()
                .GetGenericArguments();

            var task = (Task< ILiveResult>) typeof(InMemorySpecificationExecutor)
                .GetMethod(nameof(ExecuteQuery),
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                .MakeGenericMethod(new[] {query.GetType(), args[1], args[0]})
                .Invoke(this, new[] {query});


            ILiveResult r = await task;
            
            return r;
        }

        public IEnumerable<ILiveResult> GetQueryResults()
        {
            return _queryResults.Select(x=>x.Result);
        }

        private async Task<ILiveResult> ExecuteQuery<TQuery, TResult, TModel>(TQuery query) 
            where TModel : IModel
            where TQuery: IQuery<TModel, TResult>
        {
            // 1. Get the projection
            // 2. Get the query-handler
            var querySchema = _querySchemaRegister.First(x => x.Type == query.GetType());
            var projectionSchema = _projectionSchemaRegister.FindByModelType(querySchema.ModelType);

            TModel model = default(TModel);
            if (typeof(TModel).IsInterface)
            {
                var implementationByConvention = typeof(TModel).Assembly.GetTypes()
                    .First(x => typeof(TModel).IsAssignableFrom(x) && !x.IsInterface);
                model = (TModel) Activator.CreateInstance(implementationByConvention);
            }
            else model = Activator.CreateInstance<TModel>();

            var projection = (IProjection<TModel>)Activator.CreateInstance(projectionSchema.Type, model);
            _runningProjections.Add((projection, projectionSchema));

            var rightEvents = projectionSchema.Events.ToHashSet();
            var eventsToProject = _eventsInPast.Where(x => rightEvents.Contains(x.Item2.GetType()))
                .Select(x => (new EventMetadata(x.Item1, null, Guid.NewGuid(), -1), x.Item2));

            await projection.Execute(eventsToProject);

            var queryHandler = (IQueryHandler<TQuery, TModel, TResult>) Activator.CreateInstance(querySchema.QueryHandlerType);
            var qr = queryHandler.Execute(projection.Model, query);

            var result =
                new QueryEngine<TModel>.LiveQuery<TQuery, TResult>(query, null, querySchema, OnDispose,
                    new QueryOptions());

            result.OnResult(qr);

            _queryResults.Add(new LiveQuery { Query = query, Result = result, Projection = projection});

            return result;
        }

        private void OnDispose<TQuery>(TQuery obj) where TQuery : IQuery
        {
            
        }

        public void Dispose()
        {
        }
    }
}