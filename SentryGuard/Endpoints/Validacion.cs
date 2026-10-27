using System.Text.RegularExpressions;

namespace SentryGuard.Endpoints
{
    // Validacion de servidor compartida por los endpoints. La del frontend solo da respuesta inmediata:
    // el cliente es controlable por el atacante, asi que estas reglas son las que realmente protegen.
    internal static partial class Validacion
    {
        public static void Texto(Dictionary<string, string[]> errores, string campo, string? valor, int min, int max, bool requerido)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                if (requerido) errores[campo] = ["Este campo es obligatorio."];
                else if (valor is not null && min > 0) errores[campo] = [$"Debe tener entre {min} y {max} caracteres."];
                return;
            }

            var largo = valor.Trim().Length;
            if (largo < min || largo > max)
                errores[campo] = [$"Debe tener entre {min} y {max} caracteres."];
        }

        public static void Horas(Dictionary<string, string[]> errores, string campo, decimal valor, decimal max = 720m)
        {
            if (valor <= 0 || valor > max)
                errores[campo] = [$"Debe ser mayor que 0 y no superar {max} horas."];
        }

        // Correo con forma usuario@dominio.tld (la unicidad se valida aparte contra la BD).
        public static bool EsEmail(string? valor) =>
            !string.IsNullOrWhiteSpace(valor) && valor.Length <= 320 && EmailRegex().IsMatch(valor.Trim());

        // Politica minima de contraseñas: 8 a 128 caracteres con mayuscula, minuscula, numero y simbolo.
        public static string? ErrorPassword(string? valor)
        {
            if (string.IsNullOrEmpty(valor)) return "Este campo es obligatorio.";
            if (valor.Length < 8 || valor.Length > 128) return "Debe tener entre 8 y 128 caracteres.";
            if (!valor.Any(char.IsUpper) || !valor.Any(char.IsLower) || !valor.Any(char.IsDigit) || valor.All(char.IsLetterOrDigit))
                return "Debe incluir mayúscula, minúscula, número y símbolo.";
            return null;
        }

        // Nombre de host, IPv4 o IPv6: solo letras, numeros, punto, guion y dos puntos.
        public static bool EsHostnameOIp(string valor) => HostRegex().IsMatch(valor);

        /// <summary>RUT sin puntos y con guion ("76123456-0"), DV en mayuscula. null si el formato no es valido.</summary>
        public static string? NormalizarRut(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var rut = valor.Trim().Replace(".", "").ToUpperInvariant();
            return RutRegex().IsMatch(rut) ? rut : null;
        }

        // Digito verificador por modulo 11.
        public static bool RutTieneDvValido(string rutNormalizado)
        {
            var partes = rutNormalizado.Split('-');
            int suma = 0, factor = 2;
            for (var i = partes[0].Length - 1; i >= 0; i--)
            {
                suma += (partes[0][i] - '0') * factor;
                factor = factor == 7 ? 2 : factor + 1;
            }
            var resto = 11 - (suma % 11);
            var esperado = resto == 11 ? "0" : resto == 10 ? "K" : resto.ToString();
            return partes[1] == esperado;
        }

        /// <summary>Respuesta 409 con el formato de error propio ({ codigo, mensaje }) que el frontend muestra tal cual.</summary>
        public static IResult Conflicto(string codigo, string mensaje) =>
            Results.Json(new { codigo, mensaje }, statusCode: StatusCodes.Status409Conflict);

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"^[A-Za-z0-9.:\-]{1,255}$")]
        private static partial Regex HostRegex();

        [GeneratedRegex(@"^\d{7,8}-[\dK]$")]
        private static partial Regex RutRegex();
    }
}
