namespace Backend.Repository.Services
{
    internal static class IdentityTokenUrlDecoder
    {
        public static string Decode(string token)
        {
            var decodedToken = Uri.UnescapeDataString(token);

            // Some mail clients or query parsers can turn an unescaped '+' into a space.
            return decodedToken.Replace(' ', '+');
        }
    }
}
