using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PaymentService.Application.Features.Orders.Commands.CreateOrder;
using PaymentService.Application.Features.Orders.Queries.GetOrderById;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Users;
using PaymentService.Domain.Enums.Orders;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Application.Tests.Orders;

public class OrderHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<CreateOrderCommandHandler> _createLogger;
    private readonly ILogger<GetOrderByIdQueryHandler> _getOrderByIdQueryLogger;

    public OrderHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
        
        _createLogger = Substitute.For<ILogger<CreateOrderCommandHandler>>();
        _getOrderByIdQueryLogger = Substitute.For<ILogger<GetOrderByIdQueryHandler>>();
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

    private async Task<User> CreateUserAsync(
        string phone = "+992123456789",
        string email = "test@example.com",
        string fullName = "Test User")
    {
        var ctx = CreateContext();
        
        var user = User.Register(phone, email, fullName, "Password1").Value;
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        
        return user;
    }

    [Fact]
    public async Task CreateOrder_ShouldSucceed()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();

        var handler = new CreateOrderCommandHandler(ctx, _createLogger);

        // Act
        var result = await handler.Handle(new CreateOrderCommand(user.Id, 100m, "TJS", Guid.NewGuid().ToString()),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var orderDto = result.Value;
        orderDto.Id.Should().NotBeEmpty();
        orderDto.UserId.Should().Be(user.Id);
        orderDto.Status.Should().Be(OrderStatus.Created);
        orderDto.Amount.Should().Be(100m);
        orderDto.Currency.Should().Be("TJS");
        orderDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var order = await ctx.Orders.FirstOrDefaultAsync();
        order.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Created);
        order.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateOrder_WithNegativeAmount_ShouldFail()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();

        var handler = new CreateOrderCommandHandler(ctx, _createLogger);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(user.Id, -50m, "TJS", Guid.NewGuid().ToString()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidCurrency_ShouldFail()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();
        var handler = new CreateOrderCommandHandler(ctx, _createLogger);

        // Act
        var result = await handler.Handle(new CreateOrderCommand(user.Id, 100m, "INVALID", Guid.NewGuid().ToString()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOrder()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();

        var createHandler = new CreateOrderCommandHandler(ctx, _createLogger);
        var createResult =
            await createHandler.Handle(new CreateOrderCommand(user.Id, 100m, "TJS", Guid.NewGuid().ToString()), CancellationToken.None);
        var orderId = createResult.Value.Id;

        var getHandler = new GetOrderByIdQueryHandler(ctx, _getOrderByIdQueryLogger);

        // Act
        var getResult = await getHandler.Handle(new GetOrderByIdQuery(orderId, user.Id), CancellationToken.None);

        // Assert
        getResult.IsSuccess.Should().BeTrue();
        var orderDto = getResult.Value;
        orderDto.Id.Should().Be(orderId);
        orderDto.UserId.Should().Be(user.Id);
        orderDto.Status.Should().Be(OrderStatus.Created);
        orderDto.Amount.Should().Be(100m);
        orderDto.Currency.Should().Be("TJS");
        orderDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetOrderById_WithNonExistentOrder_ShouldFail()
    {
        // Arrange
        var ctx = CreateContext();
        var user = await CreateUserAsync();
        var getHandler = new GetOrderByIdQueryHandler(ctx, _getOrderByIdQueryLogger);

        // Act
        var getResult = await getHandler.Handle(new GetOrderByIdQuery(Guid.NewGuid(), user.Id),
            CancellationToken.None);

        // Assert
        getResult.IsFailure.Should().BeTrue();
        getResult.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetOrderById_WithWrongUser_ShouldFail()
    {
        // Arrange
        var ctx = CreateContext();
        var user1 = await CreateUserAsync("+992123456789", "test1@mail.com", "Test User 1");
        var user2 = await CreateUserAsync("+992987654321", "test2@mail.com", "Test User 2");

        var createHandler = new CreateOrderCommandHandler(ctx, _createLogger);
        var createResult = await createHandler.Handle(new CreateOrderCommand(user1.Id, 100m, "TJS", Guid.NewGuid().ToString()), CancellationToken.None);
        var orderId = createResult.Value.Id;

        var getHandler = new GetOrderByIdQueryHandler(ctx, _getOrderByIdQueryLogger);

        // Act
        var getResult = await getHandler.Handle(new GetOrderByIdQuery(orderId, user2.Id),
            CancellationToken.None);

        // Assert
        getResult.IsFailure.Should().BeTrue();
        getResult.Error.Type.Should().Be(ErrorType.NotFound);
    }
}