using PrivateListings.Application;
using PrivateListings.Infrastructure;
using PrivateListings.Infrastructure.Configuration;
using PrivateListings.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
PrivateListingsProductionConfigurationValidator.Validate(
    builder.Configuration,
    builder.Environment,
    validateAllowedHosts: false);
builder.Services
    .AddPrivateListingsApplication()
    .AddPrivateListingsWorkerInfrastructure(builder.Configuration, builder.Environment);
builder.Services.Configure<ListingExpirationWorkerOptions>(
    builder.Configuration.GetSection(ListingExpirationWorkerOptions.SectionName));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
