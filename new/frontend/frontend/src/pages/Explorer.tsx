import { useState } from 'react'
import { useAppStore } from '../store/useAppStore'
import { ChevronRight, ChevronDown, File, Folder, AlertCircle } from 'lucide-react'
import { Link } from 'react-router-dom'

export default function Explorer() {
  const { solution, selectedProject, setSelectedProject } = useAppStore()
  const [expandedProjects, setExpandedProjects] = useState<Set<string>>(new Set())
  
  if (!solution) {
    return (
      <div className="p-8">
        <div className="gradient-header rounded-xl p-8 mb-8">
          <h1 className="text-3xl font-bold">📁 Explorer</h1>
          <p className="text-white/90 mt-2">Browse projects, files, and code structure</p>
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
  
  const toggleProject = (projectName: string) => {
    const newExpanded = new Set(expandedProjects)
    if (newExpanded.has(projectName)) {
      newExpanded.delete(projectName)
    } else {
      newExpanded.add(projectName)
    }
    setExpandedProjects(newExpanded)
  }
  
  const projectTypeIcons: Record<string, string> = {
    ClassLibrary: '📚',
    Console: '⌨️',
    Wpf: '🖼️',
    Test: '🧪',
    WebApi: '🌐',
    Blazor: '⚡',
    Maui: '📱',
  }
  
  // Group projects by folder
  const groupedProjects = solution.projects.reduce((acc, project) => {
    const pathParts = project.path.split(/[/\\]/)
    const folder = pathParts.length > 2 ? pathParts[pathParts.length - 3] : 'root'
    if (!acc[folder]) acc[folder] = []
    acc[folder].push(project)
    return acc
  }, {} as Record<string, typeof solution.projects>)
  
  return (
    <div className="p-8">
      {/* Header */}
      <div className="gradient-header rounded-xl p-8 mb-8">
        <h1 className="text-3xl font-bold">📁 Project Explorer</h1>
        <p className="text-white/90 mt-2">Browse projects, files, and code structure</p>
      </div>
      
      {/* Explorer Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Projects Panel */}
        <div className="lg:col-span-1 card max-h-[calc(100vh-300px)] overflow-y-auto">
          <h2 className="text-xl font-bold mb-4 sticky top-0 bg-white pb-2">
            📦 Projects ({solution.projectCount})
          </h2>
          
          <div className="space-y-2">
            {Object.entries(groupedProjects).map(([folder, projects]) => (
              <div key={folder} className="mb-4">
                <div className="text-sm font-semibold text-text-muted mb-2 uppercase">
                  📁 {folder}
                </div>
                {projects.map((project) => (
                  <div
                    key={project.name}
                    className={`p-3 rounded-lg cursor-pointer transition-colors ${
                      selectedProject?.name === project.name
                        ? 'bg-primary text-white'
                        : 'bg-light hover:bg-blue-50'
                    }`}
                    onClick={() => {
                      setSelectedProject(project)
                      toggleProject(project.name)
                    }}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="text-lg">
                          {projectTypeIcons[project.projectType] || '📦'}
                        </span>
                        <span className="font-medium">{project.name}</span>
                      </div>
                      {expandedProjects.has(project.name) ? (
                        <ChevronDown size={16} />
                      ) : (
                        <ChevronRight size={16} />
                      )}
                    </div>
                    
                    {expandedProjects.has(project.name) && (
                      <div className={`mt-2 text-sm ${selectedProject?.name === project.name ? 'text-white/80' : 'text-text-muted'}`}>
                        <div>📄 {project.fileCount} files</div>
                        <div>📦 {project.classCount} classes</div>
                        {project.testCount > 0 && (
                          <div>✅ {project.testCount} tests</div>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            ))}
          </div>
        </div>
        
        {/* Details Panel */}
        <div className="lg:col-span-2 card">
          {selectedProject ? (
            <>
              <div className="flex items-center gap-3 mb-6">
                <span className="text-4xl">
                  {projectTypeIcons[selectedProject.projectType] || '📦'}
                </span>
                <div>
                  <h2 className="text-2xl font-bold">{selectedProject.name}</h2>
                  <p className="text-text-muted">{selectedProject.rootNamespace}</p>
                </div>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                <div>
                  <label className="form-label">Project Type</label>
                  <div className="p-3 bg-light rounded-lg">
                    {selectedProject.projectType}
                  </div>
                </div>
                
                <div>
                  <label className="form-label">Target Framework</label>
                  <div className="p-3 bg-light rounded-lg">
                    {selectedProject.targetFramework}
                  </div>
                </div>
                
                <div>
                  <label className="form-label">Files</label>
                  <div className="p-3 bg-light rounded-lg font-bold text-primary">
                    {selectedProject.fileCount}
                  </div>
                </div>
                
                <div>
                  <label className="form-label">Classes</label>
                  <div className="p-3 bg-light rounded-lg font-bold text-primary">
                    {selectedProject.classCount}
                  </div>
                </div>
              </div>
              
              <div className="border-t pt-6">
                <h3 className="text-lg font-bold mb-4">📄 Files</h3>
                <div className="bg-light rounded-lg p-4 text-center text-text-muted">
                  <File size={48} className="mx-auto mb-2 opacity-50" />
                  <p>File details will be shown here</p>
                  <p className="text-sm mt-2">
                    (Connect to backend to load actual file tree)
                  </p>
                </div>
              </div>
            </>
          ) : (
            <div className="text-center py-12 text-text-muted">
              <Folder size={64} className="mx-auto mb-4 opacity-50" />
              <p>Select a project to view details</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
