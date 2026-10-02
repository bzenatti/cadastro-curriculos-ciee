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

export function getCandidate(id: string): Promise<Candidate> {
  return request<Candidate>(`/api/candidates/${encodeURIComponent(id)}`)
}

export type ResumeExtraction = { name: string | null; email: string | null; phone: string | null }

// Sem Content-Type: o navegador define "multipart/form-data" com o boundary do FormData.
export function parseResume(file: File): Promise<ResumeExtraction> {
  const body = new FormData()
  body.append('file', file)
  return request<ResumeExtraction>('/api/resumes/parse', { method: 'POST', body })
}
