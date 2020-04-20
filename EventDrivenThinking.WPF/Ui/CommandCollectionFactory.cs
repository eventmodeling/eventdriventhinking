using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.Reflection;

namespace EventDrivenThinking.Ui
{
    public class CommandCollectionFactory<T>
        where T : ViewModelBase<T>
    {
        private static List<ICommandFactory<T>> _commands;

        public static void Wire(ViewModelBase<T> vm, ICommandsCollection collection)
        {
            if (_commands == null)
            {
                var viewModelType = typeof(T);
                lock (typeof(CommandCollectionFactory<T>))
                {
                    _commands = new List<ICommandFactory<T>>();
                    var tupleType = typeof(ValueTuple<,>);

                    var methods = viewModelType
                        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .ToArray();

                    foreach (var m in methods.Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == tupleType))
                    {
                        var args = m.ReturnType.GetGenericArguments();
                        var keyType = args[0];
                        var commandType = args[1];
                        
                        if (!typeof(ICommand).IsAssignableFrom(commandType))
                            throw new InvalidViewModelStructureException($"'{commandType.FullName}' is expected to implement interface ICommand, because it was found in method '{m.Name}' of class '{typeof(T).FullName}'.");
                        
                        var factoryType =
                            typeof(CommandFactory<,,>).MakeGenericType(viewModelType, keyType, commandType);
                        var factory = Ctor<ICommandFactory<T>>.Create(factoryType);
                        factory.Configure(m);
                        _commands.Add(factory);
                    }
                }
            }

            foreach (var i in _commands)
            {
                var cmd = i.Create((T) vm);
                collection.Add(i.CommandName, cmd);
            }
        }
    }

    public class InvalidViewModelStructureException : Exception
    {
        public InvalidViewModelStructureException(string message) : base(message)
        {
           
        }
    }
}