// La URL de la API viene de VITE_API_URL (ver .env.example). Se valida al arrancar para
// fallar rapido con un mensaje claro en vez de hacer peticiones a una URL mal formada.
function leerApiUrl(): string {
  const valor = import.meta.env.VITE_API_URL
  if (!valor) {
    throw new Error('Falta VITE_API_URL. Copia .env.example a .env y define la URL de la API.')
  }

  const url = new URL(valor)
  const esLocal = url.hostname === 'localhost' || url.hostname === '127.0.0.1'
  if (url.protocol !== 'https:' && !(import.meta.env.DEV && esLocal)) {
    throw new Error('VITE_API_URL debe usar HTTPS.')
  }
  return url.origin
}

export const env = {
  apiUrl: leerApiUrl(),
}
