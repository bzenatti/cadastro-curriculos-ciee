import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getCandidate } from '../../api/candidates'
import type { Candidate } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { CandidateDetailsPage } from './CandidateDetailsPage'

vi.mock('../../api/candidates')

const mockedGet = vi.mocked(getCandidate)

function candidate(overrides: Partial<Candidate> = {}): Candidate {
  return {
    id: 4242,
    name: 'Maria Silva',
    email: 'maria@exemplo.com',
    phone: '11987654321',
    position: 'Desenvolvimento',
    summary: 'Desenvolvedora com 3 anos de experiência.',
    createdAt: '2026-10-01T12:00:00Z',
    ...overrides,
  }
}

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/candidatos/:id" element={<CandidateDetailsPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('CandidateDetailsPage', () => {
  beforeEach(() => {
    mockedGet.mockReset()
  })

  it('mostra "carregando" e depois o nome como título, com os dados e sem o id', async () => {
    mockedGet.mockResolvedValue(candidate())
    renderAt('/candidatos/4242')
    expect(screen.getByText('Carregando candidato…')).toBeInTheDocument()

    expect(await screen.findByRole('heading', { name: 'Maria Silva' })).toBeInTheDocument()

    expect(mockedGet).toHaveBeenCalledWith('4242')
    expect(screen.getByText('maria@exemplo.com')).toBeInTheDocument()
    expect(screen.getByText('11987654321')).toBeInTheDocument()
    expect(screen.getByText('Desenvolvimento')).toBeInTheDocument()
    expect(screen.getByText('01/10/2026')).toBeInTheDocument()
    expect(screen.getByText('Desenvolvedora com 3 anos de experiência.')).toBeInTheDocument()
    expect(screen.queryByText(/4242/)).not.toBeInTheDocument()
  })

  it('mostra "Não informado" nos campos opcionais vazios', async () => {
    mockedGet.mockResolvedValue(candidate({ phone: null, position: null, summary: null }))
    renderAt('/candidatos/4242')

    await screen.findByRole('heading', { name: 'Maria Silva' })

    expect(screen.getAllByText('Não informado')).toHaveLength(3)
  })

  it('mostra "Candidato não encontrado" e o link de voltar quando a API responde 404', async () => {
    mockedGet.mockRejectedValue(new ApiError(404, 'Texto da API que não deve aparecer.'))
    renderAt('/candidatos/999')

    expect(await screen.findByRole('heading', { name: 'Candidato não encontrado' })).toBeInTheDocument()

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Tentar de novo' })).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Voltar para a lista de candidatos' })).toHaveAttribute('href', '/')
  })

  it('mostra a mensagem de erro e carrega de novo ao clicar em "Tentar de novo"', async () => {
    const user = userEvent.setup()
    mockedGet
      .mockRejectedValueOnce(new ApiError(0, 'Não foi possível conectar ao servidor.'))
      .mockResolvedValueOnce(candidate())
    renderAt('/candidatos/4242')

    expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível conectar ao servidor.')
    await user.click(screen.getByRole('button', { name: 'Tentar de novo' }))

    expect(await screen.findByRole('heading', { name: 'Maria Silva' })).toBeInTheDocument()
    expect(mockedGet).toHaveBeenCalledTimes(2)
  })
})
