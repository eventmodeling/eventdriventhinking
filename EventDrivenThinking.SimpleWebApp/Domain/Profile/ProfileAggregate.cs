using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Core;

namespace EventDrivenThinking.SimpleWebApp.Domain.Profile
{
    public class ProfileAggregate : Aggregate<ProfileAggregate.State>
    {
        public struct State
        {
            public string LastName;
        }

        private static IEnumerable<IEvent> When(State st, RenameProfile cmd)
        {
            if(st.LastName == cmd.NewName)
                throw new Exception("You cannot do it...");

            yield return new ProfileRenamed() {NewName = cmd.NewName };
        }



        private static State Given(State state, ProfileCreated ev)
        {
            state.LastName = ev.Name;
            return state;
        }

        private static IEnumerable<IEvent> When(State st, CreateProfile cmd)
        {
            yield return new ProfileCreated() { Name = cmd.Name };
        }
    }

    public class ProfileRenamed : IEvent
    {
        public Guid Id { get; set; }
        public string NewName { get; set; }

        public ProfileRenamed()
        {
            Id = Guid.NewGuid();
        }
    }

    public class RenameProfile : ICommand
    {
        public Guid Id { get; set; }
        public string NewName { get; set; }

        public RenameProfile()
        {
            Id = Guid.NewGuid();
        }
    }

    public class ProfileCreated : IEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ProfileCreated()
        {
            Id = Guid.NewGuid();
        }
    }

    public class CreateProfile : ICommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public CreateProfile()
        {
            Id = Guid.NewGuid();
        }
    }
}
