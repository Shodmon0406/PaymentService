namespace PaymentService.Application.Auth;

public interface IRefreshTokenGenerator
{
    string Generate();
}