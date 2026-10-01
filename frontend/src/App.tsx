import { Route, Routes } from 'react-router'
import { Layout } from './components/Layout/Layout'
import { CandidateDetailsPage } from './pages/CandidateDetailsPage/CandidateDetailsPage'
import { CandidateListPage } from './pages/CandidateListPage/CandidateListPage'
import { NewCandidatePage } from './pages/NewCandidatePage/NewCandidatePage'
import { NotFoundPage } from './pages/NotFoundPage/NotFoundPage'
import './App.css'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<CandidateListPage />} />
        <Route path="candidatos/novo" element={<NewCandidatePage />} />
        <Route path="candidatos/:id" element={<CandidateDetailsPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
