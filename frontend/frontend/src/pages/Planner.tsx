import { useState } from 'react'
import { useAppStore, MigrationStep } from '../store/useAppStore'
import { Link } from 'react-router-dom'
import { 
  AlertCircle, 
  Plus, 
  Play, 
  Save, 
  FolderOpen,
  FileText,
  FolderTree,
  Copy,
  Trash,
  GitBranch
} from 'lucide-react'
import { migrationClient } from '../lib/grpc-client'

export default function Planner() {
  const { 
    solution, 
    migrationPlan, 
    setMigrationPlan, 
    addMigrationStep, 
    removeMigrationStep,
    selectedStep,
    setSelectedStep 
  } = useAppStore()
  
  const [isExecuting, setIsExecuting] = useState(false)
  const [progress, setProgress] = useState(0)
  const [validationErrors, setValidationErrors] = useState<string[]>([])
  
  if (!solution) {
    return (
      <div className="p-8">
        <div className="gradient-header rounded-xl p-8 mb-8">
          <h1 className="text-3xl font-bold">📋 Migration Planner</h1>
          <p className="text-white/90 mt-2">
            Plan and execute migration operations
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
  
  const quickActions = [
    { id: 'move-file', icon: FileText, label: 'Move File', action: 'MOVE_FILE' },
    { id: 'move-folder', icon: FolderTree, label: 'Move Folder', action: 'MOVE_FOLDER' },
    { id: 'copy-file', icon: Copy, label: 'Copy File', action: 'COPY_FILE' },
    { id: 'copy-folder', icon: FolderOpen, label: 'Copy Folder', action: 'COPY_FOLDER' },
    { id: 'rename-ns', icon: GitBranch, label: 'Rename Namespace', action: 'RENAME_NAMESPACE' },
  ]
  
  const handleAddStep = (action: string) => {
    const newStep: MigrationStep = {
      index: (migrationPlan?.steps.length || 0) + 1,
      action,
      source: '',
      target: '',
      status: 'PENDING',
    }
    addMigrationStep(newStep)
  }
  
  const handleExecute = async () => {
    if (!migrationPlan || migrationPlan.steps.length === 0) return
    
    setIsExecuting(true)
    setProgress(0)
    setValidationErrors([])
    
    try {
      // Validate first
      const validation = await migrationClient.validatePlan(migrationPlan)
      if (!validation.isValid) {
        setValidationErrors(validation.errors)
        setIsExecuting(false)
        return
      }
      
      // Execute with progress
      await migrationClient.executeMigration(migrationPlan, (progressData) => {
        setProgress(progressData.percentComplete)
      })
      
      alert('Migration completed successfully!')
    } catch (error) {
      alert('Migration failed: ' + (error as Error).message)
    } finally {
      setIsExecuting(false)
      setProgress(0)
    }
  }
  
  return (
    <div className="p-8">
      {/* Header */}
      <div className="gradient-header rounded-xl p-8 mb-8">
        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-3xl font-bold">📋 Migration Planner</h1>
            <p className="text-white/90 mt-2">
              Plan and execute migration operations
            </p>
          </div>
          
          <div className="flex gap-3">
            <button className="btn bg-white text-primary hover:bg-gray-100">
              <FolderOpen size={20} />
              Load Plan
            </button>
            <button 
              className="btn bg-white text-primary hover:bg-gray-100"
              disabled={!migrationPlan || migrationPlan.steps.length === 0}
            >
              <Save size={20} />
              Save Plan
            </button>
            <button
              className="btn btn-success"
              onClick={handleExecute}
              disabled={!migrationPlan || migrationPlan.steps.length === 0 || isExecuting}
            >
              <Play size={20} />
              {isExecuting ? 'Executing...' : 'Execute Plan'}
            </button>
          </div>
        </div>
      </div>
      
      {/* 3-Panel Layout */}
      <div className="grid grid-cols-12 gap-6">
        {/* Left Panel: Quick Actions */}
        <div className="col-span-3 card max-h-[calc(100vh-350px)] overflow-y-auto">
          <h2 className="text-lg font-bold mb-4">⚡ Quick Actions</h2>
          
          <div className="space-y-2">
            {quickActions.map((action) => (
              <button
                key={action.id}
                className="w-full flex items-center gap-3 p-3 bg-gradient-to-r from-light to-gray-200 rounded-lg hover:from-primary hover:to-success hover:text-white transition-all duration-200"
                onClick={() => handleAddStep(action.action)}
              >
                <action.icon size={24} />
                <span className="font-medium">{action.label}</span>
              </button>
            ))}
          </div>
          
          <div className="mt-6 pt-6 border-t border-border">
            <h3 className="text-sm font-bold mb-3 text-text-muted">📊 SOLUTION INFO</h3>
            <div className="text-sm space-y-2 text-text-muted">
              <div><strong>Name:</strong> {solution.name}</div>
              <div><strong>Projects:</strong> {solution.projectCount}</div>
            </div>
          </div>
          
          <div className="mt-6 bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <h3 className="text-sm font-bold mb-2 text-yellow-800">💡 Tips</h3>
            <ul className="text-xs text-yellow-700 space-y-1">
              <li>• Use Quick Actions to add steps</li>
              <li>• Click steps to edit details</li>
              <li>• Validate before executing</li>
            </ul>
          </div>
        </div>
        
        {/* Center Panel: Migration Steps */}
        <div className="col-span-6 card">
          <div className="flex justify-between items-center mb-6">
            <div>
              <input
                type="text"
                placeholder="Migration Plan Name"
                className="text-2xl font-bold border-b-2 border-transparent hover:border-primary focus:border-primary outline-none bg-transparent"
                value={migrationPlan?.name || 'New Migration Plan'}
                onChange={(e) =>
                  setMigrationPlan({
                    ...migrationPlan!,
                    name: e.target.value,
                  })
                }
              />
              <p className="text-sm text-text-muted mt-1">
                {migrationPlan?.steps.length || 0} step(s)
              </p>
            </div>
          </div>
          
          {/* Validation Errors */}
          {validationErrors.length > 0 && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
              <h4 className="font-bold text-red-800 mb-2">⚠️ Validation Errors</h4>
              <ul className="list-disc list-inside text-sm text-red-700">
                {validationErrors.map((error, idx) => (
                  <li key={idx}>{error}</li>
                ))}
              </ul>
            </div>
          )}
          
          {/* Progress Bar */}
          {isExecuting && (
            <div className="mb-4">
              <div className="flex justify-between text-sm mb-2">
                <span>Executing migration...</span>
                <span className="font-bold">{Math.round(progress)}%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-3">
                <div
                  className="bg-gradient-to-r from-primary to-success h-3 rounded-full transition-all duration-300"
                  style={{ width: `${progress}%` }}
                />
              </div>
            </div>
          )}
          
          {/* Steps List */}
          <div className="space-y-3 max-h-[calc(100vh-550px)] overflow-y-auto">
            {migrationPlan?.steps.length === 0 || !migrationPlan ? (
              <div className="text-center py-12 text-text-muted">
                <Plus size={48} className="mx-auto mb-4 opacity-50" />
                <p>No migration steps defined</p>
                <p className="text-sm mt-2">Use Quick Actions to add steps</p>
              </div>
            ) : (
              migrationPlan.steps.map((step, idx) => (
                <div
                  key={idx}
                  className={`p-4 rounded-lg cursor-pointer transition-all ${
                    selectedStep === step
                      ? 'bg-primary text-white ring-2 ring-primary'
                      : 'bg-light hover:bg-blue-50'
                  }`}
                  onClick={() => setSelectedStep(step)}
                >
                  <div className="flex justify-between items-start">
                    <div className="flex gap-3">
                      <div className="text-2xl font-bold opacity-50">
                        #{step.index}
                      </div>
                      <div className="flex-1">
                        <div className="font-bold mb-1">{step.action.replace('_', ' ')}</div>
                        <div className={`text-sm ${selectedStep === step ? 'text-white/80' : 'text-text-muted'}`}>
                          {step.source || '(not set)'} → {step.target || '(not set)'}
                        </div>
                      </div>
                    </div>
                    
                    <button
                      className="text-red-500 hover:text-red-700"
                      onClick={(e) => {
                        e.stopPropagation()
                        removeMigrationStep(idx)
                        if (selectedStep === step) setSelectedStep(null)
                      }}
                    >
                      <Trash size={18} />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
        
        {/* Right Panel: Step Details */}
        <div className="col-span-3 card max-h-[calc(100vh-350px)] overflow-y-auto">
          <h2 className="text-lg font-bold mb-4">📝 Step Details</h2>
          
          {selectedStep ? (
            <div className="space-y-4">
              <div>
                <label className="form-label">Action Type</label>
                <div className="p-3 bg-primary text-white rounded-lg font-bold">
                  {selectedStep.action.replace('_', ' ')}
                </div>
              </div>
              
              <div>
                <label className="form-label">Source</label>
                <input
                  type="text"
                  className="form-control font-mono text-sm"
                  value={selectedStep.source}
                  placeholder="Enter source path"
                  onChange={(e) => {
                    const updated = { ...selectedStep, source: e.target.value }
                    setSelectedStep(updated)
                    // TODO: Update in plan
                  }}
                />
              </div>
              
              <div>
                <label className="form-label">Target</label>
                <input
                  type="text"
                  className="form-control font-mono text-sm"
                  value={selectedStep.target}
                  placeholder="Enter target path"
                  onChange={(e) => {
                    const updated = { ...selectedStep, target: e.target.value }
                    setSelectedStep(updated)
                    // TODO: Update in plan
                  }}
                />
              </div>
              
              <div className="pt-4 border-t border-border">
                <h4 className="font-bold mb-2">ℹ️ Description</h4>
                <p className="text-sm text-text-muted">
                  This operation will {selectedStep.action.toLowerCase().replace('_', ' ')}{' '}
                  from <span className="font-mono">{selectedStep.source || '...'}</span>{' '}
                  to <span className="font-mono">{selectedStep.target || '...'}</span>
                </p>
              </div>
            </div>
          ) : (
            <div className="text-center py-12 text-text-muted">
              <FileText size={48} className="mx-auto mb-4 opacity-50" />
              <p>Select a step to view details</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
