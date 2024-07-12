using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AspireDashboardHelper;

/// <summary>
/// Extension Methods for setting up OpenTelemetry Logging and Monitoring that can be consumed by an Aspire Dashboard
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Set Up OpenTelemetry Logging
    /// </summary>
    /// <param name="builder"></param>
    public static void AddOldOpenTel(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }
                tracing.AddAspNetCoreInstrumentation()
                           .AddHttpClientInstrumentation();
            });
        if (!string.IsNullOrEmpty(builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString")))
        {
            builder.Services.AddOpenTelemetry()
                   .UseAzureMonitor(x =>
                   {
                       x.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
                   });
        }
    }

    /// <summary>
    /// Set Up OpenTelemetry Logging
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="resourceBuilderFunc"></param>
    public static void AddCommonOTelLogging(this WebApplicationBuilder builder, Func<ResourceBuilder> resourceBuilderFunc)
    {
        builder.Logging.ClearProviders();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;

            options.SetResourceBuilder(resourceBuilderFunc());

            options.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("AspireDashboard") ?? string.Empty);
            });
        });
    }

    /// <summary>
    /// Set Up OpenTelemetry Monitoring
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="ServiceName"></param>
    /// <param name="ServiceVersion"></param>
    /// <param name="ActivitySourceName"></param>
    public static void AddCommonOTelMonitoring(this WebApplicationBuilder builder, string ServiceName, string ServiceVersion, string ActivitySourceName)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(ServiceName, serviceVersion: ServiceVersion);
                r.AddAttributes(new Dictionary<string, object>
                {
                    ["host.name"] = Environment.MachineName,
                    ["app.name"] = ServiceName,
                    ["cluster.name"] = "test",
                    ["namespace"] = "test",
                    ["deployment.environment"] = builder.Configuration.GetValue<string>("env") ?? string.Empty,
                    ["deployment.version"] = ServiceVersion,
                });
            })
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .AddSource(ActivitySourceName)
                    .SetErrorStatusOnException()
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            string cmdText = command.CommandText;
                            if (cmdText.StartsWith("-- "))
                            {
                                var lengthOfTags = cmdText.IndexOf("\r\n\r\n");
                                if (lengthOfTags > 0)
                                {
                                    string tag = cmdText.Substring(0, lengthOfTags);
                                    activity.SetTag("db.tag", tag.Replace("-- ", ""));
                                }
                            }
                        };
                    })
                    .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("AspireDashboard") ?? string.Empty));
            })
        .WithMetrics(m =>
        {
            m.AddPrometheusExporter();
            m.AddAspNetCoreInstrumentation()
                .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("AspireDashboard") ?? string.Empty));
        });
    }
}
