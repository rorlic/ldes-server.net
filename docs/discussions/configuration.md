# Configuration

The LDES server is an ASP.NET Core web application and uses a configuration file named `appsettings.json`, which can be found in the Docker container at `/ldes-server/appsettings.json`. This [configuration file](/src/ldes-server/appsettings.json) contains a nested JSON structure with settings for both the LDES server and the underlying ASP.NET technology. All LDES server specific configuration can be found under the `LdesServer` section:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error",
      "Microsoft.Hosting": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Npgsql.Command": "Warning",
      "LdesServer": "Information"
    }
  },
  "ConnectionStrings": {
    "Postgres": ""
  },
  "LdesServer": {
    "BaseUri": "http://localhost:8080/",
    "CreateEventSource": true
  }
}
```

The LDES server provides flexible configuration through ASP.NET Core's standard configuration system. You can override default settings in two ways:

- **Environment variables**: Best for overriding a few specific settings - see [here](../how-to/override-config.env-vars.md) for details
- **Custom configuration file**: Best for overriding many settings - see [here](../how-to/override-config.custom-file.md) for how to do this

Using a custom configuration file is also useful when:
- You want to manage different environments (development, staging, production)
- You want to version control your configuration changes
