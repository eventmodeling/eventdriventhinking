using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.SimpleWebApp.User
{
    public class UserEmailNotifer : Processor<UserEmailNotifer>
    {
        public IEnumerable<(Guid, ICommand)> When(EventMetadata m, UserCreated ev)
        {
            // here we calculate how the e-mail should look like...

            yield return (Guid.NewGuid(), new SendEmail());
        }
    }

    public class SendEmail : ICommand
    {
        public Guid Id { get; set; }

        public SendEmail()
        {
            Id = Guid.NewGuid();
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
        private static IEnumerable<IEvent> When(State st, CreateUser cmd)
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

    public class ChangeName : ICommand
    {
        public Guid Id { get; set; }
    }
}
