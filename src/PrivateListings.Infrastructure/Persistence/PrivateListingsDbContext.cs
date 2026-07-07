using Microsoft.EntityFrameworkCore;

namespace PrivateListings.Infrastructure.Persistence;

public sealed class PrivateListingsDbContext(DbContextOptions<PrivateListingsDbContext> options) : DbContext(options);
