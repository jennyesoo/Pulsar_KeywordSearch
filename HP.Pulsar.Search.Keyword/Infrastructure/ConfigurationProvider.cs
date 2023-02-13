using Microsoft.Extensions.Configuration;

namespace HP.Pulsar.Search.Keyword.Infrastructure
{
    internal static class ConfigurationProvider
    {
        public static IConfiguration GetConfiguration(PulsarEnvironment env)
        {
            // If this file is missing, runtime will throw exception.
            ConfigurationBuilder builder = new();

            if (env == PulsarEnvironment.Production)
            {
                builder.AddJsonFile($@".\app.Production.json");
            }
            else
            {
                builder.AddJsonFile($@".\app.Beta.json");
            }

            return builder.Build();
        }
    }
}
