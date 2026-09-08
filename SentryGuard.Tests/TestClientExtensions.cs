using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SentryGuard.Tests;

internal static class TestClientExtensions
{
    private record LoginRequest(string Email, string Password);
    private record LoginResponse(string Token, string RefreshToken, int ExpiresIn);

    /// <summary>
    /// Hace login contra /api/v1/usuarios/login y devuelve el JWT. Usa la contraseña de
    /// prueba comun a los 4 usuarios sembrados (Admin123!) salvo que se indique otra.
    /// </summary>
    public static async Task<string> LoginAsync(this HttpClient client, string email, string password = "Admin123!")
    {
        var response = await client.PostAsJsonAsync("/api/v1/usuarios/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    public static void UseBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
