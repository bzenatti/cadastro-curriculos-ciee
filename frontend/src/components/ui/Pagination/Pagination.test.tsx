import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Pagination } from './Pagination'

describe('Pagination', () => {
  it('mostra a página atual e o total de páginas', () => {
    render(<Pagination page={2} totalPages={3} onPageChange={() => {}} />)

    expect(screen.getByText('Página 2 de 3')).toBeInTheDocument()
  })

  it('pede a página anterior e a próxima ao clicar nos botões', async () => {
    const user = userEvent.setup()
    const onPageChange = vi.fn()
    render(<Pagination page={2} totalPages={3} onPageChange={onPageChange} />)

    await user.click(screen.getByRole('button', { name: 'Anterior' }))
    await user.click(screen.getByRole('button', { name: 'Próxima' }))

    expect(onPageChange).toHaveBeenNthCalledWith(1, 1)
    expect(onPageChange).toHaveBeenNthCalledWith(2, 3)
  })

  it('desabilita "Anterior" na primeira página e "Próxima" na última', () => {
    const { rerender } = render(<Pagination page={1} totalPages={3} onPageChange={() => {}} />)
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Próxima' })).toBeEnabled()

    rerender(<Pagination page={3} totalPages={3} onPageChange={() => {}} />)
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Próxima' })).toBeDisabled()
  })

  it('não aparece quando há uma página só', () => {
    render(<Pagination page={1} totalPages={1} onPageChange={() => {}} />)

    expect(screen.queryByRole('navigation')).not.toBeInTheDocument()
  })
})
