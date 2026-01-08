import { NavLink } from 'react-router-dom'
import { 
  LayoutDashboard, 
  FolderTree, 
  BarChart3, 
  ClipboardList, 
  Settings as SettingsIcon 
} from 'lucide-react'
import { useAppStore } from '../../store/useAppStore'

interface LayoutProps {
  children: React.ReactNode
}

export default function Layout({ children }: LayoutProps) {
  const solution = useAppStore(state => state.solution)
  
  const navItems = [
    { to: '/', icon: LayoutDashboard, label: '📊 Dashboard' },
    { to: '/explorer', icon: FolderTree, label: '📁 Explorer' },
    { to: '/analysis', icon: BarChart3, label: '🔬 Analysis' },
    { to: '/planner', icon: ClipboardList, label: '📋 Planner' },
    { to: '/settings', icon: SettingsIcon, label: '⚙️ Settings' },
  ]
  
  return (
    <div className="flex h-screen overflow-hidden">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="px-6 py-6">
          <h1 className="text-2xl font-bold">🔄 Migration Tool</h1>
          {solution && (
            <p className="text-sm text-gray-400 mt-2 truncate">{solution.name}</p>
          )}
        </div>
        
        <nav className="mt-8">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `sidebar-item ${isActive ? 'active' : ''}`
              }
            >
              <Icon size={20} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        
        <div className="absolute bottom-0 left-0 right-0 p-6 border-t border-gray-700">
          <p className="text-xs text-gray-500 text-center">
            Version 1.0.0 - React + gRPC
          </p>
        </div>
      </aside>
      
      {/* Main Content */}
      <main className="flex-1 overflow-auto ml-64 bg-light">
        {children}
      </main>
    </div>
  )
}
