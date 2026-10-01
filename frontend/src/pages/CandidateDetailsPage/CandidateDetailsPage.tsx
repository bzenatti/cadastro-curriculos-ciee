import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { getCandidate } from '../../api/candidates'
import type { Candidate } from '../../api/candidates'
import { ApiError } from '../../api/http'
import { Alert } from '../../components/ui/Alert/Alert'
import { Button } from '../../components/ui/Button/Button'
import { formatDate } from '../../format/formatDate'
import './CandidateDetailsPage.css'

const NOT_INFORMED = 'Não informado'

type DetailsState =
  | { status: 'loading' }
  | { status: 'not-found' }
  | { status: 'error'; message: string }
  | { status: 'success'; candidate: Candidate }

type CandidateDetailsProps = {
  id: string
  onRetry: () => void
}

function CandidateDetails({ id, onRetry }: CandidateDetailsProps) {
  const [state, setState] = useState<DetailsState>({ status: 'loading' })

  useEffect(() => {
    let ignore = false
    getCandidate(id).then(
      (candidate) => {
        if (!ignore) setState({ status: 'success', candidate })
      },
      (error: unknown) => {
        if (ignore) return
        // 404 = candidato inexistente (ou id que nem é número: a rota da API exige int e responde 404 sem corpo).
        // Por isso o texto é nosso, e não o da API.
        if (error instanceof ApiError && error.status === 404) {
          setState({ status: 'not-found' })
          return
        }
        const message = error instanceof ApiError ? error.message : 'Não foi possível carregar o candidato.'
        setState({ status: 'error', message })
      },
    )
    return () => {
      ignore = true
    }
  }, [id])

  if (state.status === 'loading') return <Alert>Carregando candidato…</Alert>

  if (state.status === 'not-found') return <h1>Candidato não encontrado</h1>

  if (state.status === 'error') {
    return (
      <>
        <Alert variant="error">{state.message}</Alert>
        <Button variant="secondary" onClick={onRetry}>Tentar de novo</Button>
      </>
    )
  }

  const { candidate } = state

  return (
    <div className="candidate-details_profile">
      <h1>{candidate.name}</h1>
      <dl className="candidate-details_list">
        <dt>E-mail</dt>
        <dd>{candidate.email}</dd>
        <dt>Telefone</dt>
        <dd>{candidate.phone ?? NOT_INFORMED}</dd>
        <dt>Cargo</dt>
        <dd>{candidate.position ?? NOT_INFORMED}</dd>
        <dt>Cadastrado em</dt>
        <dd>{formatDate(candidate.createdAt)}</dd>
        <dt>Resumo</dt>
        <dd className="candidate-details_summary">{candidate.summary ?? NOT_INFORMED}</dd>
      </dl>
    </div>
  )
}

export function CandidateDetailsPage() {
  const { id = '' } = useParams()
  const [attempt, setAttempt] = useState(0)

  return (
    <div className="candidate-details">
      <p><Link to="/">Voltar para a lista de candidatos</Link></p>
      <CandidateDetails key={`${id}-${attempt}`} id={id} onRetry={() => setAttempt((current) => current + 1)} />
    </div>
  )
}
