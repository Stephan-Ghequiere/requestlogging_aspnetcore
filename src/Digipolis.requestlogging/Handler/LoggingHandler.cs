using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digipolis.Requestlogging

{
    public class LoggingHandler : System.Net.Http.DelegatingHandler
    {
        private readonly IExternalServiceTimer _timer;
        private readonly ILogger<LoggingHandler> _logger;
        private readonly OutgoingRequestLoggingOptions _options;

        public LoggingHandler(ILogger<LoggingHandler> logger, IExternalServiceTimer timer, IOptions<OutgoingRequestLoggingOptions> options)
        {
            _logger = logger;
            _timer = timer;
            _options = options.Value;
        }
        
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var excluded = _options.ExcludedPaths;
            var isExcludedCall = excluded.Any(ex => request.RequestUri.PathAndQuery.Contains(ex));
            if (!isExcludedCall)
            {
                var logInfo = new LogInfo
                {
                    InnerCorrelationId = Guid.NewGuid(),
                    StartTime = DateTime.UtcNow,
                    HttpMethod = request.Method.ToString(),
                    RequestPath = request.RequestUri.OriginalString
                };
                LogRequest(request, logInfo);
                try
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    LogResponse(response, logInfo);
                    return response;
                }
                catch (TimeoutException)
                {
                    _logger.LogInformation("Request with inner correlation id: {InnerCorrelationId} timed out", logInfo.InnerCorrelationId);
                    throw;
                }
                catch (Exception ex)
                {
                    LogError(logInfo, ex);
                    throw;
                }
            }
            else
                return await base.SendAsync(request, cancellationToken);
        }
        private void LogError(LogInfo logInfo, Exception ex)
        {
            var messageFormat = new StringBuilder();
            messageFormat.AppendLine("An error occurred from outgoing request with inner correlation id: {InnerCorrelationId}");
            messageFormat.AppendLine("Method: {HttpMethod}");
            messageFormat.AppendLine("Path: {RequestPath}");
            messageFormat.AppendLine("Elapsed time: {TimeElapsed}");
            messageFormat.AppendLine("Exception message: {ExceptionMessage}");

            var millisecondsSpent = (int) DateTime.UtcNow.Subtract(logInfo.StartTime).TotalMilliseconds;

            var values = new object[]
            {
                logInfo.InnerCorrelationId,
                logInfo.HttpMethod,
                logInfo.RequestPath,
                millisecondsSpent,
                ex.Message
            };
            _logger.LogInformation(messageFormat.ToString(), values);
        }

        private void LogRequest(HttpRequestMessage request, LogInfo logInfo)
        {
            var messageFormat = new StringBuilder();
            messageFormat.AppendLine("Start sending outgoing request with inner correlation id: {InnerCorrelationId}");
            messageFormat.AppendLine("Method: {HttpMethod}");
            messageFormat.AppendLine("Path: {RequestPath}");
            messageFormat.AppendLine("Headers: {HttpHeaders}");

            var headers = string.Join(Environment.NewLine, request.Headers.Select(GetFormattedHeader));
           
            var values = new object[]
            {
                logInfo.InnerCorrelationId,
                logInfo.HttpMethod,
                logInfo.RequestPath,
                headers
            };

            _logger.LogInformation(messageFormat.ToString(), values);
        }

        private void LogResponse(HttpResponseMessage response, LogInfo info)
        {
            var messageFormat = new StringBuilder();
            messageFormat.AppendLine("Received answer from outgoing request with inner correlation id: {InnerCorrelationId}");
            messageFormat.AppendLine("Method: {HttpMethod}");
            messageFormat.AppendLine("Path: {RequestPath}");
            messageFormat.AppendLine("Status code: {ResponseStatusCode}");
            messageFormat.AppendLine("Elapsed time: {TimeElapsed}");

            var statusCode = (int)response.StatusCode;
            var timeElapsed = DateTime.UtcNow.Subtract(info.StartTime);
            _timer.AddTimeSpan(timeElapsed);

            var values = new object[]
            {
                info.InnerCorrelationId,
                info.HttpMethod,
                info.RequestPath,
                statusCode,
                (int) timeElapsed.TotalMilliseconds
            };

            _logger.LogInformation(messageFormat.ToString(), values);
        }

        private string GetFormattedHeader(KeyValuePair<string, IEnumerable<string>> rawHeader)
        {
            var valuesFormatted = string.Join(", ", rawHeader.Value.Select(y => y));
            return $"{rawHeader.Key}:{valuesFormatted}";
        }
    }
}
