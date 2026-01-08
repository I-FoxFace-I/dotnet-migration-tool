// This file will be replaced with actual gRPC client once you run:
// npm install
// npm run proto:generate

// For now, we'll use mock data to demonstrate the UI

import { SolutionInfo, ProjectInfo, MigrationPlan } from '../store/useAppStore'

class MigrationServiceClient {
  private baseUrl = 'http://localhost:5000'
  
  async analyzeSolution(path: string): Promise<SolutionInfo> {
    // TODO: Replace with actual gRPC call
    // const client = new MigrationServiceClient(this.baseUrl)
    // const request = new AnalyzeSolutionRequest()
    // request.setSolutionPath(path)
    // const response = await client.analyzeSolution(request, {})
    // return response.toObject()
    
    // Mock data for now
    return new Promise((resolve) => {
      setTimeout(() => {
        resolve({
          name: 'Sample Solution',
          path: path,
          projectCount: 12,
          testProjectCount: 4,
          sourceProjectCount: 8,
          totalFiles: 156,
          totalClasses: 234,
          totalTests: 89,
          projects: [
            {
              name: 'Core.Library',
              path: '/path/to/Core.Library.csproj',
              targetFramework: 'net9.0',
              projectType: 'ClassLibrary',
              rootNamespace: 'MyApp.Core',
              isTestProject: false,
              fileCount: 25,
              classCount: 45,
              testCount: 0,
            },
            {
              name: 'Core.Tests',
              path: '/path/to/Core.Tests.csproj',
              targetFramework: 'net9.0',
              projectType: 'Test',
              rootNamespace: 'MyApp.Core.Tests',
              isTestProject: true,
              fileCount: 15,
              classCount: 20,
              testCount: 45,
            },
            {
              name: 'UI.Wpf',
              path: '/path/to/UI.Wpf.csproj',
              targetFramework: 'net9.0-windows',
              projectType: 'Wpf',
              rootNamespace: 'MyApp.UI',
              isTestProject: false,
              fileCount: 35,
              classCount: 50,
              testCount: 0,
            },
            {
              name: 'Services.Api',
              path: '/path/to/Services.Api.csproj',
              targetFramework: 'net9.0',
              projectType: 'WebApi',
              rootNamespace: 'MyApp.Services',
              isTestProject: false,
              fileCount: 40,
              classCount: 60,
              testCount: 0,
            },
            {
              name: 'Services.Tests',
              path: '/path/to/Services.Tests.csproj',
              targetFramework: 'net9.0',
              projectType: 'Test',
              rootNamespace: 'MyApp.Services.Tests',
              isTestProject: true,
              fileCount: 20,
              classCount: 30,
              testCount: 44,
            },
          ],
        })
      }, 500)
    })
  }
  
  async validatePlan(plan: MigrationPlan): Promise<{ isValid: boolean; errors: string[]; warnings: string[] }> {
    // Mock validation
    return new Promise((resolve) => {
      setTimeout(() => {
        const errors: string[] = []
        const warnings: string[] = []
        
        if (plan.steps.length === 0) {
          errors.push('Migration plan has no steps')
        }
        
        resolve({
          isValid: errors.length === 0,
          errors,
          warnings,
        })
      }, 300)
    })
  }
  
  async executeMigration(
    plan: MigrationPlan,
    onProgress: (progress: { currentStep: number; totalSteps: number; percentComplete: number; currentAction: string }) => void
  ): Promise<void> {
    // Mock execution
    const totalSteps = plan.steps.length
    
    for (let i = 0; i < totalSteps; i++) {
      await new Promise(resolve => setTimeout(resolve, 500))
      
      onProgress({
        currentStep: i + 1,
        totalSteps,
        percentComplete: ((i + 1) / totalSteps) * 100,
        currentAction: `Executing ${plan.steps[i].action}: ${plan.steps[i].source}`,
      })
    }
  }
}

export const migrationClient = new MigrationServiceClient()
