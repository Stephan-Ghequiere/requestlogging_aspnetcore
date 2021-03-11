namespace Digipolis.Requestlogging
{
    public abstract class RequestLoggingOptions
    {
        public string[] ExcludedPaths { get; set; } = { };
    }

    public class IncomingRequestLoggingOptions : RequestLoggingOptions
    {
        public bool IncludeBody { get; set; } = false;

        public string[] ExcludedBodyProperties { get; set; } = { };
    }

    public class OutgoingRequestLoggingOptions : RequestLoggingOptions
    {
    }
}
