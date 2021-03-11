using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Digipolis.Requestlogging
{
    public interface IExternalServiceTimer
    {
        void AddTimeSpan(TimeSpan timeSpent);
        TimeSpan Calculate();
    }

    public class ExternalServiceTimer : IExternalServiceTimer
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public ExternalServiceTimer(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            if (_contextAccessor.HttpContext == null)
                _contextAccessor.HttpContext = new DefaultHttpContext();
            
            contextAccessor.HttpContext.Items["ExternalServiceTimer"] = new ConcurrentBag<TimeSpan>();
        }

        public void AddTimeSpan(TimeSpan timeSpent)
        {
            if (_contextAccessor.HttpContext.Items["ExternalServiceTimer"] is ConcurrentBag<TimeSpan> bag)
                bag.Add(timeSpent);
        }

        public TimeSpan Calculate()
        {
            var result = new TimeSpan();
            if (_contextAccessor.HttpContext.Items["ExternalServiceTimer"] is ConcurrentBag<TimeSpan> bag)
            {
                foreach (var timing in bag)
                {
                    result = result.Add(timing);
                }
            }
            return result;
        }
    }
}