using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration
{
    public class Services : IServiceExtensionProvider
    {
        private readonly Dictionary<Type, object> _extensions;
        /// <summary>
        /// Service provider is used in factories. It should not be used in configuration of services, but in configure method in Startup class.
        /// </summary>
        private IAggregateSchemaRegister _aggregateSchemaRegister;
        private IProjectionSchemaRegister _projectionSchemaRegister;
        private IProcessorSchemaRegister _processorSchemaRegister;
        private ICommandInvocationSchemaRegister _commandInvocationRegister;
        private IQuerySchemaRegister _querySchemaRegister;
        private Lazy<Dictionary<Type, ISchemaRegister>> _register;

        public void AddSchemaRegister<T>(ISchemaRegister<T> schemaRegister)
            where T:ISchema
        {
            
            _register.Value.Add(typeof(T), schemaRegister);
        }
        private  Services AddSchemaInDict<T>(IDictionary<Type, ISchemaRegister> dict, 
            ISchemaRegister<T> schemaRegister) where T : ISchema
        {
            dict.Add(typeof(T), schemaRegister);
            return this;
        }
        public ISchemaRegister<T> GetSchemaRegister<T>()
            where T : ISchema
        {
            
            return (ISchemaRegister<T>) _register.Value[typeof(T)];
        }

      

        public void AddExtension<T>(object instance)
        {
            _extensions.Add(typeof(T), instance);
        }
        public T ResolveExtension<T>()
        {
            return (T)_extensions[typeof(T)];
        }

        public Services()
        {
            _extensions = new Dictionary<Type, object>();
            _register = new Lazy<Dictionary<Type, ISchemaRegister>>(() =>
            {
                Dictionary<Type, ISchemaRegister> r = new Dictionary<Type, ISchemaRegister>();
                AddSchemaInDict(r,CommandInvocationRegister)
                    .AddSchemaInDict(r,AggregateSchemaRegister)
                    .AddSchemaInDict(r,ProjectionSchemaRegister)
                    .AddSchemaInDict(r,ProcessorSchemaRegister)
                    .AddSchemaInDict(r,QuerySchemaRegister);
                return r;
            });
        }
        public IQuerySchemaRegister QuerySchemaRegister
        {
            get
            {
                if (_querySchemaRegister == null)
                {
                    _querySchemaRegister = new QuerySchemaRegister();

                }

                return _querySchemaRegister;
            }
            set
            {
                if (_querySchemaRegister != null) throw new InvalidOperationException($"{nameof(IQuerySchemaRegister)} has already been used.");
                _querySchemaRegister = value;
            }
        }

        public ICommandInvocationSchemaRegister CommandInvocationRegister
        {
            get
            {
                if (_commandInvocationRegister == null)
                {
                    _commandInvocationRegister = new CommandInvocationSchemaInvocationSchemaRegister();
                    
                }

                return _commandInvocationRegister;
            }
            set
            {
                if (_commandInvocationRegister != null) throw new InvalidOperationException($"{nameof(ICommandInvocationSchemaRegister)} has already been used.");
                _commandInvocationRegister = value;
            }
        }


        public IAggregateSchemaRegister AggregateSchemaRegister
        {
            get
            {
                if (_aggregateSchemaRegister == null)
                {
                    _aggregateSchemaRegister = new AggregateSchemaRegister();
                    
                }

                return _aggregateSchemaRegister;
            }
            set
            {
                if (_aggregateSchemaRegister != null) throw new InvalidOperationException($"{nameof(IAggregateSchemaRegister)} has already been used.");
                _aggregateSchemaRegister = value;
            }
        }

        public IProjectionSchemaRegister ProjectionSchemaRegister
        {
            get
            {
                if (_projectionSchemaRegister == null)
                {
                    _projectionSchemaRegister = new ProjectionSchemaRegister();
                    
                }

                return _projectionSchemaRegister;
            }
            set
            {
                if (_projectionSchemaRegister != null) throw new InvalidOperationException($"{nameof(IProjectionSchema)} has already been used.");
                _projectionSchemaRegister = value;
            }
        }

        public IProcessorSchemaRegister ProcessorSchemaRegister
        {
            get
            {
                if (_processorSchemaRegister == null)
                {
                    _processorSchemaRegister = new ProcessorSchemaRegister();
                   
                }

                return _processorSchemaRegister;
            }
            set
            {
                if (_processorSchemaRegister != null) throw new InvalidOperationException($"{nameof(IProcessorSchemaRegister)} has already been used.");
                _processorSchemaRegister = value;
            }
        }

        public IEnumerable<ISchemaRegister> Registers()
        {
            return _register.Value.Values;
        }
    }
}