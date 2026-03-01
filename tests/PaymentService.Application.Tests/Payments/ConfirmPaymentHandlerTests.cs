using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PaymentService.Application.Auth;
using PaymentService.Application.Features.Orders.Commands.CreateOrder;
using PaymentService.Application.Features.Payments.Commands.ConfirmPayment;
using PaymentService.Application.Features.Payments.Commands.CreatePayment;
using PaymentService.Application.Features.Services;
using PaymentService.Application.Features.Users.Commands.Register;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Users;
using PaymentService.Domain.Enums.Orders;
using PaymentService.Domain.Enums.Payments;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Application.Tests.Payments;

public class ConfirmPaymentHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IOrderLockService _orderLockService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;

    public ConfirmPaymentHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _jwtTokenService.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>()).Returns("access_token");
        
        _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
        _refreshTokenGenerator.Generate().Returns(_ => Guid.NewGuid().ToString("N"));
        
        _orderLockService = Substitute.For<IOrderLockService>();
        _orderLockService.AcquireLockAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private ApplicationDbContext CreateDbContext()
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

    private async Task<Guid> RegisterUserAsync(string phone = "+992123456789", string email = "test@mail.com")
    {
        var handler = new RegisterCommandHandler(CreateDbContext(), _jwtTokenService, _refreshTokenGenerator);
        var command = new RegisterCommand(phone, email, "Test Testov", "Passw0rd!", "127.0.0.1");
        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value.UserId;
    }

    private async Task<Guid> CreateOrderAsync(Guid userId, decimal amount = 100m, string currency = "TJS")
    {
        var handler = new CreateOrderCommandHandler(CreateDbContext());
        var command = new CreateOrderCommand(userId, amount, currency);
        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value.Id;
    }

    private async Task<Guid> CreatePaymentAsync(Guid userId, Guid orderId)
    {
        var handler = new CreatePaymentCommandHandler(CreateDbContext());
        var command = new CreatePaymentCommand(userId, orderId);
        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value.Id;
    }
    
    [Fact]
    public async Task ConfirmPayment_WithValidPendingPayment_ShouldSucceed()
    {
        // Arrange
        var context = CreateDbContext();
        var userId = await RegisterUserAsync();
        var orderId = await CreateOrderAsync(userId);
        var paymentId = await CreatePaymentAsync(userId, orderId);
        
        var handler = new ConfirmPaymentCommandHandler(context, _orderLockService);
        var command = new ConfirmPaymentCommand(userId, paymentId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(paymentId);
        result.Value.OrderId.Should().Be(orderId);
        result.Value.UserId.Should().Be(userId);
        result.Value.Status.Should().Be(PaymentStatus.Successful);
        
        var dbOrder = await context.Orders.FindAsync(orderId);
        dbOrder.Should().NotBeNull();
        dbOrder.Status.Should().Be(OrderStatus.Paid);
    }
    
    [Fact]
    public async Task ConfirmPayment_WithNonExistentPayment_ShouldFail()
    {
        // Arrange
        var context = CreateDbContext();
        var userId = await RegisterUserAsync();
        var nonExistentPaymentId = Guid.NewGuid();
        
        var handler = new ConfirmPaymentCommandHandler(context, _orderLockService);
        var command = new ConfirmPaymentCommand(userId, nonExistentPaymentId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
    
    [Fact]
    public async Task ConfirmPayment_WhenOrderAlreadyPaid_ShouldFail()
    {
        // Arrange
        var context = CreateDbContext();
        var userId = await RegisterUserAsync();
        var orderId = await CreateOrderAsync(userId);
        var firstPaymentId = await CreatePaymentAsync(userId, orderId);
        
        // Simulate another payment being confirmed for the same order
        var order = await context.Orders.FindAsync(orderId);
        order.Should().NotBeNull();
        order.MarkAsPaid();
        await context.SaveChangesAsync();
        
        var confirmHandler = new ConfirmPaymentCommandHandler(context, _orderLockService);
        var command = new ConfirmPaymentCommand(userId, firstPaymentId);

        // Act
        var confirmResult = await confirmHandler.Handle(command, CancellationToken.None);

        // Assert
        confirmResult.IsFailure.Should().BeTrue();
        confirmResult.Error.Type.Should().Be(ErrorType.Conflict);
    }
    
    [Fact]
    public async Task ConfirmPayment_WithAlreadyConfirmedPayment_ShouldFail()
    {
        // Arrange
        var context = CreateDbContext();
        var userId = await RegisterUserAsync();
        var orderId = await CreateOrderAsync(userId);
        var paymentId = await CreatePaymentAsync(userId, orderId);
        
        var confirmHandler = new ConfirmPaymentCommandHandler(context, _orderLockService);
        var confirmCommand = new ConfirmPaymentCommand(userId, paymentId);
        var confirmResult = await confirmHandler.Handle(confirmCommand, CancellationToken.None);
        confirmResult.IsSuccess.Should().BeTrue();

        // Act
        var secondConfirmResult = await confirmHandler.Handle(confirmCommand, CancellationToken.None);

        // Assert
        secondConfirmResult.IsFailure.Should().BeTrue();
        secondConfirmResult.Error.Code.Should().Be("Payment.Status");
    }
    
    [Fact]
    public async Task ConfirmPayment_WhenAnotherPaymentAlreadyConfirmed_ShouldFail()
    {
        // Arrange
        var context = CreateDbContext();
        var userId = await RegisterUserAsync();
        var orderId = await CreateOrderAsync(userId);
        var firstPaymentId = await CreatePaymentAsync(userId, orderId);
        var secondPaymentId = await CreatePaymentAsync(userId, orderId);
        
        var confirmHandler = new ConfirmPaymentCommandHandler(context, _orderLockService);
        var firstConfirmCommand = new ConfirmPaymentCommand(userId, firstPaymentId);
        var secondConfirmCommand = new ConfirmPaymentCommand(userId, secondPaymentId);
        
        var firstConfirmResult = await confirmHandler.Handle(firstConfirmCommand, CancellationToken.None);
        firstConfirmResult.IsSuccess.Should().BeTrue();

        // Act
        var secondConfirmResult = await confirmHandler.Handle(secondConfirmCommand, CancellationToken.None);

        // Assert
        secondConfirmResult.IsFailure.Should().BeTrue();
        secondConfirmResult.Error.Code.Should().Be("Payment.AlreadyConfirmed");
    }
    
    [Fact]
    public async Task ConfirmPayment_WithConcurrentConfirms_ShouldOnlyAllowOne()
    {
        // Arrange
        var context = CreateDbContext();
        var userId = await RegisterUserAsync();
        var orderId = await CreateOrderAsync(userId);
        var paymentId = await CreatePaymentAsync(userId, orderId);
        
        var handler1 = new ConfirmPaymentCommandHandler(context, _orderLockService);
        var handler2 = new ConfirmPaymentCommandHandler(context, _orderLockService);
        
        var command1 = new ConfirmPaymentCommand(userId, paymentId);
        var command2 = new ConfirmPaymentCommand(userId, paymentId);

        // Act
        var task1 = handler1.Handle(command1, CancellationToken.None);
        var task2 = handler2.Handle(command2, CancellationToken.None);
        
        await Task.WhenAll(task1, task2);

        // Assert
        var results = new[] { await task1, await task2 };
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure).Should().Be(1);
        
        var dbOrder = await context.Orders.FindAsync(orderId);
        dbOrder.Should().NotBeNull();
        dbOrder.Status.Should().Be(OrderStatus.Paid);
    }
}