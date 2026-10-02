import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createCandidate, parseResume } from '../../api/candidates'
import type { Candidate, ResumeExtraction } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { NewCandidatePage } from './NewCandidatePage'

vi.mock('../../api/candidates')

const mockedParse = vi.mocked(parseResume)
const mockedCreate = vi.mocked(createCandidate)

type User = ReturnType<typeof userEvent.setup>

// O que o backend devolveria para um currículo em que o parser achou tudo.
const found: ResumeExtraction = { name: 'Maria da Silva', email: 'maria@example.com', phone: '41900001234' }

const field = (label: string) => screen.getByLabelText(label)
const button = (name: string) => screen.getByRole('button', { name })

function importPdf(user: User) {
  const pdf = new File(['%PDF-'], 'cv.pdf', { type: 'application/pdf' })
  return user.upload(screen.getByLabelText('Arquivo PDF do currículo'), pdf)
}

async function fillRequired(user: User) {
  await user.type(field('Nome completo'), 'Maria da Silva')
  await user.type(field('E-mail'), 'maria@example.com')
}

describe('NewCandidatePage', () => {
  beforeEach(() => {
    mockedParse.mockReset()
    mockedCreate.mockReset()
  })

  describe('importar PDF', () => {
    it('preenche os campos encontrados e avisa o que ficou de fora', async () => {
      const user = userEvent.setup()
      mockedParse.mockResolvedValue({ ...found, phone: null })
      render(<NewCandidatePage />)

      await importPdf(user)

      expect(await screen.findByText(/Não encontramos telefone/)).toBeInTheDocument()
      expect(mockedParse.mock.calls[0][0].name).toBe('cv.pdf')
      expect(field('Nome completo')).toHaveValue('Maria da Silva')
      expect(field('E-mail')).toHaveValue('maria@example.com')
      expect(field('Telefone')).toHaveValue('')
      expect(screen.getByText('Arquivo selecionado: cv.pdf')).toBeInTheDocument()
    })

    it('não sobrescreve o que a pessoa já digitou', async () => {
      const user = userEvent.setup()
      mockedParse.mockResolvedValue(found)
      render(<NewCandidatePage />)
      await user.type(field('Nome completo'), 'Digitado à mão')

      await importPdf(user)

      expect(await screen.findByText(/Confira os dados/)).toBeInTheDocument()
      expect(field('Nome completo')).toHaveValue('Digitado à mão')
      expect(field('E-mail')).toHaveValue('maria@example.com')
      expect(field('Telefone')).toHaveValue('41900001234')
    })

    it('com PDF ilegível, mostra o motivo, mantém o formulário e não deixa o PDF como "selecionado"', async () => {
      const user = userEvent.setup()
      mockedParse.mockRejectedValue(new ApiError(422, 'O PDF está protegido por senha. Envie uma versão sem senha.'))
      render(<NewCandidatePage />)
      await user.type(field('Nome completo'), 'Maria da Silva')

      await importPdf(user)

      expect(await screen.findByRole('alert')).toHaveTextContent('O PDF está protegido por senha.')
      expect(field('Nome completo')).toHaveValue('Maria da Silva')
      expect(button('Cadastrar')).toBeEnabled()
      expect(screen.queryByText(/Arquivo selecionado/)).not.toBeInTheDocument()
    })

    it('enquanto lê, avisa e trava o botão Cadastrar e a escolha de outro arquivo', async () => {
      const user = userEvent.setup()
      let finishReading: (data: ResumeExtraction) => void = () => {}
      mockedParse.mockReturnValue(new Promise<ResumeExtraction>((resolve) => { finishReading = resolve }))
      render(<NewCandidatePage />)

      await importPdf(user)

      expect(screen.getByText('Lendo o currículo…')).toBeInTheDocument()
      expect(button('Cadastrar')).toBeDisabled()
      expect(button('escolha o arquivo')).toBeDisabled()

      finishReading(found)

      expect(await screen.findByText(/Currículo lido/)).toBeInTheDocument()
      expect(button('Cadastrar')).toBeEnabled()
      expect(button('escolha o arquivo')).toBeEnabled()
    })
  })

  describe('cadastrar', () => {
    it('envia o que está no formulário, limpa os campos e esquece o PDF escolhido', async () => {
      const user = userEvent.setup()
      mockedParse.mockResolvedValue(found)
      mockedCreate.mockResolvedValue({} as Candidate)
      render(<NewCandidatePage />)
      await importPdf(user)
      await screen.findByText(/Currículo lido/)
      expect(screen.getByText('Arquivo selecionado: cv.pdf')).toBeInTheDocument()

      await user.click(button('Cadastrar'))

      expect(await screen.findByText('Candidato cadastrado com sucesso.')).toBeInTheDocument()
      expect(mockedCreate).toHaveBeenCalledWith({ ...found, position: '', summary: '' })
      expect(field('Nome completo')).toHaveValue('')
      expect(field('E-mail')).toHaveValue('')
      expect(screen.queryByText(/Arquivo selecionado/)).not.toBeInTheDocument()
    })

    it('enquanto cadastra, não deixa escolher outro PDF', async () => {
      const user = userEvent.setup()
      let finishSaving: () => void = () => {}
      mockedCreate.mockReturnValue(new Promise<Candidate>((resolve) => { finishSaving = () => resolve({} as Candidate) }))
      render(<NewCandidatePage />)
      await fillRequired(user)

      await user.click(button('Cadastrar'))

      expect(button('escolha o arquivo')).toBeDisabled()

      finishSaving()

      expect(await screen.findByText('Candidato cadastrado com sucesso.')).toBeInTheDocument()
      expect(button('escolha o arquivo')).toBeEnabled()
    })

    it.each([
      {
        caso: '409 (e-mail repetido)',
        error: new ApiError(409, 'Já existe um candidato cadastrado com este e-mail.'),
        label: 'E-mail',
        shown: 'Já existe um candidato cadastrado com este e-mail.',
      },
      {
        caso: '400 com erro de campo',
        error: new ApiError(400, 'Dados inválidos.', { name: 'Nome inválido.' }),
        label: 'Nome completo',
        shown: 'Nome inválido.',
      },
    ])('$caso: a mensagem aparece embaixo do campo, sem alerta no topo', async ({ error, label, shown }) => {
      const user = userEvent.setup()
      mockedCreate.mockRejectedValue(error)
      render(<NewCandidatePage />)
      await fillRequired(user)

      await user.click(button('Cadastrar'))

      expect(await screen.findByText(shown)).toBeInTheDocument()
      expect(field(label)).toHaveAccessibleDescription(shown)
      expect(screen.getAllByRole('alert')).toHaveLength(1)
    })

    it('em outros erros, mostra o alerta no topo e mantém os dados digitados', async () => {
      const user = userEvent.setup()
      mockedCreate.mockRejectedValue(new ApiError(500, 'Erro inesperado no servidor.'))
      render(<NewCandidatePage />)
      await fillRequired(user)

      await user.click(button('Cadastrar'))

      expect(await screen.findByRole('alert')).toHaveTextContent('Erro inesperado no servidor.')
      expect(field('Nome completo')).toHaveValue('Maria da Silva')
      expect(button('Cadastrar')).toBeEnabled()
    })
  })
})
