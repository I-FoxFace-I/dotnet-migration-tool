import { useState } from 'react'
import { useAppStore } from '../store/useAppStore'
import { Link } from 'react-router-dom'
import { AlertCircle, Package, GitBranch, Box } from 'lucide-react'

type TabType = 'namespaces' | 'dependencies' | 'packages'

export default function Analysis() {
  const { solution } = useAppStore()
  const [activeTab, setActiveTab] = useState<TabType>('namespaces')
  
  if (!solution) {
    return (
      <div className="p-8">
        <div className="gradient-header rounded-xl p-8 mb-8">
          <h1 className="text-3xl font-bold">🔬 Code Analysis</h1>
          <p className="text-white/90 mt-2">
            Analyze namespaces, dependencies, and potential conflicts
          </p>
        </div>
        
        <div className="card text-center py-12">
          <AlertCircle size={64} className="mx-auto text-gray-400 mb-4" />
          <p className="text-text-muted mb-6">No solution loaded</p>
          <Link to="/settings" className="btn btn-primary">
            Load Solution
          </Link>
        </div>
      </div>
    )
  }
  
  // Mock namespace data
  const namespaces = [
    { name: 'MyApp.Core', fileCount: 25, typeCount: 45, projects: ['Core.Library'] },
    { name: 'MyApp.Core.Tests', fileCount: 15, typeCount: 20, projects: ['Core.Tests'] },
    { name: 'MyApp.Services', fileCount: 40, typeCount: 60, projects: ['Services.Api'] },
    { name: 'MyApp.UI', fileCount: 35, typeCount: 50, projects: ['UI.Wpf'] },
  ]
  
  // Mock package data
  const packages = [
    {
      name: 'xunit',
      usages: [
        { project: 'Core.Tests', version: '2.9.0' },
        { project: 'Services.Tests', version: '2.9.0' },
      ],
    },
    {
      name: 'Moq',
      usages: [
        { project: 'Core.Tests', version: '4.20.0' },
        { project: 'Services.Tests', version: '4.18.0' },
      ],
    },
    {
      name: 'FluentAssertions',
      usages: [
        { project: 'Core.Tests', version: '7.0.0' },
        { project: 'Services.Tests', version: '6.12.0' },
      ],
    },
  ]
  
  return (
    <div className="p-8">
      {/* Header */}
      <div className="gradient-header rounded-xl p-8 mb-8">
        <h1 className="text-3xl font-bold">🔬 Code Analysis</h1>
        <p className="text-white/90 mt-2">
          Analyze namespaces, dependencies, and potential conflicts across projects
        </p>
      </div>
      
      {/* Tabs */}
      <div className="flex gap-2 mb-6 border-b-2 border-border">
        <button
          className={`tab ${activeTab === 'namespaces' ? 'active' : ''}`}
          onClick={() => setActiveTab('namespaces')}
        >
          <Box size={20} />
          Namespaces
        </button>
        <button
          className={`tab ${activeTab === 'dependencies' ? 'active' : ''}`}
          onClick={() => setActiveTab('dependencies')}
        >
          <GitBranch size={20} />
          Dependencies
        </button>
        <button
          className={`tab ${activeTab === 'packages' ? 'active' : ''}`}
          onClick={() => setActiveTab('packages')}
        >
          <Package size={20} />
          Packages
        </button>
      </div>
      
      {/* Tab Content */}
      <div className="card">
        {activeTab === 'namespaces' && (
          <div>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">📦 Namespaces</h2>
              <div className="text-text-muted">
                {namespaces.length} unique namespaces
              </div>
            </div>
            
            <div className="overflow-x-auto">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Namespace</th>
                    <th>Files</th>
                    <th>Types</th>
                    <th>Projects</th>
                  </tr>
                </thead>
                <tbody>
                  {namespaces.map((ns) => (
                    <tr key={ns.name}>
                      <td className="font-mono font-semibold">{ns.name}</td>
                      <td>{ns.fileCount}</td>
                      <td>{ns.typeCount}</td>
                      <td>
                        {ns.projects.map((proj) => (
                          <span
                            key={proj}
                            className="inline-block bg-blue-100 text-primary px-2 py-1 rounded mr-2 text-sm"
                          >
                            {proj}
                          </span>
                        ))}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
        
        {activeTab === 'dependencies' && (
          <div>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">🔗 Project Dependencies</h2>
              <div className="text-text-muted">
                {solution.projectCount} projects
              </div>
            </div>
            
            <div className="bg-light rounded-lg p-8 text-center">
              <GitBranch size={64} className="mx-auto mb-4 text-gray-400" />
              <p className="text-lg font-medium mb-2">
                Dependency Graph Visualization
              </p>
              <p className="text-text-muted mb-6">
                This will use React Flow to display an interactive dependency graph
              </p>
              <div className="inline-flex gap-4">
                <div className="text-center">
                  <div className="text-3xl font-bold text-primary">
                    {solution.projectCount}
                  </div>
                  <div className="text-sm text-text-muted">Nodes</div>
                </div>
                <div className="text-center">
                  <div className="text-3xl font-bold text-primary">
                    ~{solution.projectCount * 2}
                  </div>
                  <div className="text-sm text-text-muted">Edges</div>
                </div>
              </div>
              <p className="text-sm text-text-muted mt-6">
                (Connect to backend API to load actual graph data)
              </p>
            </div>
          </div>
        )}
        
        {activeTab === 'packages' && (
          <div>
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-bold">📦 NuGet Packages</h2>
              <div className="text-text-muted">
                {packages.length} packages
              </div>
            </div>
            
            <div className="space-y-6">
              {packages.map((pkg) => (
                <div key={pkg.name} className="bg-light rounded-lg p-6">
                  <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center gap-3">
                      <Package size={32} className="text-primary" />
                      <div>
                        <h3 className="text-xl font-bold">{pkg.name}</h3>
                        <p className="text-sm text-text-muted">
                          Used in {pkg.usages.length} project(s)
                        </p>
                      </div>
                    </div>
                    
                    {pkg.usages.length > 1 &&
                      new Set(pkg.usages.map((u) => u.version)).size > 1 && (
                        <span className="bg-warning text-white px-3 py-1 rounded-full text-sm font-medium">
                          ⚠️ Version Conflict
                        </span>
                      )}
                  </div>
                  
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead className="text-left text-sm text-text-muted">
                        <tr>
                          <th className="pb-2">Project</th>
                          <th className="pb-2">Version</th>
                        </tr>
                      </thead>
                      <tbody>
                        {pkg.usages.map((usage, idx) => (
                          <tr key={idx}>
                            <td className="py-2">{usage.project}</td>
                            <td className="py-2 font-mono">{usage.version}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
