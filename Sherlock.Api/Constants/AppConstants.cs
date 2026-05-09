namespace Sherlock.Api.Constants;

/// <summary>
/// Constantes da aplicação
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Constantes de autenticação
    /// </summary>
    public static class Auth
    {
        public const int BcryptWorkFactor = 12;
        public const int DefaultTokenExpiryMinutes = 30;
        public const int EmailMaxLength = 256;
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 128;
        public const int UsernameMaxLength = 100;
    }

    /// <summary>
    /// Constantes do carrinho de compras
    /// </summary>
    public static class Cart
    {
        public const int MaxBooksPerOptimization = 20;
        public const int DefaultQuantity = 1;
        public const int MaxQuantityPerBook = 10;
    }

    /// <summary>
    /// Constantes de cache
    /// </summary>
    public static class Cache
    {
        public const int DefaultCacheHours = 2;
    }
}
