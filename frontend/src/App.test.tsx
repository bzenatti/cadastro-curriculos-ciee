import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { getCandidate, listCandidates } from './api/candidates'

vi.mock('./api/candidates')

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

describe('App', () => {
  beforeEach(() => {
    vi.mocked(listCandidates).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0
    })
    vi.mocked(getCandidate).mockResolvedValue({
      id: 42,
      name: 'Maria Silva',
      email: 'maria@exemplo.com',
      phone: null,
      position: null,
      summary: null,
      createdAt: '2026-10-01T12:00:00Z',
    })
  })

  it('mostra a lista de candidatos na raiz', async () => {
    renderAt('/')

    expect(screen.getByRole('heading', { name: 'Candidatos' })).toBeInTheDocument()
    await screen.findByText(/Nenhum candidato cadastrado ainda/)
  })

  it('mostra o formulário de cadastro em /candidatos/novo, e não os detalhes de um candidato', () => {
    renderAt('/candidatos/novo')

    expect(screen.getByRole('heading', { name: 'Novo candidato' })).toBeInTheDocument()
    expect(screen.getByLabelText('Nome completo')).toBeInTheDocument()
  })

  it('mostra os detalhes do candidato indicado no endereço', async () => {
    renderAt('/candidatos/42')

    expect(await screen.findByRole('heading', { name: 'Maria Silva' })).toBeInTheDocument()
    expect(getCandidate).toHaveBeenCalledWith('42')
  })

  it('mostra "página não encontrada" em um endereço desconhecido, mantendo o menu', () => {
    renderAt('/qualquer-coisa')

    expect(screen.getByRole('heading', { name: 'Página não encontrada' })).toBeInTheDocument()
    expect(screen.getByRole('navigation', { name: 'Principal' })).toBeInTheDocument()
  })

  it('navega pelo menu e marca no menu a página atual', async () => {
    const user = userEvent.setup()
    renderAt('/')
    await screen.findByText(/Nenhum candidato cadastrado ainda/)
    expect(screen.getByRole('link', { name: 'Candidatos' })).toHaveAttribute('aria-current', 'page')

    await user.click(screen.getByRole('link', { name: 'Novo candidato' }))

    expect(screen.getByRole('heading', { name: 'Novo candidato' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Novo candidato' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('link', { name: 'Candidatos' })).not.toHaveAttribute('aria-current')
  })
})
