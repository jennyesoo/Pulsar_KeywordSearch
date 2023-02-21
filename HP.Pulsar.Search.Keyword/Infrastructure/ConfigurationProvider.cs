using HP.Pulsar.Search.Keyword.Orchestrator;
using LemmaSharp.Classes;
using Microsoft.Extensions.Configuration;

namespace HP.Pulsar.Search.Keyword.Infrastructure
{
    internal static class ConfigurationProvider
    {
        public static IConfiguration GetConfiguration(PulsarEnvironment env)
        {
            // If this file is missing, runtime will throw exception.
            ConfigurationBuilder builder = new();
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            string Path = dir.Parent.Parent.Parent.Parent.FullName;

            if (env == PulsarEnvironment.Production)
            {
                builder.AddJsonFile($@"{Path}\HP.Pulsar.Search.Keyword\AppData\app.Production.json");
            }
            else
            {
                builder.AddJsonFile($@"{Path}\HP.Pulsar.Search.Keyword\AppData\app.Beta.json");
            }

            return builder.Build();
        }
    }
}

