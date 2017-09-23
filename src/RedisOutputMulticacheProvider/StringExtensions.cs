namespace RedisOutputMulticache.MVC5
{
    internal static class StringExtensions
    {
        public static bool IsNullOrEmpty( this string operand ) => string.IsNullOrEmpty( operand );
        public static bool IsNullOrWhiteSpace( this string operand ) => string.IsNullOrWhiteSpace( operand );
    }
}
