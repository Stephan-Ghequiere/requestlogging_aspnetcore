# Requestlogging Toolbox

Tools for logging incomming and outgoing requests.

## Table of Contents

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [Installation](#installation)

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

The toolbox provides a middleware for logging incoming requests and a delegating handler for logging outgoing requests.
Both will log the the HttpMethod, Path and Headers on requests, on responses the HttpMethod, Path, StatusCode and elapsed time is logged.
When using the middleware in conjunction with the delegating handler the middleware will also log the total time tha app was waiting on calls.

To use the request logging middelware two steps are needed.

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
                    options.ExcludedPaths = new[] {"status", "swagger", "hangfire"};
                    options.IncludeBody = true;
                    options.ExcludedBodyProperties = new[] {"photo", "userid"};
                },
                outgoing: options =>
                {
                    options.ExcludedPaths = new[] {"status"};
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
  app.UseRequestlogging();
```

Please note that the order in wich middleware is added is the order of execution of the middleware. Putting UseCorrelation() (with correlationheader required) before UseSwaggerUI() will make the SwaggerUI fail. UseCorrelation should come after UseSwaggerUI() and before UseMvc().
