using System;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenUi.Tests
{
    public static class ServiceProviderAssertions
    {
        public static void AssertTryResolveService<T>(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<T>();
        }
        public static void AssertTryResolveClass<T>(this IServiceProvider serviceProvider)
        {
            try
            {
                var type = typeof(T);
                ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type);
                ActivatorUtilities.CreateInstance<T>(serviceProvider);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}