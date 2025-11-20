# Setting Up a Minimal LDES Server

This tutorial will show you how to set up a minimal **LDES server** to accept linked data members.

## Enter the LDES Server

The [LDES-Server.NET](https://github.com/rorlic/ldes-server.net) is designed to accept dataset members, store them in external storage, and expose them as a **Linked Data Event Stream (LDES)**. This allows clients to replicate the full dataset or retrieve subsets as needed.

The real power of LDES lies in its ability to let data clients **stay in sync** with any updates to a dataset.

Clients are often interested not just in the *current* state of data, but also in its *evolution over time*. For instance, to analyze air quality trends in a city, one needs access to sensor readings collected across different moments.

The LDES server expects incoming data to be either **state objects** — representing an entity’s current state — or **version objects** — capturing an entity’s state at a specific point in time. This structure allows you to trace how an object evolves from its initial version to its most recent, and, by staying synchronized, even capture future updates as they occur.

Pretty cool, right?

> [!NOTE]
> Please make sure you have installed all the [prerequisites](/README.md#prerequisites) as described in the [top-level README](/README.md) file.

## Where Do We Keep This Gem?

Each **Data Publisher** manages and publishes its own dataset. The LDES-Server.NET is distributed as a **Docker image** that can be configured to suit your deployment needs.

Pre-built images are available on [Docker Hub](https://hub.docker.com/r/rorlic/ldes-server), with [stable releases](https://hub.docker.com/r/rorlic/ldes-server/tags) versioned according to [Semantic Versioning](https://semver.org/).

You can expect backward-compatible updates to keep the same **major** version number, while **minor** versions introduce new features. Occasional **patch releases** (with a non-zero patch number) include important or urgent fixes.

## Set Up the Basics

You’ll need some familiarity with **Docker Compose**, since we’ll use it to run both the **LDES server** and its **database** as containers.

Currently, only [PostgreSQL](https://www.postgresql.org/) is supported for storing LDES members and metadata. Official PostgreSQL images can be found on [Docker Hub](https://hub.docker.com/_/postgres), along with their available [tags](https://hub.docker.com/_/postgres/tags). Support for additional databases may be added in the future.

In the provided [Docker Compose file](./docker-compose.yml), you’ll see a **private network** connecting two services: the LDES server and the PostgreSQL database. This allows the server to reference the database by its service name, `postgres`.

The server runs internally on port **80**, which we map to **8080** on the host machine, making it accessible via [`http://localhost:8080`](http://localhost:8080). The Compose file defines which images and tags to use (acting as a “template” for container creation) and ensures the LDES server starts **after** PostgreSQL by defining a dependency.

You’ll also find several **environment variables** defined in the Compose file. The key one is:

```
ConnectionStrings:Postgres=Host=<server>;Port=<port>;Database=<database>
```

Where:

- `<server>` = `postgres` (the database service name)  
- `<port>` = `5432` (the default PostgreSQL port)  
- `<database>` = `minimal-server` (the database name)

The `LdesServer:BaseUri` variable defines the **external base path** of the LDES server, ensuring that links within the stream resolve correctly.  
We set this to `http://localhost:8080/feed/`, as the server serves all defined LDES instances under the `/feed` subpath.

> [!NOTE]
> Database credentials (`POSTGRES_USER` and `POSTGRES_PASSWORD`) are provided via environment variables. Default values are defined in the [`.env`](./.env) file, which Docker automatically loads.  
> You can also supply credentials through other methods; see the [Docker documentation on environment variables](https://docs.docker.com/compose/environment-variables/) for details.

Ready for some action? Let’s roll up our sleeves and dive in!

## Systems Ready? 3, 2, 1... Ignition!

Enough theory — let’s get the LDES server running.

To execute the commands below, use a [Bash shell](https://en.wikipedia.org/wiki/Bash_(Unix_shell)). Bash ensures consistent behavior across Linux, Windows, and macOS.  
If you cloned this repository locally, you already have Bash available via your Git installation.

> [!WARNING]
> Please make sure you open a Bash shell in the directory where this tutorial is located.

Start the LDES server and PostgreSQL containers with:

```bash
clear
docker compose up -d --wait
```

## Defining Our First LDES

Once the containers are running, we need to tell the LDES server which dataset to store.  
The server can host multiple datasets, each defined via its **administration API**.

For now, we’ll send the [LDES definition](./definitions/occupancy.ttl) to the API so we can store [some data](./data/member.ttl) in our new dataset.

```bash
curl -X POST -H "content-type: text/turtle" "http://localhost:8080/admin/api/v1/collection" -d "@./definitions/occupancy.ttl"
```

At first glance, this might look a little mystical — but it’s actually straightforward.  
The [LDES definition](./definitions/occupancy.ttl) is a [Turtle](https://www.w3.org/TR/turtle/) file, a serialization format for [RDF](https://www.w3.org/RDF/).

The file starts with a few prefix declarations for readability, followed by the actual LDES definition:  
we define `<occupancy> a ldes:EventStream` and instruct the server to make it available at `/feed/occupancy`.

Next, we tell the server to create **version objects** for our data. Each version is linked to its base entity using `dcterms:isVersionOf` and timestamped with `prov:generatedAtTime`.  
This lets us group members by entity and order them chronologically — so we can track the evolution of data over time.

To confirm the LDES is registered, fetch it from the server:

```bash
curl "http://localhost:8080/feed/occupancy"
```

The LDES Server automatically creates a default view named `_` — the event source — which will contain any members added to the collection. So, let’s go ahead and add some members!

## Storing Our First Member

With the LDES defined, we can now send our first member to it.  
Members are ingested through the `/data/{collection-name}` subpath.

```bash
curl -X POST -H "content-type: text/turtle" "http://localhost:8080/data/occupancy" -d "@./data/member.ttl"
```

> [!NOTE]
> Members are serialized as [Linked Data](https://www.w3.org/standards/semanticweb/data) using the Turtle format (`.ttl`, MIME type `text/turtle`).  
> The LDES server also supports other RDF serialization formats, each identified by its own MIME type.

## Show Me the Data!

That’s it! 🎉  
You might need to wait a short moment before the members appear — processing happens asynchronously in the background.

Retrieve your dataset with:

```bash
curl "http://localhost:8080/feed/occupancy/_"
```


## All Good Things Must Come to an End

Congratulations — you’ve mastered the essentials of the LDES server!  
You now know what it is, how to set it up, and how to publish and retrieve data.  
All that’s left is to stop the server and clean up.

To bring down the containers and remove the private network:

```bash
docker compose down
```

✨ **You’re ready for the next step!**  
Now that you’ve created and published your first LDES, it’s time to explore more advanced configurations.
