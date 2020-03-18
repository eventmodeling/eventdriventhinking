# Values

1. Make EventSourcing easy, effortless. 
2. Encourage good practices however, don't limit development.
3. Never hide business complexity. 

# Design
## Configuration through slices based on domain divided namespaces.

Business logic is divided through namespaces. Each namespace should contain one slice, this is:
* Aggregate, or
* Projection, or
* Processor

## Let's describe responsibilities:
### Aggregates
1) They aggregate state that is only required for checking if an operation can be executed.
2) They aggregate short stream of events that are locally ordered.
3) They provide transaction borders.

### Projections
1) They project events from many streams (aggregates) onto a specific model.
2) They can be reset/recreated - this is: we can delete the specific model and rebuild it from events. <br/>
--> this implicates that the order of event that projection is executing is persisted or calculated. <br/>
--> [must have] if calculated => we perform join operation on streams (in the past). <br/>
--> [optional] if persisted => while we execute projection we need to persist the order, though for instance Links in EventStore.

### Processors
1) They can be used to transfer work between aggregates. 
2) They show the right complexity of business operations that work in the background. For example: 
* I want to invoke an aggregate but I don't have enough information. Then I create a processor that shows how hard it is to aggregate information from others to build the right command for an aggregate. Then the flow would consist of 2 stages: First, a projection is executed, then the model is injected into the processor and that one issue commands.
* I want to invoke many aggregates after a fact - many departments are working because of something. 
* They should be invoked by infrastructure. They **[FUTURE]** can define declaratively a delay when the handler is executed after an event.
3) The infrastructure needs to ensure that commands get delivered to an aggregates
4) And if an exception is thrown, than the processors is notifed.

## App Level responsibilities:
### Command Handler
1) Handles a command. 
2) Understand what to do with the command - should validate, check security, etc, thus:
3) Understands the internals of event-sourcing & messaging & deep architecture.
4) It is discouraged to write it's own command-handlers, however, it is not impossible. 

### Event Handler 
1) Handles event with event-metadata
2) Can be a projection event-handler, or
3) Can be a processor event-handler
4) It is discouraged to write it's own event-handler.

### Query Handler [**TO DESIGN**]
1) Handles querying.
2) The query is against a model that is used in a projection.
3) Querying should return a result (state) from query-handler and the **live subscription** for model-changes. 
4) To support live querying - projection's stream need to be partitioned according to predicate used in a query. This way client can subscribe for changes that are only relevant to his/her query. PartitionProjections need to save Link in EventStore.

## Configuration Level Responsibilities:
1) Should decide on means of communication: REST/SignalR/SOAP/etc.
2) Should decide on the conventions that expose internal though chosen protocol.

# Q & A
## Uniqueness 
It's a typical problem in EventSourcing to provide the uniqueness of certain sets. In the end, we wish to define this though the event (simple example):

```
class FancyUser : Aggregate<...>
{
    private static IEnumerable<IEvent> When(CreateFancyUser cmd)
    {
        yield return new FancyUserCreated(cmd.Email);
    }
}
class FancyUserCreated 
{
    [Unique (Partition="User")]
    public string Email { get;set; }
}
```

We wish to ensure that a certain partition of data is unique. Above code should work as follows:

1. We insert data into the table with timestamp & correlation-id, named "User". (first transaction)
2. We open second DB TRANSACTION that alters the record - set's it's state as persisted. 
3. We invoke the aggregate.
4. We commit the DB TRANSACTION.
5. If an exception was throw than we remove the record and commit the transaction.

If power outage happened in the middle, then we can recover after catchup-correlation-id chaser is live:
* if we had appended events to eventstream, than we should corresponding event with correlation-id.
* if we had not appended event to eventstream, then we should not have corresponding event with correlation-id.


## Id requirement to invoke a command
This can be done through a convention that divides commands into 2 categories:
* the ones that require Id for an aggregate
* the ones that don't care about Id of an aggregate. Than Id of an aggregate is the id of the command.

### Is it required to have "artificial" aggregate? I only need to send an e-mail?
Aggregate is essentially a stream of events. Is some situations this can be only one event. Since we require full traceability by default, we need to create such an event in the stream, even if it is only one. Thus "Id" won't be available in API, however, everything we will provide right coherent traceability.

## Replicated data
Processors are used to transferring work between aggregates. They can have state. however, their state is not ensured to be up-to-date.
We discourage 2 step thinking interference (reasoning): Facts -> Conclusions(1) -> Conclusions (2). This indicates abstractions, that we want "usually" to avoid.
1. We create some conclusions from facts, thus we create a projection that saves them into a model.
2. We want to derive more facts based on those conclusions. This is a business decision, not an engineering one. Thus we create a processor that runs on the top of projection that emits some commands. 
3. More events are created.

Since we created facts from conclusions, and those can be recreated at any given time, we can lose traceability here unless "original projection's stream order" is persisted.

## Data retention
Human memory works in such a way, that we have a short memory and long memory. We should have the same concept in event-driven-thinking.
We save a story with details, then after some time, we remember some conclusions, some events are forgotten, and later even conclusions might be forgotten. 
So:
1. Some streams can be deleted by policy - some aggregates might be deleted after x-days.
2. Deleting a stream should invoke "close books" procedure. This works similar to a processor, that's model is a list of aggregates events and the event is "close-books".
3. Closing books is a business operation in most cases.

## GDPR [TO DESIGN]
Sensitive data should be placed in a separate stream. An aggregate would be constructed out of the join of those streams. An event can be marked declaratively as sensitive.

## Row-level-security
* ACLs should be stored in EventMetadata. 
