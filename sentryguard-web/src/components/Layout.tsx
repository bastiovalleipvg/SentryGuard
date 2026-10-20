import { NavLink, Outlet } from 'react-router'
import { useAuth } from '../features/auth/useAuth'
import { puede } from '../features/auth/permisos'

function Logo() {
  return (
    <svg className="marca__logo" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <path fill="currentColor" d="M16 2 4 6.5v8.2c0 7.3 5.1 13.6 12 15.3 6.9-1.7 12-8 12-15.3V6.5L16 2z" />
      <path fill="#fff" d="m14.2 20.6-4.6-4.6 1.9-1.9 2.7 2.7 6.3-6.3 1.9 1.9z" />
    </svg>
  )
}

export function Layout() {
  const { usuario, cerrarSesion } = useAuth()

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

          <nav aria-label="Principal" className="nav">
            <ul className="nav__lista">
              <li>
                <NavLink to="/incidentes" end className="nav__enlace">
                  Incidentes
                </NavLink>
              </li>
              {puede(usuario, 'crearIncidente') && (
                <li>
                  <NavLink to="/incidentes/nuevo" className="nav__enlace">
                    Nuevo incidente
                  </NavLink>
                </li>
              )}
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
