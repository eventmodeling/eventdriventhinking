using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace EventDrivenThinking.Logging
{
    public class LoggerFactory
    {
        private static LoggerFactory _instance;
        public static void Init(ILogger logger)
        {
            _instance = new LoggerFactory(logger);
        
        }

        public static ILogger For<T>()
        {
            if (_instance != null)
                return _instance.ForContext<T>();
            else throw new InvalidOperationException();
        }
        private readonly ILogger _logger;
        

        private LoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger ForContext<T>()
        {
            return _logger.ForContext<T>();
        }

    }
}
