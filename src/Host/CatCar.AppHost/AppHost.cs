var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL 17 container with persistent data volume
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("catcar-postgres-data")
    .WithPgAdmin();

// Single database — schemas are created by EF Core migrations
var catcarDb = postgres.AddDatabase("catcar");

// CatCar API project — the web application
var api = builder.AddProject<Projects.CatCar_Api>("api")
    .WithHttpEndpoint()
    .WithReference(catcarDb)
    .WaitFor(catcarDb)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

builder.Build().Run();
