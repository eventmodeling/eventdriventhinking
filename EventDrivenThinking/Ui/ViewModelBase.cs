using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Windows.Input;
using Prism.Mvvm;

namespace EventDrivenThinking.Ui
{
    public abstract class ViewModelBase<T> : BindableBase, IIdentity
        where T : ViewModelBase<T>
    {
        /// <summary>
        ///     Given, When
        /// </summary>
        protected Guid _id;

        protected ViewModelBase(Guid id = default)
        {
            if (id == Guid.Empty)
                id = Guid.NewGuid();

            _id = id;
            Commands = new CommandsCollection();
            CommandCollectionFactory<T>.Wire(this, Commands);
        }

        public dynamic Commands { get; }

        public Guid Id
        {
            get => _id;
            set => base.SetProperty(ref _id, value);
        }


        private class CommandsCollection : DynamicObject, ICommandsCollection
        {
            private readonly Dictionary<string, ICommand> _commands;

            public CommandsCollection()
            {
                _commands = new Dictionary<string, ICommand>();
            }


            public ICommandsCollection Add(string name, ICommand cmd)
            {
                _commands.Add(name, cmd);
                return this;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var r = _commands.TryGetValue(binder.Name, out var cmd);
                if (!r)
                    Debug.WriteLine($"{binder.Name} was not present in the collection of commands: {Environment.NewLine}{string.Join(Environment.NewLine,_commands.Select(x=>x.Key))}.");
                result = cmd;
                return r;
            }
        }
    }
}