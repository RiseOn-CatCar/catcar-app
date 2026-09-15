using System.Security.Cryptography;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var catcarDb = postgres.AddDatabase("catcar");

builder.AddProject<Projects.CatCar_Api>("api")
    .WithHttpEndpoint()
    .WithExternalHttpEndpoints()
    .WithReference(catcarDb)
    .WaitFor(catcarDb)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Jwt__Secret", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

builder.Build().Run();
