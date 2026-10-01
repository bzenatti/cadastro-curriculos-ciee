import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import App from './App'

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

describe('App', () => {
  it('mostra a lista de candidatos na raiz', () => {
    renderAt('/')

    expect(screen.getByRole('heading', { name: 'Candidatos' })).toBeInTheDocument()
  })

  it('mostra o formulário de cadastro em /candidatos/novo, e não os detalhes de um candidato', () => {
    renderAt('/candidatos/novo')

    expect(screen.getByRole('heading', { name: 'Novo candidato' })).toBeInTheDocument()
    expect(screen.getByLabelText('Nome completo')).toBeInTheDocument()
  })

  it('mostra os detalhes do candidato indicado no endereço', () => {
    renderAt('/candidatos/42')

    expect(screen.getByRole('heading', { name: 'Candidato 42' })).toBeInTheDocument()
  })

  it('mostra "página não encontrada" em um endereço desconhecido, mantendo o menu', () => {
    renderAt('/qualquer-coisa')

    expect(screen.getByRole('heading', { name: 'Página não encontrada' })).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Principal' })).toBeInTheDocument()
  })

  it('navega pelo menu e marca no menu a página atual', async () => {
    const user = userEvent.setup()
    renderAt('/')
    expect(screen.getByRole('link', { name: 'Candidatos' })).toHaveAttribute('aria-current', 'page')

    await user.click(screen.getByRole('link', { name: 'Novo candidato' }))

    expect(screen.getByRole('heading', { name: 'Novo candidato' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Novo candidato' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('link', { name: 'Candidatos' })).not.toHaveAttribute('aria-current')
  })
})
