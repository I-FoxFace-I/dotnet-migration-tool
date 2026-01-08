import { create } from 'zustand'

// Types (these will be replaced with generated protobuf types)
export interface ProjectInfo {
  name: string
  path: string
  targetFramework: string
  projectType: string
  rootNamespace: string
  isTestProject: boolean
  fileCount: number
  classCount: number
  testCount: number
}

export interface SolutionInfo {
  name: string
  path: string
  projectCount: number
  testProjectCount: number
  sourceProjectCount: number
  totalFiles: number
  totalClasses: number
  totalTests: number
  projects: ProjectInfo[]
}

export interface MigrationStep {
  index: number
  action: string
  source: string
  target: string
  status: string
  error?: string
}

export interface MigrationPlan {
  name: string
  description: string
  steps: MigrationStep[]
}

interface AppState {
  // Solution state
  solution: SolutionInfo | null
  isLoading: boolean
  error: string | null
  
  // Selected items
  selectedProject: ProjectInfo | null
  selectedStep: MigrationStep | null
  
  // Migration plan
  migrationPlan: MigrationPlan | null
  
  // Actions
  setSolution: (solution: SolutionInfo | null) => void
  setLoading: (loading: boolean) => void
  setError: (error: string | null) => void
  setSelectedProject: (project: ProjectInfo | null) => void
  setSelectedStep: (step: MigrationStep | null) => void
  setMigrationPlan: (plan: MigrationPlan | null) => void
  addMigrationStep: (step: MigrationStep) => void
  removeMigrationStep: (index: number) => void
  clearMigrationPlan: () => void
}

export const useAppStore = create<AppState>((set) => ({
  // Initial state
  solution: null,
  isLoading: false,
  error: null,
  selectedProject: null,
  selectedStep: null,
  migrationPlan: null,
  
  // Actions
  setSolution: (solution) => set({ solution }),
  setLoading: (isLoading) => set({ isLoading }),
  setError: (error) => set({ error }),
  setSelectedProject: (selectedProject) => set({ selectedProject }),
  setSelectedStep: (selectedStep) => set({ selectedStep }),
  setMigrationPlan: (migrationPlan) => set({ migrationPlan }),
  
  addMigrationStep: (step) =>
    set((state) => ({
      migrationPlan: state.migrationPlan
        ? {
            ...state.migrationPlan,
            steps: [...state.migrationPlan.steps, step],
          }
        : {
            name: 'New Migration Plan',
            description: '',
            steps: [step],
          },
    })),
  
  removeMigrationStep: (index) =>
    set((state) => ({
      migrationPlan: state.migrationPlan
        ? {
            ...state.migrationPlan,
            steps: state.migrationPlan.steps.filter((_, i) => i !== index),
          }
        : null,
    })),
  
  clearMigrationPlan: () => set({ migrationPlan: null }),
}))
