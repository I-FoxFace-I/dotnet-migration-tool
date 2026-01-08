import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useAppStore } from '../store/useAppStore'
import { 
  Package, 
  TestTube, 
  FileCode, 
  Box, 
  AlertCircle 
} from 'lucide-react'

export default function Dashboard() {
  const { solution, isLoading, error } = useAppStore()
  
  if (!solution) {
    return (
      <div className="p-8">
        <div className="gradient-header rounded-xl p-8 mb-8">
          <h1 className="text-3xl font-bold">📊 Dashboard</h1>
          <p className="text-white/90 mt-2">Welcome to Migration Tool</p>
        </div>
        
        <div className="card max-w-2xl mx-auto text-center py-12">
          <AlertCircle size={64} className="mx-auto text-gray-400 mb-4" />
          <h2 className="text-2xl font-bold mb-4">No Solution Loaded</h2>
          <p className="text-text-muted mb-6">
            Load a solution to get started with migration planning
          </p>
          <Link to="/settings" className="btn btn-primary">
            Go to Settings
          </Link>
        </div>
      </div>
    )
  }
  
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="loading-spinner"></div>
      </div>
    )
  }
  
  if (error) {
    return (
      <div className="p-8">
        <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-red-800">
          <strong>Error:</strong> {error}
        </div>
      </div>
    )
  }
  
  const stats = [
    {
      label: 'Projects',
      value: solution.projectCount,
      icon: Package,
      color: 'border-primary',
    },
    {
      label: 'Test Projects',
      value: solution.testProjectCount,
      icon: TestTube,
      color: 'border-success',
    },
    {
      label: 'Files',
      value: solution.totalFiles,
      icon: FileCode,
      color: 'border-warning',
    },
    {
      label: 'Classes',
      value: solution.totalClasses,
      icon: Box,
      color: 'border-secondary',
    },
  ]
  
  // Group projects by type
  const projectsByType = solution.projects.reduce((acc, project) => {
    const type = project.projectType || 'Other'
    acc[type] = (acc[type] || 0) + 1
    return acc
  }, {} as Record<string, number>)
  
  const projectTypeIcons: Record<string, string> = {
    ClassLibrary: '📚',
    Console: '⌨️',
    Wpf: '🖼️',
    Test: '🧪',
    WebApi: '🌐',
    Blazor: '⚡',
    Maui: '📱',
  }
  
  return (
    <div className="p-8">
      {/* Header */}
      <div className="gradient-header rounded-xl p-8 mb-8">
        <h1 className="text-3xl font-bold">📊 Dashboard</h1>
        <p className="text-white/90 mt-2">Overview of {solution.name}</p>
      </div>
      
      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {stats.map((stat) => (
          <div key={stat.label} className={`stat-card ${stat.color}`}>
            <stat.icon size={32} className="text-primary mx-auto mb-3" />
            <div className="stat-value">{stat.value}</div>
            <div className="stat-label">{stat.label}</div>
          </div>
        ))}
      </div>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Project Types */}
        <div className="card">
          <h2 className="card-header">📊 Project Types</h2>
          <div className="space-y-3">
            {Object.entries(projectsByType).map(([type, count]) => (
              <div key={type} className="flex items-center justify-between p-3 bg-light rounded-lg">
                <div className="flex items-center gap-3">
                  <span className="text-2xl">{projectTypeIcons[type] || '📦'}</span>
                  <span className="font-medium">{type}</span>
                </div>
                <span className="text-2xl font-bold text-primary">{count}</span>
              </div>
            ))}
          </div>
        </div>
        
        {/* Recent Projects */}
        <div className="card">
          <h2 className="card-header">📦 Projects</h2>
          <div className="space-y-2 max-h-96 overflow-y-auto">
            {solution.projects.slice(0, 10).map((project) => (
              <div
                key={project.name}
                className="flex items-center justify-between p-3 bg-light rounded-lg hover:bg-blue-50 transition-colors cursor-pointer"
              >
                <div className="flex items-center gap-3">
                  <span className="text-xl">{projectTypeIcons[project.projectType] || '📦'}</span>
                  <div>
                    <div className="font-medium">{project.name}</div>
                    <div className="text-sm text-text-muted">{project.rootNamespace}</div>
                  </div>
                </div>
                <div className="text-right text-sm text-text-muted">
                  <div>{project.fileCount} files</div>
                  <div>{project.classCount} classes</div>
                </div>
              </div>
            ))}
          </div>
          {solution.projects.length > 10 && (
            <div className="mt-4 text-center">
              <Link to="/explorer" className="text-primary hover:underline">
                View all {solution.projects.length} projects →
              </Link>
            </div>
          )}
        </div>
      </div>
      
      {/* Quick Actions */}
      <div className="card mt-8">
        <h2 className="card-header">⚡ Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Link to="/explorer" className="btn btn-primary justify-center">
            📁 Browse Projects
          </Link>
          <Link to="/analysis" className="btn btn-secondary justify-center">
            🔬 Analyze Code
          </Link>
          <Link to="/planner" className="btn btn-success justify-center">
            📋 Create Migration Plan
          </Link>
        </div>
      </div>
    </div>
  )
}
