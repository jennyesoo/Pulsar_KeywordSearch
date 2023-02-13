using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Extensions.Configuration;
using ConfigurationProvider = HP.Pulsar.Search.Keyword.Infrastructure.ConfigurationProvider;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class ConnectionStringProvider
{
    private readonly IConfiguration _config;

    public ConnectionStringProvider(PulsarEnvironment env)
    {
        _config = ConfigurationProvider.GetConfiguration(env);
    }

    public string GetSqlServerConnectionString()
    {
        return _config["DatabaseConnectionString"];
    }
}
