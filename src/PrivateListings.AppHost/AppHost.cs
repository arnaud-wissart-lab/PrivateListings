var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("privatelistingsdb");
var mailpit = builder.AddMailPit("mailpit");

builder.AddProject<Projects.PrivateListings_Web>("web")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(mailpit)
    .WaitFor(mailpit);

builder.AddProject<Projects.PrivateListings_Worker>("worker")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(mailpit)
    .WaitFor(mailpit);

builder.Build().Run();
