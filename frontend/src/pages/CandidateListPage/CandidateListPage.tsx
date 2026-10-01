import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { listCandidates } from '../../api/candidates'
import type { CandidateListItem, Page } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { Alert } from '../../components/ui/Alert/Alert'
import { Button } from '../../components/ui/Button/Button'
import { Pagination } from '../../components/ui/Pagination/Pagination'
import './CandidateListPage.css'

type ListState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'success'; data: Page<CandidateListItem> }

type CandidateListProps = {
  page: number
  onPageChange: (page: number) => void
  onRetry: () => void
}

// Página inválida na URL (abc, 0, -3) vira a primeira, para nunca mandar lixo à API.
function parsePage(value: string | null): number {
  const page = Number(value)
  return Number.isInteger(page) && page >= 1 ? page : 1
}

function CandidateList({ page, onPageChange, onRetry }: CandidateListProps) {
  const [state, setState] = useState<ListState>({ status: 'loading' })

  useEffect(() => {
    let ignore = false
    listCandidates(page).then(
      (data) => {
        if (!ignore) setState({ status: 'success', data })
      },
      (error: unknown) => {
        if (ignore) return
        const message = error instanceof ApiError ? error.message : 'Não foi possível carregar os candidatos.'
        setState({ status: 'error', message })
      },
    )
    return () => {
      ignore = true
    }
  }, [page])

  if (state.status === 'loading') return <Alert>Carregando candidatos…</Alert>

  if (state.status === 'error') {
    return (
      <>
        <Alert variant="error">{state.message}</Alert>
        <Button variant="secondary" onClick={onRetry}>Tentar de novo</Button>
      </>
    )
  }

  const { data } = state

  if (data.items.length === 0) {
    return <p>Nenhum candidato cadastrado ainda. <Link to="/candidatos/novo">Cadastrar o primeiro</Link></p>
  }

  // O servidor ajusta páginas fora do intervalo e devolve a página real; é ela que vale.
  const totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize))

  return (
    <>
      <div className="candidate-list_scroll">
        <table className="candidate-list_table">
          <thead>
            <tr>
              <th scope="col">Nome</th>
              <th scope="col">E-mail</th>
              <th scope="col">Cargo</th>
              <th scope="col">Cadastrado em</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((candidate) => (
              <tr key={candidate.id}>
                <td><Link to={`/candidatos/${candidate.id}`}>{candidate.name}</Link></td>
                <td>{candidate.email}</td>
                <td>{candidate.position ?? 'Não informado'}</td>
                <td className="candidate-list_date">{new Date(candidate.createdAt).toLocaleDateString('pt-BR')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination page={data.page} totalPages={totalPages} onPageChange={onPageChange} />
    </>
  )
}

export function CandidateListPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [attempt, setAttempt] = useState(0)
  const page = parsePage(searchParams.get('page'))

  return (
    <>
      <h1>Candidatos</h1>
      <CandidateList
        key={`${page}-${attempt}`}
        page={page}
        onPageChange={(next) => setSearchParams({ page: String(next) })}
        onRetry={() => setAttempt((current) => current + 1)}
      />
    </>
  )
}
