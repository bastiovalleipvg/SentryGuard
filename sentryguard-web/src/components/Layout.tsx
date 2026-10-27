import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { useAuth } from '../features/auth/useAuth'
import { puede, type Accion } from '../features/auth/permisos'
import type { Rol } from '../api/contracts'

function Logo() {
  return (
    <svg className="marca__logo" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <path fill="currentColor" d="M16 2 4 6.5v8.2c0 7.3 5.1 13.6 12 15.3 6.9-1.7 12-8 12-15.3V6.5L16 2z" />
      <path fill="#fff" d="m14.2 20.6-4.6-4.6 1.9-1.9 2.7 2.7 6.3-6.3 1.9 1.9z" />
    </svg>
  )
}

interface Enlace {
  a: string
  texto: string
  /** Accion de permisos.ts que habilita el enlace; sin ella lo ven todos los roles. */
  accion?: Accion
  soloRol?: Rol
}

// El menu solo muestra lo que el rol puede abrir; la ruta y la API vuelven a validar el permiso.
const ENLACES: Enlace[] = [
  { a: '/incidentes', texto: 'Incidentes' },
  { a: '/activos', texto: 'Activos' },
  { a: '/organizaciones', texto: 'Organizaciones', accion: 'verOrganizaciones' },
  { a: '/mi-organizacion', texto: 'Mi organización', soloRol: 'Cliente' },
  { a: '/usuarios', texto: 'Usuarios', accion: 'gestionarUsuarios' },
  { a: '/catalogos', texto: 'Catálogos', accion: 'gestionarCatalogos' },
  { a: '/auditoria', texto: 'Auditoría', accion: 'verAuditoria' },
]

export function Layout() {
  const { usuario, cerrarSesion } = useAuth()
  const { pathname } = useLocation()
  // En movil el menu queda abierto solo en la ruta donde se abrio: al navegar se cierra solo
  // (y el foco pasa al encabezado de la nueva vista).
  const [abiertoEn, setAbiertoEn] = useState<string | null>(null)
  const menuAbierto = abiertoEn === pathname

  const enlaces = ENLACES.filter((e) => (e.soloRol ? usuario?.rol === e.soloRol : !e.accion || puede(usuario, e.accion)))

  return (
    <div className="app">
      <a href="#contenido" className="saltar-enlace">
        Saltar al contenido principal
      </a>

      <header className="cabecera">
        <div className="contenedor cabecera__interior">
          <NavLink to="/incidentes" className="marca">
            <Logo />
            <span>SentryGuard</span>
          </NavLink>

          <button
            type="button"
            className="boton boton--menu"
            aria-expanded={menuAbierto}
            aria-controls="menu-principal"
            onClick={() => setAbiertoEn(menuAbierto ? null : pathname)}
          >
            <span aria-hidden="true">{menuAbierto ? '✕' : '☰'} </span>Menú
          </button>

          <nav aria-label="Principal" className="nav">
            <ul id="menu-principal" className={`nav__lista${menuAbierto ? ' nav__lista--abierta' : ''}`}>
              {enlaces.map((e) => (
                <li key={e.a}>
                  <NavLink to={e.a} end={e.a === '/mi-organizacion'} className="nav__enlace">
                    {e.texto}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>

          {usuario && (
            <div className="sesion">
              <p className="sesion__usuario">
                <span className="sesion__nombre">{usuario.nombre}</span>
                <span className="sesion__rol">Rol: {usuario.rol}</span>
              </p>
              <button type="button" className="boton boton--secundario" onClick={() => cerrarSesion('manual')}>
                Cerrar sesión
              </button>
            </div>
          )}
        </div>
      </header>

      <main id="contenido" tabIndex={-1} className="contenedor principal">
        <Outlet />
      </main>

      <footer className="pie">
        <div className="contenedor">
          <p>SentryGuard · CyberOps Chile SpA</p>
        </div>
      </footer>
    </div>
  )
}
