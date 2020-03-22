using System;
using AutoMapper;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    class MirroringMapper<T>
    {
        public static readonly MirroringMapper<T> Instance = new MirroringMapper<T>();
        MirroringMapper()
        {
            var config = new MapperConfiguration(cfg => cfg.CreateMap<T, T>());
            this.Mapper = config.CreateMapper();
        }

        public IMapper Mapper { get; }
    }
    public static class ObjectExtensions{
        public static T CopyFrom<T>(this T destination, T source)
        {
            MirroringMapper<T>.Instance.Mapper.Map(source, destination);
            return destination;
        }
    }

    //public class QueryResult<TQuery, TModel, TResult> : IQueryResult<TModel, TResult>
    //    where TModel : IModel
    //    where TQuery : IQuery<TModel, TResult>
    //{
    //    private static IMapper _mapper;
    //    static QueryResult()
    //    {
    //        var config = new MapperConfiguration(cfg => cfg.CreateMap<TResult, TResult>());
    //        _mapper = config.CreateMapper();
    //    }
    //    private readonly TModel _model;
        
    //    private readonly TQuery _query;
    //    private readonly QueryOptions _options;
    //    private readonly Action<TQuery> _onDispose;


    //    public QueryResult(TModel model,
    //        TQuery query,
    //        QueryOptions options, Action<TQuery> onDispose)
    //    {
    //        _model = model;
        

    //        _query = query;
    //        _options = options;
    //        _onDispose = onDispose;
    //    }

    //    public void Dispose()
    //    {
    //        _onDispose(_query);
    //    }

    //    public TResult Result { get; set; }
    //    object ILiveResult.Model => Model;

    //    object ILiveResult.Result => Result;
    //    public TModel Model => _model;
    //    private EventHandler _initialized;
    //    public event EventHandler IsInitialized
    //    {
    //        add
    //        {
    //            _initialized += value; 
    //            if(IsCompleted)
    //                value(this, EventArgs.Empty);
    //        }
    //        remove { _initialized -= value; }
    //    }

    //    public bool IsCompleted { get; private set; }

    //    public void Update(TResult data)
    //    {
    //        if(IsCompleted)
    //            Result.CopyFrom(data);
    //    }
    //    public void OnComplete(TResult result)
    //    {
    //        IsCompleted = true;
    //        Result = result;
    //        if(_initialized != null)
    //            _initialized(this, EventArgs.Empty);
    //    }
    //}
}