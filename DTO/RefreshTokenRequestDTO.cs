namespace JwtApp.DTO
{
    public class RefreshTokenRequestDTO
    {
        public Guid UserID { get; set; }
        public string RefreshToken { get; set; } = string.Empty;

    }
}
