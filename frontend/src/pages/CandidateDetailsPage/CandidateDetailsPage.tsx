import { useParams } from 'react-router'

// @TODO: dados do candidato.
export function CandidateDetailsPage() {
  const { id } = useParams()
  return <h1>Candidato {id}</h1>
}
