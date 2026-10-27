interface Props {
  pagina: number
  totalPaginas: number
  onCambiar: (pagina: number) => void
  /** Nombre accesible del bloque, por ejemplo "Paginación de activos". */
  etiqueta?: string
}

export function Pagination({ pagina, totalPaginas, onCambiar, etiqueta = 'Paginación de incidentes' }: Props) {
  if (totalPaginas <= 1) return null

  return (
    <nav className="paginacion" aria-label={etiqueta}>
      <button type="button" className="boton boton--secundario" onClick={() => onCambiar(pagina - 1)} disabled={pagina <= 1}>
        <span aria-hidden="true">← </span>Anterior
      </button>
      <p className="paginacion__estado">
        Página {pagina} de {totalPaginas}
      </p>
      <button type="button" className="boton boton--secundario" onClick={() => onCambiar(pagina + 1)} disabled={pagina >= totalPaginas}>
        Siguiente<span aria-hidden="true"> →</span>
      </button>
    </nav>
  )
}
