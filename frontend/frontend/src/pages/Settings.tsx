import { useState } from 'react'
import { useAppStore } from '../store/useAppStore'
import { migrationClient } from '../lib/grpc-client'
import { FolderOpen, Upload, Trash, CheckCircle } from 'lucide-react'

export default function Settings() {
  const { solution, setSolution, setLoading, setError } = useAppStore()
  const [solutionPath, setSolutionPath] = useState('')
  const [isAnalyzing, setIsAnalyzing] = useState(false)
  const [successMessage, setSuccessMessage] = useState('')
  
  const handleLoadSolution = async () => {
    if (!solutionPath) {
      setError('Please enter a solution path')
      return
    }
    
    setIsAnalyzing(true)
    setError(null)
    setSuccessMessage('')
    
    try {
      const analyzedSolution = await migrationClient.analyzeSolution(solutionPath)
      setSolution(analyzedSolution)
      setSuccessMessage(`✅ Successfully loaded: ${analyzedSolution.name}`)
    } catch (error) {
      setError('Failed to load solution: ' + (error as Error).message)
    } finally {
      setIsAnalyzing(false)
    }
  }
  
  const handleClearSolution = () => {
    setSolution(null)
    setSolutionPath('')
    setSuccessMessage('Solution cleared')
    setTimeout(() => setSuccessMessage(''), 3000)
  }
  
  // Mock recent paths
  const recentPaths = [
    'C:\\Projects\\MyApp\\MyApp.sln',
    'C:\\Projects\\Framework\\Framework.sln',
    'C:\\Projects\\Sample\\Sample.sln',
  ]
  
  return (
    <div className="p-8">
      {/* Header */}
      <div className="gradient-header rounded-xl p-8 mb-8">
        <h1 className="text-3xl font-bold">⚙️ Settings</h1>
        <p className="text-white/90 mt-2">
          Load solutions and configure Migration Tool
        </p>
      </div>
      
      {/* Main Content */}
      <div className="max-w-4xl mx-auto space-y-8">
        {/* Load Solution Section */}
        <div className="card">
          <h2 className="card-header">📁 Load Solution</h2>
          
          <div className="space-y-4">
            <div>
              <label className="form-label">Solution Path</label>
              <div className="flex gap-3">
                <input
                  type="text"
                  className="form-control flex-1"
                  placeholder="C:\path\to\your\solution.sln"
                  value={solutionPath}
                  onChange={(e) => setSolutionPath(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleLoadSolution()}
                />
                <button className="btn btn-secondary">
                  <FolderOpen size={20} />
                  Browse
                </button>
              </div>
              <p className="text-sm text-text-muted mt-2">
                Enter the full path to your .sln file
              </p>
            </div>
            
            <div className="flex gap-3">
              <button
                className="btn btn-primary"
                onClick={handleLoadSolution}
                disabled={isAnalyzing || !solutionPath}
              >
                {isAnalyzing ? (
                  <>
                    <div className="loading-spinner w-5 h-5 mr-2" />
                    Analyzing...
                  </>
                ) : (
                  <>
                    <Upload size={20} />
                    Load Solution
                  </>
                )}
              </button>
              
              {solution && (
                <button
                  className="btn btn-danger"
                  onClick={handleClearSolution}
                >
                  <Trash size={20} />
                  Clear Solution
                </button>
              )}
            </div>
          </div>
          
          {/* Recent Paths */}
          {recentPaths.length > 0 && (
            <div className="mt-6 pt-6 border-t border-border">
              <h3 className="text-sm font-bold mb-3 text-text-muted">
                🕐 RECENT SOLUTIONS
              </h3>
              <div className="space-y-2">
                {recentPaths.map((path, idx) => (
                  <div
                    key={idx}
                    className="flex items-center justify-between p-3 bg-light rounded-lg hover:bg-blue-50 cursor-pointer transition-colors"
                    onClick={() => setSolutionPath(path)}
                  >
                    <span className="text-sm font-mono">{path}</span>
                    <FolderOpen size={16} className="text-primary" />
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
        
        {/* Current Solution Info */}
        {solution && (
          <div className="card bg-gradient-to-br from-primary/10 to-success/10 border-2 border-primary/30">
            <div className="flex items-start gap-4">
              <CheckCircle size={48} className="text-success" />
              <div className="flex-1">
                <h2 className="text-2xl font-bold mb-2">✅ Current Solution</h2>
                <div className="space-y-2">
                  <div>
                    <span className="font-semibold">Name:</span> {solution.name}
                  </div>
                  <div>
                    <span className="font-semibold">Path:</span>{' '}
                    <span className="font-mono text-sm">{solution.path}</span>
                  </div>
                  <div className="flex gap-6 mt-4">
                    <div>
                      <div className="text-3xl font-bold text-primary">
                        {solution.projectCount}
                      </div>
                      <div className="text-sm text-text-muted">Projects</div>
                    </div>
                    <div>
                      <div className="text-3xl font-bold text-success">
                        {solution.testProjectCount}
                      </div>
                      <div className="text-sm text-text-muted">Test Projects</div>
                    </div>
                    <div>
                      <div className="text-3xl font-bold text-warning">
                        {solution.totalFiles}
                      </div>
                      <div className="text-sm text-text-muted">Files</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
        
        {/* Success Message */}
        {successMessage && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-6 text-green-800">
            {successMessage}
          </div>
        )}
        
        {/* Error Message */}
        {/* This uses the global error state from store, you could also use local state */}
        
        {/* Language Settings */}
        <div className="card">
          <h2 className="card-header">🌐 Language</h2>
          
          <div>
            <label className="form-label">Interface Language</label>
            <select className="form-control max-w-xs">
              <option value="en">🇬🇧 English</option>
              <option value="cs">🇨🇿 Čeština</option>
              <option value="pl">🇵🇱 Polski</option>
              <option value="uk">🇺🇦 Українська</option>
            </select>
            <p className="text-sm text-text-muted mt-2">
              Select your preferred interface language
            </p>
          </div>
        </div>
        
        {/* Backend Connection */}
        <div className="card">
          <h2 className="card-header">🔌 Backend Connection</h2>
          
          <div className="space-y-4">
            <div>
              <label className="form-label">gRPC Server URL</label>
              <input
                type="text"
                className="form-control"
                value="http://localhost:5000"
                readOnly
              />
              <p className="text-sm text-text-muted mt-2">
                Make sure the gRPC server is running on this address
              </p>
            </div>
            
            <div className="flex items-center gap-3 p-4 bg-light rounded-lg">
              <div className="w-3 h-3 bg-success rounded-full animate-pulse"></div>
              <span className="text-sm font-medium">Server Status: Connected</span>
            </div>
          </div>
        </div>
        
        {/* About */}
        <div className="card">
          <h2 className="card-header">ℹ️ About</h2>
          
          <div className="space-y-3 text-sm text-text-muted">
            <div>
              <strong>Version:</strong> 1.0.0
            </div>
            <div>
              <strong>Technology:</strong> React + TypeScript + gRPC
            </div>
            <div>
              <strong>Backend:</strong> .NET 9 + Roslyn
            </div>
            <div className="pt-4 border-t border-border">
              <p>
                Migration Tool helps you analyze, reorganize, and migrate .NET projects
                with ease. Built with Roslyn for accurate code analysis.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
