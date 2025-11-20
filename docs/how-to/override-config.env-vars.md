# Override Configuration Settings Using Environment Variables

This guide shows you how to **change the default settings** of the LDES server when deployed using [Docker Compose](/docker-compose.yml) using **environment variables**.

For an overview of the configuration system see [LDES Server Configuration](../discussions/configuration.md).  
You can also override the default settings [using a custom configuration file](./override-config.custom-file.md).

If you want to **override default settings using environment variables**, you need to:

1. In the Docker compose file, under the `environment` attribute for the LDES server service, add each setting you want to override. You need to name the environment variable using the structured, nested name with each sub-structure's name separated with a colon (`:`), and set the environment value to the required setting.  
   
   E.g. to change the logging level for the ASP.NET Core infrastructure, add the `Logging:LogLevel:Microsoft.AspNetCore` environment variable to the [Docker compose](/docker-compose.yml) file:
   ```yaml
   services:
     ldes-server:
       image: rorlic/ldes-server:latest
       environment:
         - Logging:LogLevel:Microsoft.AspNetCore=Information  # override the default (Warning) level
   ```

2. Restart the LDES server for changes to take effect:
   ```bash
   docker compose rm -s -f ldes-server # stop and remove LDES server service without confirmation and
   docker compose up -d --wait --no-deps ldes-server # create and start LDES server service only
   ```
