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

export type CandidateListItem = Pick<Candidate, 'id' | 'name' | 'email' | 'position' | 'createdAt'>

export type Page<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export function createCandidate(values: CandidateFormValues): Promise<Candidate> {
  return request<Candidate>('/api/candidates', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values),
  })
}

export function listCandidates(page: number): Promise<Page<CandidateListItem>> {
  return request<Page<CandidateListItem>>(`/api/candidates?page=${page}`)
}
