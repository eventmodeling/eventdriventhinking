using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.SimpleWebApp.User
{
    public class UserProcessor : Processor<UserProcessor>
    {
        public IEnumerable<(Guid, ICommand)> When(EventMetadata m, UserCreated ev)
        {
            yield break;
        }
    }

    public class UserAggregate : Aggregate<UserAggregate.State>
    {
        public struct State
        {

        }

        private static State Given(State st, UserCreated ev)
        {
            return st;
        }
        private static IEnumerable<IEvent> When(State st, CreateUser user)
        {
            yield return new UserCreated();
        }
    }
    public class UserCreated : IEvent {
        public Guid Id { get; set; }

        public UserCreated()
        {
            Id = Guid.NewGuid();
        }
    }
    public class CreateUser : ICommand {
        public Guid Id { get; set; }

        public CreateUser()
        {
            Id = Guid.NewGuid();
        }
    }
}
