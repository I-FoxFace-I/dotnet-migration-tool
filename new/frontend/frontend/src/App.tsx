import { Routes, Route } from 'react-router-dom'
import Layout from './components/layout/Layout'
import Dashboard from './pages/Dashboard'
import Explorer from './pages/Explorer'
import Analysis from './pages/Analysis'
import Planner from './pages/Planner'
import Settings from './pages/Settings'

function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/explorer" element={<Explorer />} />
        <Route path="/analysis" element={<Analysis />} />
        <Route path="/planner" element={<Planner />} />
        <Route path="/settings" element={<Settings />} />
      </Routes>
    </Layout>
  )
}

export default App
