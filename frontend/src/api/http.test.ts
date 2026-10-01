import { afterEach, describe, expect, it, vi } from 'vitest'
import { request } from './http'

function respondWith(status: number, body: object | string) {
  const text = typeof body === 'string' ? body : JSON.stringify(body)
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(text, { status })))
}

afterEach(() => vi.unstubAllGlobals())

describe('request', () => {
  it('devolve o corpo JSON quando a resposta é de sucesso', async () => {
    respondWith(201, { id: 7 })

    await expect(request('/api/x')).resolves.toEqual({ id: 7 })
  })

  it('no 400 de validação, guarda a primeira mensagem de cada campo', async () => {
    respondWith(400, { errors: { email: ['Informe o e-mail.', 'outra'], name: ['Informe o nome completo.'] } })

    await expect(request('/api/x')).rejects.toMatchObject({
      status: 400,
      fieldErrors: { email: 'Informe o e-mail.', name: 'Informe o nome completo.' },
    })
  })

  it('usa o detail da API como mensagem (ex.: 409)', async () => {
    respondWith(409, { detail: 'Já existe um candidato cadastrado com este e-mail.' })

    await expect(request('/api/x')).rejects.toMatchObject({
      status: 409,
      message: 'Já existe um candidato cadastrado com este e-mail.',
    })
  })

  it('com corpo que não é JSON (ex.: erro do proxy), usa a mensagem padrão', async () => {
    respondWith(500, '')

    await expect(request('/api/x')).rejects.toMatchObject({
      status: 500,
      message: expect.stringContaining('Tente novamente'),
    })
  })

  it('sem conexão com o servidor, lança erro com status 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(request('/api/x')).rejects.toMatchObject({ status: 0 })
  })
})
