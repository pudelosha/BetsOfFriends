using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, IConfiguration config, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring(7);
            _logger.LogInformation("JWT Middleware: Extracted token");

            var claimsPrincipal = ValidateToken(token);

            if (claimsPrincipal != null)
            {
                _logger.LogInformation("JWT Middleware: Token validated successfully");
                context.User = claimsPrincipal;
            }
            else
            {
                _logger.LogWarning("JWT Middleware: Token validation failed");
            }
        }
        else
        {
            _logger.LogWarning("JWT Middleware: No valid Authorization header found");
        }

        await _next(context);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("JWT Middleware: Token is null or empty");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            // Read JWT settings from `appsettings.json`
            var key = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT Middleware: JWT configuration is missing required values");
                return null;
            }

            var keyBytes = Encoding.UTF8.GetBytes(key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

                ValidateIssuer = true,
                ValidateAudience = true,

                ValidIssuer = issuer,
                ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            _logger.LogInformation("JWT Middleware: Starting token validation");

            ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
            _logger.LogInformation("JWT Middleware: Token validation successful");
            return claimsPrincipal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT Middleware: Token has expired");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            _logger.LogWarning("JWT Middleware: Invalid issuer");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            _logger.LogWarning("JWT Middleware: Invalid audience");
        }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            _logger.LogError("JWT Middleware: Signature key not found. Possible misconfigured key.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"JWT Middleware: Token validation failed - {ex.Message}");
        }

        return null;
    }
}
