namespace ThreeBooks.BookBackend.LoginService.Contracts.Requests;

public sealed record RegisterRequest(
    string Username,
    string? DisplayName,
    string? Mobile,
    string Password);

public sealed record PasswordLoginRequest(
    string Identity,
    string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);