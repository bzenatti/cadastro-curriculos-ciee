import { NavLink, Outlet } from 'react-router'
import './Layout.css'

export function Layout() {
  return (
    <>
      <header className="layout_header">
        <nav className="layout_nav" aria-label="Principal">
          <span className="layout_brand">Cadastro de candidatos</span>
          <NavLink to="/" end>Candidatos</NavLink>
          <NavLink to="/candidatos/novo">Novo candidato</NavLink>
        </nav>
      </header>
      <main className="layout_content">
        <Outlet />
      </main>
    </>
  )
}
