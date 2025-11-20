using System.Data;
using LdesServer;
using LdesServer.Administration.Initializer;
using LdesServer.Administration.Interfaces;
using LdesServer.Administration.Services;
using LdesServer.Core.Interfaces;
using LdesServer.Bucketization;
using LdesServer.Core.InputFormatters;
using LdesServer.Core.OutputFormatters;
using LdesServer.Core.Models.Configuration;
using LdesServer.Serving;
using LdesServer.Serving.Services;
using LdesServer.Fragmentation;
using LdesServer.Ingestion;
using LdesServer.Ingestion.Algorithms;
using LdesServer.Ingestion.Services;
using LdesServer.Pagination;
using LdesServer.Storage.Postgres.Initializer;
using LdesServer.Storage.Postgres.Repositories;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Npgsql;
using Prometheus;
using VDS.RDF;

UriFactory.InternUris = false; // disable interning

var builder = WebApplication.CreateBuilder(args);

var ldesServerConfigSection = builder.Configuration.GetSection("LdesServer");
var ldesServerConfig = ldesServerConfigSection.Get<LdesServerConfiguration>() ?? new LdesServerConfiguration();

var bucketizationConfigSection = ldesServerConfigSection.GetSection("Bucketization");
var paginationConfigSection = ldesServerConfigSection.GetSection("Pagination");
var servingConfigSection = ldesServerConfigSection.GetSection("Serving");

builder.Services
    .AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
    .AddControllers()
    .AddControllersAsServices()
    .AddMvcOptions(options =>
    {
        options.InputFormatters.Insert(0, new StreamInputFormatter());

        var defaultOutputFormatters = options.OutputFormatters.ToList();
        options.OutputFormatters.Clear();
        options.OutputFormatters.Add(new TurtleOutputFormatter());
        options.OutputFormatters.Add(new TriGOutputFormatter());
        options.OutputFormatters.Add(new JsonLdOutputFormatter());
        options.OutputFormatters.Add(new NodeAsTreeProfileOutputFormatter());
        options.OutputFormatters.Add(new NQuadsOutputFormatter());
        options.OutputFormatters.Add(new NTriplesOutputFormatter());
        defaultOutputFormatters.ForEach(x => options.OutputFormatters.Add(x));
    });

builder.Services.AddHealthChecks();

builder.Services
    .Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; })
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails()
    .AddNpgsqlDataSource(builder.Configuration.GetConnectionString("Postgres")!)
    .AddTransient<IDbConnection, NpgsqlConnection>(sp =>
    {
        var connection = sp.GetRequiredService<NpgsqlConnection>();
        connection.Open();
        return connection;
    })
    .AddSingleton<IStorageInitializer, StorageInitializer>()
    // repositories (state-less)
    .AddSingleton<ICollectionRepository, CollectionRepository>()
    .AddSingleton<IViewRepository, ViewRepository>()
    .AddSingleton<IMemberRepository, MemberRepository>()
    .AddSingleton<IBucketRepository, BucketRepository>()
    .AddSingleton<IPageRepository, PageRepository>()
    .AddSingleton<IStatisticsRepository, StatisticsRepository>()
    // server
    .AddSingleton(ldesServerConfig)
    .AddSingleton<LinkedDataReader>()
    // administration
    .AddTransient<ICollectionService, CollectionService>()
    .AddTransient<IViewService, ViewService>()
    .AddSingleton<DefinitionsInitializer>()
    // ingestion
    .AddSingleton<IIngestAlgorithmFactory, IngestAlgorithmFactory>()
    .AddScoped<MemberService>()
    // bucketization
    .AddSingleton<DefaultBucketizer>()
    .AddSingleton<TimeBucketizer>()
    .AddSingleton<MemberBucketizerConfiguration>(_ =>
        bucketizationConfigSection.Get<MemberBucketizerConfiguration>() ?? new MemberBucketizerConfiguration())
    .AddTransient<MemberBucketizer>()
    .AddTransient<BucketizerService>()
    .AddHostedService<BucketizerBackgroundService>()
    // pagination
    .AddSingleton<BucketPaginatorConfiguration>(_ =>
        paginationConfigSection.Get<BucketPaginatorConfiguration>() ?? new BucketPaginatorConfiguration())
    .AddTransient<BucketPaginator>()
    .AddTransient<PaginationService>()
    .AddHostedService<PaginationBackgroundService>()
    // fetching
    .AddSingleton<ServingConfiguration>(_ =>
        servingConfigSection.Get<ServingConfiguration>() ?? new ServingConfiguration())
    .AddScoped<NodeService>()
    // web api
    .AddSingleton<ILogger>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("LDES Server .NET"));

var app = builder.Build();
app.UseExceptionHandler();

var isDevelopment = app.Environment.IsDevelopment();
var storageInitializer = app.Services.GetRequiredService<IStorageInitializer>();
var done = await storageInitializer.InitializeAsync(isDevelopment);
if (!done) return;

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapHealthChecks("/health");

app.RegisterIngestionMetrics();
app.RegisterBucketizationMetrics();
app.RegisterPaginationMetrics();
app.UseMetricServer();

app.MapControllers();

// seed definitions
if (ldesServerConfig.DefinitionsDirectory != null)
{
    var initializer = app.Services.GetRequiredService<DefinitionsInitializer>();
    var paths = Directory
        .GetFiles(ldesServerConfig.DefinitionsDirectory, "*", SearchOption.AllDirectories);
    await initializer.Seed(paths.Select(x => new FileInfo(x)));
}

app.Run();