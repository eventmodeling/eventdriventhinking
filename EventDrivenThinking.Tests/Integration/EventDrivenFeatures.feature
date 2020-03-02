Feature: EventDrivenFeatures
	In order to siplify development I want to show development process

Scenario: I want to track every command execution

Scenario Outline: I want to develop command-slice.

Given The app is '<AppType>'
And The app uses '<EventStore>' as eventStore
And The app invokes command using '<CommandInvocationTransport>'
And The server projection use '<ServerProjectionSubscriptionMode>' for subscriptions
And The client projection use '<ClientProjectionSubscriptionMode>' for subscriptions
And I've build the pipelines
And I've defined past from the events
And I setup view-model
When I invoke commands
Then The command-ui-event is published
And The aggregate gets the command
And The event is published
And The projection gets the event
And The view-model gets changes

Examples: 
| AppType      | EventStore | ServerProjectionSubscriptionMode | CommandInvocationTransport | ClientProjectionSubscriptionMode |
| Standalone   | InProc     | EventAggregator                  | InProcRcp                  | EventAggregator                  |
| Standalone   | EventStore | EventStore                       | InProcRcp                  | EventStore                       |
| ClientServer | EventStore | EventStore                       | Rest                       | SignalR                          |


