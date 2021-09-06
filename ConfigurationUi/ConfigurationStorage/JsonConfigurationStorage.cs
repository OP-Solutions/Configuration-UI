﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ConfigurationUi.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace ConfigurationUi.ConfigurationStorage
{
    internal class JsonConfigurationStorage : IConfigurationStorage
    {
        private readonly string _filePath;
        private IConfiguration _configuration;

        public JsonConfigurationStorage(string filePath, IFileProvider fileProvider)
        {
            _filePath = filePath;
            
            var fileInfo = fileProvider.GetFileInfo(_filePath);
            if (!fileInfo.Exists)
            {
                var streamWriter = File.CreateText(fileInfo.PhysicalPath);
                streamWriter.WriteLine("{}"); // write empty json object initially
                streamWriter.Flush();
                streamWriter.Close();
            }
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetFileProvider(fileProvider);
            _configuration = configurationBuilder.AddJsonFile(_filePath).Build();
        }

        public Task<IConfiguration> ReadConfigurationAsync()
        {
            return Task.FromResult(_configuration);
        }

        public async Task WriteConfigurationAsync(IConfiguration configuration, JsonSchema schema)
        {
            var jToken = GetJTokenFromConfiguration(configuration, schema);
            await using var streamWriter = new StreamWriter(_filePath);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            await jToken.WriteToAsync(jsonWriter);
            await jsonWriter.FlushAsync();
        }

        private JToken GetJTokenFromConfiguration(IConfiguration configuration, JsonSchema schema)
        {
            if (schema.Type == JsonObjectType.Object)
            {
                var objectToken = new JObject();
                foreach (var propertyConfig in configuration.GetChildren())
                {
                    var propertyToken = GetJTokenFromConfiguration(propertyConfig, schema.Properties[propertyConfig.Key]);
                    objectToken[propertyConfig.Key] = propertyToken;
                }
                
                return objectToken;
            }
            
            if (schema.Type == JsonObjectType.Array)
            {
                var arrayToken = new JArray();
                var elemSchema = schema.Item;
                    
                foreach (var elemConfig in configuration.GetChildren())
                {
                    var elemToken = GetJTokenFromConfiguration(elemConfig, elemSchema);
                    arrayToken.Add(elemToken);
                }

                return arrayToken;
            }

            var section = configuration as IConfigurationSection;
            Debug.Assert(section != null, nameof(section) + " != null");

            var value = section.Value;
            
            if (value == null) return JValue.CreateNull();

            return schema.Type switch
            {
                JsonObjectType.String => new JValue(value),
                JsonObjectType.Boolean => new JValue(bool.Parse(value)),
                JsonObjectType.Integer => new JValue(long.Parse(value)),
                JsonObjectType.Number => new JValue(decimal.Parse(value)),
                _ => throw new NotSupportedException($"unsupported json token type {schema.Type}")
            };
        }

    }
}