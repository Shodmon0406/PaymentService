using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Features.Payments.Commands.CreatePayment;
using PaymentService.Application.Features.Payments.Queries.GetPaymentsByOrder;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Orders;
using PaymentService.Domain.Entities.Users;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Application.Tests.Payments;

public class PaymentHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public PaymentHandlerTests()
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

    private async Task<User> CreateUserAsync(
        string phone = "+992123456789",
        string email = "test@mail.com",
        string fullName = "Test User")
    {
        var ctx = CreateContext();

        var user = User.Register(phone, email, fullName, "Password1").Value;
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        return user;
    }

    private async Task<Order> CreateOrderAsync(Guid userId, decimal amount = 100m, string currency = "TJS")
    {
        var ctx = CreateContext();

        var order = Order.Create(userId, amount, currency).Value;
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        return order;
    }

    public void Dispose()
    {
        _connection.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreatePayment_ShouldSucceed()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();
        var order = await CreateOrderAsync(user.Id);

        var handler = new CreatePaymentCommandHandler(ctx);

        // Act
        var result = await handler.Handle(new CreatePaymentCommand(user.Id, order.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paymentDto = result.Value;
        paymentDto.Id.Should().NotBeEmpty();
        paymentDto.OrderId.Should().Be(order.Id);
        paymentDto.UserId.Should().Be(user.Id);
        paymentDto.Amount.Should().Be(order.Amount);
        paymentDto.Currency.Should().Be(order.Currency);
        paymentDto.Status.Should().Be(Domain.Enums.Payments.PaymentStatus.Pending);
        paymentDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreatePayment_ForPaidOrder_ShouldReturnConflict()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();
        var order = await CreateOrderAsync(user.Id);

        var dbOrder = await ctx.Orders.FindAsync(order.Id);
        dbOrder!.MarkAsPaid();
        await ctx.SaveChangesAsync();

        var handler = new CreatePaymentCommandHandler(ctx);

        // Act
        var result = await handler.Handle(new CreatePaymentCommand(user.Id, order.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreatePayment_ForCancelledOrder_ShouldReturnConflict()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();
        var order = await CreateOrderAsync(user.Id);

        var dbOrder = await ctx.Orders.FindAsync(order.Id);
        dbOrder!.MarkAsCancelled();
        await ctx.SaveChangesAsync();

        var handler = new CreatePaymentCommandHandler(ctx);

        // Act
        var result = await handler.Handle(new CreatePaymentCommand(user.Id, order.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreatePayment_ForNonExistingOrder_ShouldFail()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();

        var handler = new CreatePaymentCommandHandler(ctx);

        // Act
        var result = await handler.Handle(new CreatePaymentCommand(user.Id, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CreatePayment_ForOrderOfAnotherUser_ShouldFail()
    {
        // Arrange
        var ctx = CreateContext();
        var user1 = await CreateUserAsync(phone: "+992123456780", email: "test1@mail.com");
        var user2 = await CreateUserAsync(phone: "+992123456781", email: "test2@mail.com");
        var order = await CreateOrderAsync(user1.Id);

        var handler = new CreatePaymentCommandHandler(ctx);

        // Act
        var result = await handler.Handle(new CreatePaymentCommand(user2.Id, order.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetPaymentsByOrder_WithExistingPayments_ShouldReturnOrderedByCreatedAtDesc()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();
        var order = await CreateOrderAsync(user.Id);

        var handler = new CreatePaymentCommandHandler(ctx);

        for (var i = 0; i < 3; i++)
        {
            await handler.Handle(new CreatePaymentCommand(user.Id, order.Id), CancellationToken.None);
            await Task.Delay(100);
        }

        var queryHandler = new GetPaymentsByOrderQueryHandler(ctx);

        // Act
        var result = await queryHandler.Handle(new GetPaymentsByOrderQuery(user.Id, order.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var payments = result.Value;
        payments.Should().HaveCount(3);
        payments[0].CreatedAt.Should().BeAfter(payments[1].CreatedAt);
        payments[1].CreatedAt.Should().BeAfter(payments[2].CreatedAt);
    }

    [Fact]
    public async Task GetPaymentsByOrder_ForAnotherUsersOrder_ShouldReturnNotFound()
    {
        // Arrange
        var ctx = CreateContext();
        var user1 = await CreateUserAsync(phone: "+992123456780", email: "test1@mail.com");
        var user2 = await CreateUserAsync(phone: "+992123456781", email: "test2@mail.com");
        var order = await CreateOrderAsync(user1.Id);

        var queryHandler = new GetPaymentsByOrderQueryHandler(ctx);

        // Act
        var result = await queryHandler.Handle(new GetPaymentsByOrderQuery(user2.Id, order.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetPaymentsByOrder_ForNonExistingOrder_ShouldReturnNotFound()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();

        var queryHandler = new GetPaymentsByOrderQueryHandler(ctx);

        // Act
        var result = await queryHandler.Handle(new GetPaymentsByOrderQuery(user.Id, Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}