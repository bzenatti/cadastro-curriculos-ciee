import { afterEach, describe, expect, it, vi } from 'vitest'
import { parseResume } from './candidates'

afterEach(() => vi.unstubAllGlobals())

describe('parseResume', () => {
  it('envia o PDF como FormData, na parte "file", sem definir o Content-Type', async () => {
    const found = { name: 'Maria da Silva', email: null, phone: null }
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(found)))
    vi.stubGlobal('fetch', fetchMock)
    const pdf = new File(['%PDF-'], 'cv.pdf', { type: 'application/pdf' })

    await expect(parseResume(pdf)).resolves.toEqual(found)

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/resumes/parse')
    expect(init.method).toBe('POST')
    expect(init.body).toBeInstanceOf(FormData)
    expect((init.body as FormData).get('file')).toHaveProperty('name', 'cv.pdf')
    expect(init.headers).toBeUndefined()
  })
})
