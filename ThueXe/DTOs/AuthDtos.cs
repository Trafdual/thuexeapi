namespace ThueXe.Dtos
{
    public record RegisterRequest(string Phone, string Password, string FullName, string? Email);
    public record VerifyOtpRequest(string Phone, string Otp);
    public record LoginRequest(string Phone, string Password);

    public record MeResponse(
        long Id, string Phone, string FullName, string? Email,
        bool IsOwner, string Status, string DocumentStatus);

    public record AuthResponse(string Token, MeResponse Profile);

    public record SubmitDocumentsRequest(
        string? CccdNo, string? GplxNo, string? GplxClass, DateOnly? GplxExpiry,
        string FrontUrl, string BackUrl, string? SelfieUrl);
}
