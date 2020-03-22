using AutoMapper;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.Example.Model.Domain.Hotel
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