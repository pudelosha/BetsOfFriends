namespace Backend.Repository.Services
{
    public static class IdentityTokenUrlDecoder
    {
        private const string Base64UrlPrefix = "v2_";

        public static string Encode(string token)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            return Base64UrlPrefix + Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);
        }

        public static string Decode(string token)
        {
            var decodedToken = Uri.UnescapeDataString(token);
            var trimmedToken = decodedToken.Trim();

            if (trimmedToken.StartsWith(Base64UrlPrefix, StringComparison.Ordinal))
            {
                var encodedToken = trimmedToken[Base64UrlPrefix.Length..];
                var bytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(encodedToken);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }

            // Some mail clients or query parsers can turn an unescaped '+' into a space.
            return decodedToken.Replace(' ', '+');
        }
    }
}
