using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.EventInference.Projections
{
    public class ProjectionExecutor<TModel> : IProjectionExecutor<TModel> where TModel : IModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectionSchema _schema;

        private TModel _model;
        private readonly ILogger _logger;
        public ProjectionExecutor(IProjectionSchemaRegister projectionSchema, IServiceProvider serviceProvider, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _schema = projectionSchema.FindByModelType(typeof(TModel));
        }

        public void Configure(TModel model)
        {
            _model = model;
        }

        public async Task Execute(IEnumerable<EventEnvelope> events)
        {
            IProjection<TModel> projection = (IProjection<TModel>)ActivatorUtilities.CreateInstance(_serviceProvider, _schema.Type, _model);

            foreach(var e in events)
                _logger.Information("ProjectionExecutor for {modelName} is receiving from {aggregateId} an {eventName} {eventId}", typeof(TModel).Name, e.Metadata.AggregateId, e.Event.GetType().Name, e.Event.Id);

            await projection.Execute(events.Select(x=>(x.Metadata, x.Event)));
        }
    }
}