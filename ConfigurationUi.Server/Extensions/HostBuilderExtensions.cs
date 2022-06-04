﻿using ConfigurationUi.Server.Api.Controllers;
using ConfigurationUi.Server.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ConfigurationUi.Server.Extensions
{
    /// <summary>
    /// Extensions for <see cref="IHostBuilder"/>
    /// </summary>
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddConfigurationUi(this IHostBuilder hostBuilder, string filePath, string configApiPath)
        {

            IFileProvider? configurationFileProvider = null;

            hostBuilder.ConfigureAppConfiguration((_, builder) =>
            {
                configurationFileProvider = builder.GetFileProvider();
                builder.Sources.Add(new JsonConfigurationSource { Path = filePath, ReloadOnChange = true });
            });

            hostBuilder.ConfigureServices((_, services) =>
            {
                services.Configure<ConfigurationUiServerOptions>(options =>
                {
                    options.ConfigApiPath = configApiPath;
                    options.UseJsonFileStorage(filePath, configurationFileProvider!);
                });
                services.AddControllers().AddApplicationPart(typeof(ConfigurationController).Assembly);
            });

            return hostBuilder;
        }
    }
}