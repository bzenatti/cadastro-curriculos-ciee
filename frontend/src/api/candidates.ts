import type { CandidateFormValues } from '../components/CandidateForm/CandidateForm'
import { request } from './http'

export type Candidate = {
  id: number
  name: string
  email: string
  phone: string | null
  position: string | null
  summary: string | null
  createdAt: string
}

export function createCandidate(values: CandidateFormValues): Promise<Candidate> {
  return request<Candidate>('/api/candidates', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
}
