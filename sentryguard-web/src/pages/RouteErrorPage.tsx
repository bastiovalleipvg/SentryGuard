// Ultimo recurso ante un error de renderizado: mensaje generico, sin detalles tecnicos ni stack trace.
export function RouteErrorPage() {
  return (
    <main className="contenedor principal">
      <h1 className="titulo-pagina">Algo salió mal</h1>
      <p>Ocurrió un error inesperado en la aplicación. Recarga la página para continuar.</p>
      <p>
        <a href="/incidentes" className="boton boton--secundario">Volver al inicio</a>
      </p>
    </main>
  )
}
