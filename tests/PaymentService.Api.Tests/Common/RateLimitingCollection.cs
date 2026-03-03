using PaymentService.Api.IntegrationTests.Infrastructure;

namespace PaymentService.Api.Tests.Common;

[CollectionDefinition(nameof(RateLimitingCollection))]
public sealed class RateLimitingCollection : ICollectionFixture<RateLimitingWebApplicationFactory> { }
