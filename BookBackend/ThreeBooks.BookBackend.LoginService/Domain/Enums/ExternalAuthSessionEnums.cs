namespace ThreeBooks.BookBackend.LoginService.Domain.Enums;

public enum ExternalAuthPurpose
{
    Login = 0,
    Bind = 1
}

public enum ExternalAuthSessionStatus
{
    Pending = 0,
    Authorized = 1,
    Completed = 2,
    Failed = 3,
    Expired = 4,
    Consumed = 5,
    Bound = 6
}