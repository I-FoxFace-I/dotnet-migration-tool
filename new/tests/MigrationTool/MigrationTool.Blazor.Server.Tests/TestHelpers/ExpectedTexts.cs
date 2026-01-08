namespace MigrationTool.Blazor.Server.Tests.TestHelpers;

/// <summary>
/// Contains expected text values for tests.
/// These should match the actual localized strings from Translations.
/// </summary>
public static class ExpectedTexts
{
    // Dashboard
    public const string DashboardTitle = "Dashboard";
    public const string DashboardDescription = "Overview of your solution and migration progress.";
    public const string NoProjectsLoaded = "No projects loaded. Please select a solution in settings.";
    
    // Stats
    public const string TotalProjects = "Total Projects";
    public const string TestProjects = "Test Projects";
    public const string SourceFiles = "Source Files";
    public const string TotalClasses = "Total Classes";
    public const string TotalTests = "Total Tests";
    
    // Explorer
    public const string ProjectExplorerTitle = "Project Explorer";
    public const string ProjectExplorerDescription = "Browse projects, files, and code structure.";
    public const string Project = "Project";
    public const string FilePath = "File Path";
    public const string FileName = "File Name";
    public const string ClassName = "Class Name";
    public const string Tests = "Tests";
    public const string FilesTree = "Files (Tree)";
    public const string NoFilesFound = "No files found in this project.";
    public const string Select = "Select";
    
    // Planner
    public const string MigrationPlannerTitle = "Migration Planner";
    public const string MigrationPlannerDescription = "Plan and execute your project migrations.";
    public const string CreatePlan = "Create Plan";
    public const string LoadPlan = "Load Plan";
    public const string SavePlan = "Save Plan";
    public const string ExecutePlan = "Execute Plan";
    public const string PlanDetails = "Plan Details";
    public const string Actions = "Actions";
    public const string Add = "Add";
    public const string Type = "Type";
    public const string Source = "Source";
    public const string Target = "Target";
    public const string Delete = "Delete";
    public const string NoData = "No data available.";
    public const string NotImplemented = "This feature is not yet implemented.";
    
    // Settings
    public const string SettingsTitle = "Settings";
    public const string SettingsDescription = "Configure application settings.";
    public const string WorkspacePath = "Workspace Path";
    public const string SelectSolution = "Select Solution";
    public const string SelectWorkspace = "Please select a workspace path.";
    public const string SolutionsFound = "Solutions found:";
    public const string NoSolutionsFound = "No solutions found in the workspace.";
    public const string Language = "Language";
    public const string Browse = "Browse";
    public const string Apply = "Apply";
    public const string Success = "Success";
    public const string ChangesSaved = "Changes saved successfully!";
    
    // Common
    public const string Loading = "Loading...";
    public const string Settings = "Settings";
}
