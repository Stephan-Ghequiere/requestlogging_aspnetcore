# Requestlogging Toolbox

Tools for logging incomming and outgoing requests.

## Table of Contents

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [Installation](#installation)
- [Usage](#usage)
- [Coupling with a Service Agent](#coupling-with-a-service-agent)
- [Contributing](#contributing)
- [Support](#support)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Installation

To add the toolbox to a project, you add the package to the csproj-file :

```xml
  <ItemGroup>
    <PackageReference Include="Digipolis.Requestlogging" Version="1.0.0" />
  </ItemGroup>
``` 

In Visual Studio you can also use the NuGet Package Manager to do this.

## Usage

Consistent and clear logging makes tracking down errors easier. 
Logging incoming and outgoing REST calls can play a major role in finding why calls return errors.

The toolbox provides a Middleware for logging incoming requests and a DelegatingHandler for logging outgoing requests.
Both will log the the HttpMethod, Path and Headers on requests, on responses the HttpMethod, Path, StatusCode and elapsed time is logged.
When using the Middleware in conjunction with the DelegetingHandler the Middleware will also log the total time the app was waiting on calls.

To use the RequestLoggingMiddelware two steps are needed.

First register the service in the **ConfigureServices** method in the **Startup** class:


With the default options:
``` csharp
  services.AddRequestLogging();
```

With custom options:
``` csharp
  services.AddRequestLogging(
                incoming: options =>
                {
                    options.ExcludedPaths = new[] {"/status", "/swagger", "/hangfire"};
                    options.IncludeBody = true;
                    options.ExcludedBodyProperties = new[] {"photo", "userid"};
                },
                outgoing: options =>
                {
                    options.ExcludedPaths = new[] {"/status"};
                }
            );
```

Following options can be set :

**IncomingRequestLoggingOptions**

Option | Description | Default
------ | ----------- | -------
ExcludedPaths | Any incoming request with a path that contains any of the strings provided, will be excluded from incoming request logging. | ```new[] {}```
IncludeBody | If set to ```true``` the middleware will log the body of the request, if there is one. | ```false```
ExcludedBodyProperties | If the body is included in the logging, but you need to specify certain properties shouldn't be included in the log.<br/>Beware, as this will recursively run trough all the properties in the body and could potentially make calls slower | ```new[] {}```

**OutgoingRequestLoggingOptions**

Option | Description | Default
------ | ----------- | -------
ExcludedPaths | Any outgoing request with a path that contains any of the strings provided, will be excluded from outgoing request logging. | ```new[] {}```

Then add the middleware to the appication in the **Configure** method in the **Startup** class:

``` csharp
  app.UseRequestLogging();
```

Please note that the order in wich middleware is added is the order of execution of the middleware. UseRequestLogging should preferably be as early in the request pipeline as possible for accurate timing of the request and certainly before UseMvc().


## Coupling with a Service Agent

Starting from .NET Core v2.1, it is recommended to register HttpClient in serviceagents as a singleton using the HttpClientFactory. This package contains a DelegatingHandler which can be used to add the correlationheader to each outgoing request.

``` csharp

 services.AddHttpClient(nameof(SampleApi2Agent), (provider, client) =>
            {
                var settings = provider.GetService<IOptions<SampleApi2AgentSettings>>().Value;
                client.BaseAddress = new Uri($"{settings.Url.Normalize()}");
                foreach (var header in settings.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            })
            .AddHttpMessageHandler<CorrelationIdHandler>()
 ```

## Contributing

Pull requests are always welcome, however keep the following things in mind:

- New features (both breaking and non-breaking) should always be discussed with the [repo's owner](#support). If possible, please open an issue first to discuss what you would like to change.
- Fork this repo and issue your fix or new feature via a pull request.
- Please make sure to update tests as appropriate. Also check possible linting errors and update the CHANGELOG if applicable.

## Support

Stephan Ghequiere (<stephan.ghequiere@digipolis.be>)