import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listCandidates } from '../../api/candidates'
import type { CandidateListItem, Page } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { CandidateListPage } from './CandidateListPage'

vi.mock('../../api/candidates')

const mockedList = vi.mocked(listCandidates)

function item(id: number, overrides: Partial<CandidateListItem> = {}): CandidateListItem {
  return {
    id,
    name: `Candidato ${id}`,
    email: `candidato${id}@exemplo.com`,
    position: 'Desenvolvimento',
    createdAt: '2026-10-01T12:00:00Z',
    ...overrides,
  }
}

function pageOf(items: CandidateListItem[], overrides: Partial<Page<CandidateListItem>> = {}): Page<CandidateListItem> {
  return { items, page: 1, pageSize: 10, totalCount: items.length, ...overrides }
}

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <CandidateListPage />
    </MemoryRouter>,
  )
}

describe('CandidateListPage', () => {
  beforeEach(() => {
    mockedList.mockReset()
  })

  it('mostra "carregando" e depois a tabela, com link para os detalhes, "Não informado" e data em pt-BR', async () => {
    mockedList.mockResolvedValue(pageOf([item(1, { name: 'Maria Silva', position: null }), item(2)]))
    renderAt('/')
    expect(screen.getByText('Carregando candidatos…')).toBeInTheDocument()

    const link = await screen.findByRole('link', { name: 'Maria Silva' })

    expect(link).toHaveAttribute('href', '/candidatos/1')
    expect(screen.getByText('candidato1@exemplo.com')).toBeInTheDocument()
    expect(screen.getByText('Não informado')).toBeInTheDocument()
    expect(screen.getAllByText('01/10/2026')).toHaveLength(2)
  })

  it('mostra o estado vazio com link para cadastrar', async () => {
    mockedList.mockResolvedValue(pageOf([]))
    renderAt('/')

    expect(await screen.findByText(/Nenhum candidato cadastrado ainda/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Cadastrar o primeiro' })).toHaveAttribute('href', '/candidatos/novo')
  })

  it('mostra a mensagem de erro e carrega de novo ao clicar em "Tentar de novo"', async () => {
    const user = userEvent.setup()
    mockedList
      .mockRejectedValueOnce(new ApiError(0, 'Não foi possível conectar ao servidor.'))
      .mockResolvedValueOnce(pageOf([item(1, { name: 'Maria Silva' })]))
    renderAt('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível conectar ao servidor.')
    await user.click(screen.getByRole('button', { name: 'Tentar de novo' }))

    expect(await screen.findByRole('link', { name: 'Maria Silva' })).toBeInTheDocument()
    expect(mockedList).toHaveBeenCalledTimes(2)
  })

  it('lê a página da URL e pede a página escolhida ao usar a paginação', async () => {
    const user = userEvent.setup()
    mockedList.mockResolvedValue(pageOf([item(11)], { page: 2, totalCount: 25 }))
    renderAt('/?page=2')

    expect(await screen.findByText('Página 2 de 3')).toBeInTheDocument()
    expect(mockedList).toHaveBeenCalledWith(2)

    await user.click(screen.getByRole('button', { name: 'Próxima' }))

    expect(await screen.findByText('Página 2 de 3')).toBeInTheDocument()
    expect(mockedList).toHaveBeenLastCalledWith(3)
  })

  it('trata uma página inválida na URL como a primeira', async () => {
    mockedList.mockResolvedValue(pageOf([]))
    renderAt('/?page=abc')

    await screen.findByText(/Nenhum candidato cadastrado ainda/)

    expect(mockedList).toHaveBeenCalledWith(1)
  })
})
