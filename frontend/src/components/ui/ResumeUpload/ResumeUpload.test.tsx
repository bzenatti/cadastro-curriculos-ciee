import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ResumeUpload } from './ResumeUpload'

// applyAccept: false deixa o teste enviar um não-PDF; senão o user-event o descarta
// antes de chegar no componente, como o filtro da janela do navegador faz.
const user = userEvent.setup({ applyAccept: false })

const makePdf = () => new File(['%PDF-1.4'], 'cv.pdf', { type: 'application/pdf' })
const getInput = () => screen.getByLabelText('Arquivo PDF do currículo')
const dropOnArea = (file: File) =>
  fireEvent.drop(screen.getByText(/Arraste o currículo/), { dataTransfer: { files: [file] } })


describe('ResumeUpload', () => {
  it('com um PDF válido, mostra o nome e entrega o arquivo para onSelect', async () => {
    const onSelect = vi.fn()
    render(<ResumeUpload onSelect={onSelect} />)
    const pdf = makePdf()

    await user.upload(getInput(), pdf)

    expect(onSelect).toHaveBeenCalledWith(pdf)
    expect(screen.getByText('Arquivo selecionado: cv.pdf')).toBeInTheDocument()
  })

  it.each([
    {
      motivo: 'não é PDF',
      file: new File(['texto'], 'notas.txt', { type: 'text/plain' }),
      mensagem: 'O arquivo precisa ser um PDF.',
    },
    {
      motivo: 'passa de 5 MB',
      file: new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'grande.pdf', { type: 'application/pdf' }),
      mensagem: 'O PDF deve ter no máximo 5 MB.',
    },
  ])('recusa o arquivo se $motivo: mostra o erro e não chama onSelect', async ({ file, mensagem }) => {
    const onSelect = vi.fn()
    render(<ResumeUpload onSelect={onSelect} />)

    await user.upload(getInput(), file)

    expect(screen.getByRole('alert')).toHaveTextContent(mensagem)
    expect(onSelect).not.toHaveBeenCalled()
  })

  it('escolher o mesmo arquivo de novo entrega o arquivo outra vez', async () => {
    const onSelect = vi.fn()
    render(<ResumeUpload onSelect={onSelect} />)
    const pdf = makePdf()

    await user.upload(getInput(), pdf)
    await user.upload(getInput(), pdf)

    expect(onSelect).toHaveBeenCalledTimes(2)
  })

  it('um arquivo inválido limpa o arquivo escolhido antes', async () => {
    render(<ResumeUpload onSelect={vi.fn()} />)

    await user.upload(getInput(), makePdf())
    await user.upload(getInput(), new File(['texto'], 'notas.txt', { type: 'text/plain' }))

    expect(screen.queryByText(/Arquivo selecionado/)).not.toBeInTheDocument()
  })

  it('soltar um PDF válido mostra o nome e entrega o arquivo para onSelect', () => {
    const onSelect = vi.fn()
    render(<ResumeUpload onSelect={onSelect} />)
    const pdf = makePdf()

    dropOnArea(pdf)

    expect(onSelect).toHaveBeenCalledWith(pdf)
    expect(screen.getByText('Arquivo selecionado: cv.pdf')).toBeInTheDocument()
  })

  it('soltar um arquivo inválido mostra o erro e não chama onSelect', () => {
    const onSelect = vi.fn()
    render(<ResumeUpload onSelect={onSelect} />)

    dropOnArea(new File(['texto'], 'notas.txt', { type: 'text/plain' }))

    expect(screen.getByRole('alert')).toHaveTextContent('O arquivo precisa ser um PDF.')
    expect(onSelect).not.toHaveBeenCalled()
  })

  it('desabilitado, o botão não funciona e soltar um PDF não chama onSelect', () => {
    const onSelect = vi.fn()
    render(<ResumeUpload onSelect={onSelect} disabled />)

    dropOnArea(makePdf())

    expect(screen.getByRole('button', { name: 'escolha o arquivo' })).toBeDisabled()
    expect(onSelect).not.toHaveBeenCalled()
  })
})
