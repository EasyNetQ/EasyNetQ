﻿using System;
using System.IO;
using System.Reflection;
using EasyNetQ.Management.Client.Serialization;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Tests
{
    public class ResourceLoader
    {
        /// <summary>
        /// Loads an embedded resource 
        /// </summary>
        /// <param name="fileToLoad"></param>
        /// <returns>The contents as a string</returns>
        public static T LoadObjectFromJson<T>(string fileToLoad)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new RabbitContractResolver(),
            };

            settings.Converters.Add(new PropertyConverter());
            return LoadObjectFromJson<T>(fileToLoad, settings);
        }
        public static T LoadObjectFromJson<T>(string fileToLoad, JsonSerializerSettings settings)
        {
            const string namespaceFormat = "EasyNetQ.Management.Client.Tests.Json.{0}";
            var resourceName = string.Format(namespaceFormat, fileToLoad);
            var assembly = Assembly.GetExecutingAssembly();
            string contents;
            using(var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ApplicationException("Couldn't load resource stream: " + resourceName);
                }
                using (var reader = new StreamReader(resourceStream))
                {
                    contents = reader.ReadToEnd();
                }
            }

            

            return JsonConvert.DeserializeObject<T>(contents, settings);
        }     
    }
}