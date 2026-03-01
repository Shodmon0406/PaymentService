namespace PaymentService.Api.Tests.Common;

[CollectionDefinition(nameof(PaymentServiceCollection))]
public sealed class PaymentServiceCollection : ICollectionFixture<PaymentServiceWebApplicationFactory>;