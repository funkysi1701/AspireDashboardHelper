Aspire Dashboard Helper

The Aspire Dashboard Helper is a library that helps you to add OpenTelemetry to your application and send the data to an Aspire Dashboard running via docker.

Add the following code to your program.cs file

```csharp
var sName = "Application Name";
builder.AddCommonOTelLogging(() => ResourceBuilder.CreateDefault().AddService($"{sName} {builder.Configuration.GetValue<string>("env")}"));
builder.AddCommonOTelMonitoring($"{sName} {builder.Configuration.GetValue<string>("env")}", builder.Configuration.GetValue<string>("BuildNumber"), sName);
builder.AddOldOpenTel();
```

Add the following settings to your appsettings.json file

```json 
"AspireDashboard": "http://localhost:18889/",//The URL to send data to the Aspire Dashboard
"env": "Dev",//The environment of your application
"BuildNumber": "",//The build or version number of your application
```

To run the Aspire Dashboard from a docker compose file you can use the following code

```yaml
  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/nightly/aspire-dashboard:8.0.0-preview.5
    container_name: aspire-dashboard
    expose:
      - "18888"
      - "18889"
    ports:
      - 18889:18889
```

- localhost:18888 will be the URL of the Aspire Dashboard
- localhost:18889 will be the URL your application uses to send OpenTelemetry data to the Aspire Dashboard
