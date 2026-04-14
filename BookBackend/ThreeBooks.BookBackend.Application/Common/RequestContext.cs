namespace ThreeBooks.BookBackend.Application.Common;

public sealed record RequestContext(string? IpAddress, string? UserAgent);