using AutoMapper;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenUi.Tests.Model.Hotel
{
    public static class Constructor<TDestination>
        where TDestination : IEvent
    {
        private static Mapper _mapper;
        public static TDestination From<TCommand>(TCommand cmd)
            where TCommand : ICommand
        {
            return _mapper.Map<TDestination>(cmd);
        }
    }
}