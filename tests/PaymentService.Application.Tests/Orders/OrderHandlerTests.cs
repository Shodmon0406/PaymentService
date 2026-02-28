using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Application.Tests.Orders;

public class OrderHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public OrderHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }
    
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();

        GC.SuppressFinalize(this);
    }
}