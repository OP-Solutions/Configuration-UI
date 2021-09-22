﻿using ConfigurationUi.Abstractions;
using ConfigurationUi.Middlewares;
using ConfigurationUi.Options.Builder;
using ConfigurationUi.Ui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfigurationUi.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddConfigurationUi<TConfigModel>(this IHostBuilder hostBuilder, string filePath)
        {
            
            hostBuilder.ConfigureAppConfiguration((_, builder) =>
            {
                var optionsBuilder = new ConfigurationUiOptionsBuilder();
                optionsBuilder.UseJsonFileStorage(filePath, builder.GetFileProvider())
                    .WithSchemeFromType<TConfigModel>();
                builder.Sources.Add(new ChainedConfigurationSource()
                {
                    Configuration = optionsBuilder.Options.StorageProvider.Configuration
                });

                hostBuilder.ConfigureServices((_, services) =>
                {
                    services.AddSingleton(optionsBuilder.Options);
                    services.AddSingleton<IEditorUiBuilder, UiBuilder>();
                    services.AddSingleton<ConfigurationUiMiddleware>();
                });
            });

            

            return hostBuilder;
        }
    }
}