using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Digipolis.Requestlogging
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggerMiddleware> _logger;
        private readonly IncomingRequestLoggingOptions _options;

        public LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger, IOptions<IncomingRequestLoggingOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Logs incoming requests.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="timer">The timer.</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context, IExternalServiceTimer timer)
        {
            var request = context.Request;
            var excluded = _options.ExcludedPaths;
            var isStatusCall = excluded.Any(x => request.Path.Value.Contains(x, StringComparison.InvariantCultureIgnoreCase));
            if (!isStatusCall)
            {
                context.Request.EnableRewind();
                var logInfo = new LogInfo
                {
                    InnerCorrelationId = Guid.NewGuid(),
                    StartTime = DateTime.UtcNow,
                    HttpMethod = context.Request.Method,
                    RequestPath = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}"
                };
                LogRequest(context.Request, logInfo);
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                try
                {
                    await _next(context);
                    LogResponse(context, logInfo, timer);
                }
                catch (Exception ex)
                {
                    LogErrorResponse(context, logInfo, timer, ex);
                    throw;
                }
            }
            else
            {
                await _next(context);
            }

        }

        private void LogRequest(HttpRequest request, LogInfo logInfo)
        {
            var messageFormat = new StringBuilder();
            messageFormat.AppendLine("Start processing incoming request with inner correlation id: {InnerCorrelationId}");
            messageFormat.AppendLine("Method: {HttpMethod}");
            messageFormat.AppendLine("Path: {RequestPath}");
            messageFormat.AppendLine("Headers: {HttpHeaders}");
            messageFormat.AppendLine("Content-Length: {ContentLength}");
            
            var headers = string.Join(Environment.NewLine, request.Headers.Select(GetFormattedHeader));
            var length = Convert.ToInt32(request.ContentLength);
            string jsonData ;
            if (_options.IncludeBody)
            {
                messageFormat.AppendLine("Body: {Body}");
                if (length > 0)
                {
                    var body = request.Body;
                    request.EnableRewind();
                    jsonData = new StreamReader(body).ReadToEnd();
                    if (_options.ExcludedBodyProperties.Any())
                    {
                        dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(jsonData);
                        Unravel(jsonObject);
                        jsonData = JsonConvert.SerializeObject(jsonObject);
                    }
                }
                else
                {
                    jsonData = "No Content";
                }
            }
            else
            {
                jsonData = "Body not logged.";
            }

            var bodyValues = new object[]
            {
                logInfo.InnerCorrelationId,
                logInfo.HttpMethod,
                logInfo.RequestPath,
                headers,
                length,
                jsonData
            };

            _logger.LogInformation(messageFormat.ToString(), bodyValues);
        }

        private void Unravel(dynamic jsonObject)
        {
            if (!(jsonObject is ExpandoObject)) return;
            var dic = (IDictionary<string, object>)jsonObject;
            var keys = dic.Keys.ToList();
            foreach (var key in keys)
            {
                if (dic[key] is ExpandoObject childValue)
                {
                    Unravel(childValue);
                }
                else if (dic[key] is IEnumerable<object> childValues)
                {
                    foreach (var value in childValues)
                    {
                        Unravel(value);
                    }
                }
                else
                {
                    if (_options.ExcludedBodyProperties.Any(x => key.Contains(x, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        dic.Remove(key);
                    }
                }
            }
        }
        private void LogResponse(HttpContext context, LogInfo info, IExternalServiceTimer timer)
        {
            var messageFormat = new StringBuilder();
            messageFormat.AppendLine("Finished processing incoming request with inner correlation id: {InnerCorrelationId}")
                .AppendLine("Method: {HttpMethod}")
                .AppendLine("Path: {RequestPath}")
                .AppendLine("Status code: {ResponseStatusCode}")
                .AppendLine("Elapsed time: {TimeElapsed}")
                .AppendLine("Time waiting for external requests: {TimeSpentWaitingForRequests}");

            var statusCode = context.Response.StatusCode;
            var timeElapsed = (int)DateTime.UtcNow.Subtract(info.StartTime).TotalMilliseconds;
            var timeSpentWaitingForRequests = (int) timer.Calculate().TotalMilliseconds;

            var bodyValues = new object[]
            {
                info.InnerCorrelationId,
                info.HttpMethod,
                info.RequestPath,
                statusCode,
                timeElapsed,
                timeSpentWaitingForRequests
            };

            _logger.LogInformation(messageFormat.ToString(), bodyValues);
        }
        private void LogErrorResponse(HttpContext context, LogInfo info, IExternalServiceTimer timer, Exception exception)
        {
            _logger.LogError(exception, "An error occured.");

            var messageFormat = new StringBuilder();
            messageFormat.AppendLine("An error occurred while processing incoming request with inner correlation id: {innerCorrelationId}")
                .AppendLine("Method: {HttpMethod}")
                .AppendLine("Path: {RequestPath}")
                .AppendLine("Status code: {ResponseStatusCode}")
                .AppendLine("Elapsed time: {TimeElapsed}")
                .AppendLine("Time waiting for external requests: {TimeSpentWaitingForRequests}");

            var statusCode = context.Response.StatusCode;
            var timeElapsed = (int)DateTime.UtcNow.Subtract(info.StartTime).TotalMilliseconds;
            var timeSpentWaitingForRequests = (int)timer.Calculate().TotalMilliseconds;

            var bodyValues = new object[]
            {
                info.InnerCorrelationId,
                info.HttpMethod,
                info.RequestPath,
                statusCode,
                timeElapsed,
                timeSpentWaitingForRequests
            };

            _logger.LogInformation(messageFormat.ToString(), bodyValues);
        }
        private string GetFormattedHeader(KeyValuePair<string, StringValues> rawHeader)
        {
            var valuesFormatted = string.Join(", ", rawHeader.Value.Select(y => y));
            return $"{rawHeader.Key}:{valuesFormatted}";
        }
    }
}