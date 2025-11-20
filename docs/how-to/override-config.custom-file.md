# Override Configuration Settings Using Custom Configuration File

This guide shows you how to **change the default settings** of the LDES server when deployed using [Docker Compose](/docker-compose.yml) using a **custom configuration file**.

For an overview of the configuration system see [LDES Server Configuration](../discussions/configuration.md).  
You can also override the default settings [using environment variables](./override-config.env-vars.md).

If you want to **override default settings using a custom configuration file**, you need to:

1. Choose a name for your custom environment, e.g. `staging`, `production`, or `development`.

2. Set the `ASPNETCORE_ENVIRONMENT` variable in your Docker Compose file:
   ```yaml
   services:
     ldes-server:
       image: rorlic/ldes-server:latest
       environment:
         - ASPNETCORE_ENVIRONMENT=staging
   ```
> [!TIP]
> Alternatively, you can export the variable in your shell before deploying the systems as the [docker compose](/docker-compose.yml) file already allows overriding this environment variable:
> ```bash
> export ASPNETCORE_ENVIRONMENT=staging
> docker compose up -d
> ```   

3. Create a configuration file named `appsettings.{environment}.json` (e.g., `appsettings.staging.json`), containing only the settings you want to override, structured the same way as the default `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.AspNetCore": "Information",
         "Npgsql.Command": "Information"
       }
     },
     "LdesServer": {
       "BaseUri": "https://staging.example.org/"
     }
   }
   ```

4. Mount the custom configuration file into the container by adding a volume mapping to your Docker compose file:
   ```yaml
   services:
     ldes-server:
       image: rorlic/ldes-server:latest
       environment:
         - ASPNETCORE_ENVIRONMENT=staging
       volumes:
         - ./appsettings.staging.json:/ldes-server/appsettings.staging.json:ro
   ```
   Where:
   - `./appsettings.staging.json` is the path to your local file
   - `/ldes-server/appsettings.staging.json` is the path inside the container
   - `:ro` means read-only (recommended for configuration files)

5. Restart the LDES server for changes to take effect:
   ```bash
   docker compose rm -s -f ldes-server # stop and remove LDES server service without confirmation and
   docker compose up -d --wait --no-deps ldes-server # create and start LDES server service only
   ```
