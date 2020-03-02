using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Carter;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.CommandHandlers;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Projections;
using EventDrivenThinking.EventInference.QueryProcessing;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.SessionManagement;
using EventDrivenThinking.Integrations.Carter;
using EventDrivenThinking.Integrations.EventAggregator;
using EventDrivenThinking.Integrations.EventStore;
using EventDrivenThinking.Integrations.SignalR;
using EventDrivenThinking.Reflection;
using EventDrivenThinking.Ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Core;


namespace EventDrivenThinking.App.Configuration.Server
{
    
    
    
    
}