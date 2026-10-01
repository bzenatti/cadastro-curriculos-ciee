const DEFAULT_MESSAGE = 'Não foi possível concluir a operação. Tente novamente em instantes.'

// O que a tela precisa saber de uma chamada que falhou.
export class ApiError extends Error {
  readonly status: number // 0 = a requisição nem chegou à API
  readonly fieldErrors: Record<string, string> // campo -> primeira mensagem (só no 400 de validação)

  constructor(status: number, message: string, fieldErrors: Record<string, string> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

// Formato de erro da API; o 400 de validação traz também `errors`.
type ProblemDetails = { detail?: string; errors?: Record<string, string[]> }

async function toApiError(response: Response): Promise<ApiError> {
  const problem: ProblemDetails | null = await response.json().catch(() => null)
  const fieldErrors: Record<string, string> = {}
  for (const [field, messages] of Object.entries(problem?.errors ?? {})) {
    fieldErrors[field] = messages[0]
  }
  return new ApiError(response.status, problem?.detail ?? DEFAULT_MESSAGE, fieldErrors)
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, init)
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.')
  }
  if (!response.ok) throw await toApiError(response)
  return response.json()
}
