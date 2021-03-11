using System;

namespace Digipolis.Requestlogging
{
    public class LogInfo
    {
        public string RequestPath { get; set; }
        public string HttpMethod { get; set; }
        public Guid InnerCorrelationId { get; set; }
        public DateTime StartTime { get; set; }
    }
}
