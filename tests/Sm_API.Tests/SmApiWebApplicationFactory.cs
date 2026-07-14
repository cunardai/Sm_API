using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sm_API.Api.Data;

namespace Sm_API.Tests;

// Uses a real SQLite in-memory connection (not the EF InMemory provider) so
// constraints, unique indexes, and cascade behavior match production exactly.
public class SmApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SmApiWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SmApiDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<SmApiDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<SmApiDbContext>>();

            services.AddDbContext<SmApiDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
