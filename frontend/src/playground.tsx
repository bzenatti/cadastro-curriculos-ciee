// Página só de desenvolvimento para ver cada componente isolado.
// Com `npm run dev`, abra http://localhost:5173/playground.html (não entra no build).
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import { Button } from './components/ui/Button/Button'

function Playground() {
  return (
    <main style={{ padding: 24 }}>
      <h1>Playground de componentes</h1>

      <section>
        <h2>Button</h2>
        <div style={{ display: 'flex', gap: 12 }}>
          <Button>Salvar</Button>
          <Button variant="secondary">Limpar</Button>
          <Button loading>Salvar</Button>
          <Button disabled>Desabilitado</Button>
        </div>
      </section>
    </main>
  )
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Playground />
  </StrictMode>,
)
