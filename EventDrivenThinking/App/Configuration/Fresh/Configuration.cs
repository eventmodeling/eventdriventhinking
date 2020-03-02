using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EventDrivenThinking.App.Configuration.Server;
using Serilog;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public class Configuration
    {
        private readonly ILogger _logger;
        private readonly List<Assembly> _assemblies;
        private SliceConfigurationCollection _slices;
        public IEnumerable<Assembly> Assemblies => _assemblies;
        internal Configuration(ILogger logger)
        {
            _logger = logger;
            Services = new Services();
            //Slices = new SliceConfigurationCollection(_logger,Services);
            _assemblies = new List<Assembly>();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Configuration AddAssemblies()
        {
            if (_slices != null)
                throw new ForbiddenConfigurationOrderException();

            return AddAssemblies(Assembly.GetCallingAssembly());
        }

        public Configuration AddAssemblies(params Assembly[] assemblies)
        {
            if (_slices != null)
                throw new ForbiddenConfigurationOrderException();

            _assemblies.AddRange(assemblies);
            return this;
        }
        public Services Services { get; }

        public SliceConfigurationCollection Slices
        {
            get
            {
                if (_slices == null)
                {
                    _slices = new SliceConfigurationCollection(_logger, Services);
                    var assemblies = Assemblies.ToArray();
                    foreach (var i in Services.Registers())
                        i.Discover(assemblies);
                }
                return _slices;
            }
        }
    }
    public interface IServiceExtensionProvider
    {
        public void AddExtension<T>(object instance);
        public T ResolveExtension<T>();
    }
    public class ForbiddenConfigurationOrderException : Exception { }
}