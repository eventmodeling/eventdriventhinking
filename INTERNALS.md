# Assumptions:
## Server

### REST API
1. REST Carter / Creates Scope 1
2. Calls Command Handler Dispatcher / Create Scope 2
3. Calls Command Aggregate Handler
4. Calls Aggregate
5. Saves stream

### EventStore Subscriptions
1. EventStore subscription
2. Calls Event Handler Disptacher / Create Scope 1 [obsolete]
3. Calls Event (Projection | Processor) Handler

## Client
### Calling server
1. View model rises CommandEnvelope to IEventAggregator (IEventPublisher -> UiEventBus)
2. Configuration subscribes to CommandEnvelope event AggregateConfigurator and calls IHttpClient

### Subscribing from server
1. All projections & processor enabled in the app subscribe though signal-r
2. Subscription calls UiEventBus (IEventAggregator)
3. [CHECK] EventHandlerDispatcher has subscribed to handle events though IEventAggregator
4. AppProcess receives all the calls though UiEventBus
5. Manual vm subscription is possible though UiEventBus

## What is the flow for query-handlers?
1. QueryInvoker
2. Creates Model if needed
3. Prepares QueryResultObject that will finish of query-process
4. Invokes QueryEngine to load data or subscribe for changes
5. QueryEngine invokes QueryHandler (markup interface) to filter results from the model
6. Results are returned to the caller though onComplete event  of QueryResultObject.

## Handle connection issues

# Configuration
1. We can declaratively configure part of the domain to work with different stuff:
1.1 EventStore: InMemory | EventStore, AsyncPusher
1.2 Subscriptions: From EventStore Subscriptions, From EventAggregator (though signalR)
1.3 Invocation: Though EventAggregator, Through REST
2. All configuration should be devided into server/client.
3. Server part, can work in distributed environemnt, this is:
3.1 Pusher, some streams can be pushed to different server.
3.2 Merger, events when merged can:
3.3 Rewrite history ?
3.4 Accept merge or reject or ignore ?
3.5 When merge is provided though SignalR protocol. 

## Live Query Execution
1. Client can query with a REST to get last "snapshot" of a model. 
2. Then automatically it subscribes to a kown partition - that is related to query predicate.
3. When projection kicks in, the partition is calculated and events are published to this group.
