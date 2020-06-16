using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.SimpleWebApp.Domain.Profile;
using EventDrivenThinking.SimpleWebApp.User;

namespace EventDrivenThinking.SimpleWebApp.Views.User
{
    public class UserModel : IModel { }
    public class UserProjection : Projection<UserModel>
    {
        public UserProjection(UserModel model) : base(model)
        {
        }

        private static async Task Given(UserModel model, EventMetadata m, ProfileRenamed ev)
        {
            
        }

        private static async Task Given(UserModel model, EventMetadata m, UserCreated ev)
        {
           
        }
    }
}
