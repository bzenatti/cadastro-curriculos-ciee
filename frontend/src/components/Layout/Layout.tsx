import { NavLink, Outlet } from 'react-router'

export function Layout() {
  return (
    <>
      <header>
        <nav aria-label="Principal">
          <NavLink to="/" end>Candidatos</NavLink>
          <NavLink to="/candidatos/novo">Novo candidato</NavLink>
        </nav>
      </header>
      <main className="app">
        <Outlet />
      </main>
    </>
  )
}
