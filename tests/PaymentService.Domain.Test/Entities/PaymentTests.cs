using FluentAssertions;
using PaymentService.Domain.Entities.Payments;
using PaymentService.Domain.Enums.Payments;
using PaymentService.Domain.Events;

namespace PaymentService.Domain.Test.Entities;

public class PaymentTests
{
    private static readonly Guid ValidOrderId = Guid.NewGuid();
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const decimal ValidAmount = 100m;
    private const string ValidCurrency = "TJS";

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        var result = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(ValidOrderId);
        result.Value.UserId.Should().Be(ValidUserId);
        result.Value.Money.Amount.Should().Be(ValidAmount);
        result.Value.Money.Currency.Should().Be(ValidCurrency);
        result.Value.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Create_WithEmptyOrderId_ShouldFail()
    {
        var result = Payment.Create(Guid.Empty, ValidUserId, ValidAmount, ValidCurrency);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        var result = Payment.Create(ValidOrderId, Guid.Empty, ValidAmount, ValidCurrency);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldFail()
    {
        var result = Payment.Create(ValidOrderId, ValidUserId, 0m, ValidCurrency);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldFail()
    {
        var result = Payment.Create(ValidOrderId, ValidUserId, -10m, ValidCurrency);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
    }

    [Fact]
    public void Create_WithEmptyCurrency_ShouldFail()
    {
        var result = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, "");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
    }

    [Fact]
    public void Create_WithInvalidCurrencyFormat_ShouldFail()
    {
        var result = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, "INVALID");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Validation);
    }

    [Fact]
    public void Create_NormalizesLowercaseCurrency()
    {
        var result = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, "tjs");

        result.IsSuccess.Should().BeTrue();
        result.Value.Money.Currency.Should().Be("TJS");
    }

    [Fact]
    public void MarkAsCompleted_FromPending_ShouldSucceed()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;

        var result = payment.MarkAsCompleted();

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Successful);
    }

    [Fact]
    public void MarkAsCompleted_FromFailed_ShouldFail()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;
        payment.MarkAsFailed();

        var result = payment.MarkAsCompleted();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Conflict);
    }

    [Fact]
    public void MarkAsCompleted_FromSuccessful_ShouldFail()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;
        payment.MarkAsCompleted();

        var result = payment.MarkAsCompleted();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Conflict);
    }
    
    [Fact]
    public void MarkAsCompleted_ShouldAddDomainEvent()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;

        var result = payment.MarkAsCompleted();

        result.IsSuccess.Should().BeTrue();
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentSucceededDomainEvent);
        var evt = (PaymentSucceededDomainEvent)payment.DomainEvents.Single();
        evt.PaymentId.Should().Be(payment.Id);
        evt.OrderId.Should().Be(ValidOrderId);
    }

    [Fact]
    public void MarkAsFailed_FromPending_ShouldSucceed()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;

        var result = payment.MarkAsFailed();

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_FromSuccessful_ShouldFail()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;
        payment.MarkAsCompleted();

        var result = payment.MarkAsFailed();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Conflict);
    }

    [Fact]
    public void MarkAsFailed_FromFailed_ShouldFail()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;
        payment.MarkAsFailed();

        var result = payment.MarkAsFailed();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Common.ErrorType.Conflict);
    }
    
    [Fact]
    public void MarkAsFailed_ShouldNotAddDomainEvent()
    {
        var payment = Payment.Create(ValidOrderId, ValidUserId, ValidAmount, ValidCurrency).Value;

        var result = payment.MarkAsFailed();

        result.IsSuccess.Should().BeTrue();
        payment.DomainEvents.Should().NotContain(e => e is PaymentSucceededDomainEvent);
    }
}