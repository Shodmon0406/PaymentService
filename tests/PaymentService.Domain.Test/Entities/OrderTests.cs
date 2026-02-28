using FluentAssertions;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities.Orders;
using PaymentService.Domain.Entities.Payments;
using PaymentService.Domain.Enums.Orders;
using PaymentService.Domain.Events;

namespace PaymentService.Domain.Test.Entities;

public class OrderTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const decimal ValidAmount = 100m;
    private const string ValidCurrency = "TJS";

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        var result = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(ValidUserId);
        result.Value.Money.Amount.Should().Be(ValidAmount);
        result.Value.Money.Currency.Should().Be(ValidCurrency);
        result.Value.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        var result = Order.Create(Guid.Empty, ValidAmount, ValidCurrency);
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldFail()
    {
        var result = Order.Create(ValidUserId, 0m, ValidCurrency);
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldFail()
    {
        var result = Order.Create(ValidUserId, -10m, ValidCurrency);
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WithEmptyCurrency_ShouldFail()
    {
        var result = Order.Create(ValidUserId, ValidAmount, "");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_WithInvalidCurrency_ShouldFail()
    {
        var result = Order.Create(ValidUserId, ValidAmount, "INVALID");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Create_NormalizeCurrency_ShouldSucceed()
    {
        var result = Order.Create(ValidUserId, ValidAmount, "tjs");
        result.IsSuccess.Should().BeTrue();
        result.Value.Money.Currency.Should().Be("TJS");
    }

    [Fact]
    public void MarkAsPaid_WhenStatusIsCreated_ShouldSucceed()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;

        var result = order.MarkAsPaid();
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void MarkAsPaid_WhenStatusIsNotCreated_ShouldFail()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;
        order.MarkAsPaid();

        var result = order.MarkAsPaid();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void MarkAsPaid_ShouldAddDomainEvent()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;

        var result = order.MarkAsPaid();
        result.IsSuccess.Should().BeTrue();
        order.DomainEvents.Should().ContainSingle(e => e is OrderPaidDomainEvent);

        var evt = order.DomainEvents.OfType<OrderPaidDomainEvent>().Single();
        evt.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public void MarkAsCancelled_WhenStatusIsCreated_ShouldSucceed()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;

        var result = order.MarkAsCancelled();
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void MarkAsCancelled_WhenStatusIsNotCreated_ShouldFail()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;
        order.MarkAsPaid();
        
        var result = order.MarkAsCancelled();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
    
    [Fact]
    public void AddPayment_WhenOrderIdIsMatch_ShouldSucceed()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;

        var payment = CreatePaymentForOrder(order.Id);

        var result = order.AddPayment(payment);
        result.IsSuccess.Should().BeTrue();
        order.Payments.Should().ContainSingle();
    }
    
    [Fact]
    public void AddPayment_WhenOrderIdDoesNotMatch_ShouldFail()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;

        var payment = CreatePaymentForOrder(Guid.NewGuid());

        var result = order.AddPayment(payment);
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
    
    [Fact]
    public void AddPayment_WhenStatusIsPaid_ShouldFail()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;
        order.MarkAsPaid();

        var payment = CreatePaymentForOrder(order.Id);

        var result = order.AddPayment(payment);
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
    
    [Fact]
    public void AddPayment_WhenStatusIsCancelled_ShouldFail()
    {
        var orderResult = Order.Create(ValidUserId, ValidAmount, ValidCurrency);
        var order = orderResult.Value;
        order.MarkAsCancelled();

        var payment = CreatePaymentForOrder(order.Id);

        var result = order.AddPayment(payment);
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
    
    private static Payment CreatePaymentForOrder(Guid orderId)
    {
        var paymentResult = Payment.Create(orderId, ValidUserId, ValidAmount, ValidCurrency);
        return paymentResult.Value;
    }
}