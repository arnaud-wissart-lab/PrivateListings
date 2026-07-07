using PrivateListings.Application;
using PrivateListings.Infrastructure;
using PrivateListings.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services
    .AddPrivateListingsApplication()
    .AddPrivateListingsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
