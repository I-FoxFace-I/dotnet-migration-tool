"""
Fake Solution Generator for Python tests.
Mirrors the .NET FakeSolutionGenerator to create consistent test structures.
"""

import os
import uuid
from pathlib import Path
from dataclasses import dataclass, field
from typing import List, Optional


@dataclass
class FakeProject:
    """Represents a generated fake project."""
    name: str
    path: Path
    is_test_project: bool = False
    guid: str = field(default_factory=lambda: str(uuid.uuid4()).upper())
    source_files: List[Path] = field(default_factory=list)
    folders: List[str] = field(default_factory=list)
    references: List[str] = field(default_factory=list)


@dataclass
class FakeSolution:
    """Represents a generated fake solution."""
    name: str
    path: Path
    directory: Path
    projects: List[FakeProject] = field(default_factory=list)
    folders: List[str] = field(default_factory=list)


class FakeSolutionGenerator:
    """
    Generates fake .NET solutions with projects and source files for testing.
    Mirrors the .NET FakeSolutionGenerator structure.
    """
    
    def __init__(self, temp_dir: Path):
        self.temp_dir = temp_dir
    
    def create_ef_core_solution(self, solution_name: str = "EfCoreApp") -> FakeSolution:
        """
        Creates a simple EF Core solution with entities and repositories.
        Ideal for testing migrations without unnecessary complexity.
        """
        projects = []
        
        # === Domain Project (Entities only) ===
        domain_project = self._create_domain_project(solution_name, "src")
        projects.append(domain_project)
        
        # === Infrastructure Project (EF Core DbContext, Repositories) ===
        infra_project = self._create_infrastructure_project(solution_name, "src", domain_project.name)
        projects.append(infra_project)
        
        # === Test Project ===
        tests_project = self._create_test_project(solution_name, "test", domain_project.name, infra_project.name)
        projects.append(tests_project)
        
        # Create solution file
        sln_path = self._create_solution_file(solution_name, projects)
        
        return FakeSolution(
            name=solution_name,
            path=sln_path,
            directory=self.temp_dir,
            projects=projects,
            folders=["src", "test"]
        )
    
    def create_minimal_solution(self, solution_name: str = "TestSolution") -> FakeSolution:
        """Creates a minimal solution with one project."""
        project_name = f"{solution_name}.Core"
        
        # Create project directory
        project_dir = self.temp_dir / project_name
        project_dir.mkdir(parents=True, exist_ok=True)
        
        # Create .csproj
        csproj_path = self._create_file(
            f"{project_name}/{project_name}.csproj",
            self._generate_simple_csproj(project_name)
        )
        
        # Create a simple C# file
        cs_path = self._create_file(
            f"{project_name}/SampleClass.cs",
            self._generate_simple_class(project_name, "SampleClass")
        )
        
        project = FakeProject(
            name=project_name,
            path=csproj_path,
            is_test_project=False,
            source_files=[cs_path]
        )
        
        # Create .sln file
        sln_path = self._create_solution_file(solution_name, [project])
        
        return FakeSolution(
            name=solution_name,
            path=sln_path,
            directory=self.temp_dir,
            projects=[project]
        )
    
    def create_solution_with_tests(self, solution_name: str = "TestSolution") -> FakeSolution:
        """Creates a solution with source and test projects."""
        src_name = f"{solution_name}.Core"
        test_name = f"{solution_name}.Core.Tests"
        
        # Create source project
        src_dir = self.temp_dir / src_name
        src_dir.mkdir(parents=True, exist_ok=True)
        
        src_csproj = self._create_file(
            f"{src_name}/{src_name}.csproj",
            self._generate_simple_csproj(src_name)
        )
        src_cs = self._create_file(
            f"{src_name}/Calculator.cs",
            self._generate_simple_class(src_name, "Calculator")
        )
        
        src_project = FakeProject(
            name=src_name,
            path=src_csproj,
            is_test_project=False,
            source_files=[src_cs]
        )
        
        # Create test project
        test_dir = self.temp_dir / test_name
        test_dir.mkdir(parents=True, exist_ok=True)
        
        test_csproj = self._create_file(
            f"{test_name}/{test_name}.csproj",
            self._generate_test_csproj(test_name, src_name)
        )
        test_cs = self._create_file(
            f"{test_name}/CalculatorTests.cs",
            self._generate_test_class(test_name, "CalculatorTests")
        )
        
        test_project = FakeProject(
            name=test_name,
            path=test_csproj,
            is_test_project=True,
            source_files=[test_cs],
            references=[src_name]
        )
        
        # Create .sln file
        sln_path = self._create_solution_file(solution_name, [src_project, test_project])
        
        return FakeSolution(
            name=solution_name,
            path=sln_path,
            directory=self.temp_dir,
            projects=[src_project, test_project]
        )
    
    # === Private Helper Methods ===
    
    def _create_domain_project(self, solution_name: str, base_dir: str) -> FakeProject:
        """Create Domain project with Entities and Interfaces."""
        project_name = f"{solution_name}.Domain"
        project_path = f"{base_dir}/{project_name}"
        files = []
        
        # Create directories
        (self.temp_dir / project_path).mkdir(parents=True, exist_ok=True)
        (self.temp_dir / project_path / "Entities").mkdir(exist_ok=True)
        (self.temp_dir / project_path / "Interfaces").mkdir(exist_ok=True)
        
        # Create .csproj
        csproj_path = self._create_file(
            f"{project_path}/{project_name}.csproj",
            self._generate_simple_csproj(project_name)
        )
        
        # Create Entities
        files.append(self._create_file(
            f"{project_path}/Entities/BaseEntity.cs",
            self._generate_base_entity(project_name)
        ))
        files.append(self._create_file(
            f"{project_path}/Entities/Customer.cs",
            self._generate_entity(project_name, "Customer", [("Name", "string"), ("Email", "string"), ("IsActive", "bool")])
        ))
        files.append(self._create_file(
            f"{project_path}/Entities/Product.cs",
            self._generate_entity(project_name, "Product", [("Name", "string"), ("Price", "decimal"), ("Stock", "int")])
        ))
        files.append(self._create_file(
            f"{project_path}/Entities/Order.cs",
            self._generate_entity(project_name, "Order", [("CustomerId", "int"), ("Total", "decimal"), ("CreatedAt", "DateTime")])
        ))
        files.append(self._create_file(
            f"{project_path}/Entities/OrderItem.cs",
            self._generate_entity(project_name, "OrderItem", [("OrderId", "int"), ("ProductId", "int"), ("Quantity", "int")])
        ))
        files.append(self._create_file(
            f"{project_path}/Entities/Category.cs",
            self._generate_entity(project_name, "Category", [("Name", "string"), ("Description", "string?")])
        ))
        
        # Create Interfaces
        files.append(self._create_file(
            f"{project_path}/Interfaces/IRepository.cs",
            self._generate_repository_interface(project_name)
        ))
        
        return FakeProject(
            name=project_name,
            path=csproj_path,
            is_test_project=False,
            source_files=files,
            folders=["Entities", "Interfaces"]
        )
    
    def _create_infrastructure_project(self, solution_name: str, base_dir: str, domain_project: str) -> FakeProject:
        """Create Infrastructure project with DbContext and Repositories."""
        project_name = f"{solution_name}.Infrastructure"
        project_path = f"{base_dir}/{project_name}"
        files = []
        
        # Create directories
        (self.temp_dir / project_path).mkdir(parents=True, exist_ok=True)
        (self.temp_dir / project_path / "Data").mkdir(exist_ok=True)
        (self.temp_dir / project_path / "Repositories").mkdir(exist_ok=True)
        
        # Create .csproj with EF Core packages
        csproj_path = self._create_file(
            f"{project_path}/{project_name}.csproj",
            self._generate_infra_csproj(project_name, domain_project)
        )
        
        # Create Data
        files.append(self._create_file(
            f"{project_path}/Data/AppDbContext.cs",
            self._generate_db_context(project_name, domain_project)
        ))
        
        # Create Repositories
        files.append(self._create_file(
            f"{project_path}/Repositories/BaseRepository.cs",
            self._generate_base_repository(project_name, domain_project)
        ))
        files.append(self._create_file(
            f"{project_path}/Repositories/CustomerRepository.cs",
            self._generate_repository(project_name, domain_project, "Customer")
        ))
        files.append(self._create_file(
            f"{project_path}/Repositories/ProductRepository.cs",
            self._generate_repository(project_name, domain_project, "Product")
        ))
        files.append(self._create_file(
            f"{project_path}/Repositories/OrderRepository.cs",
            self._generate_repository(project_name, domain_project, "Order")
        ))
        
        return FakeProject(
            name=project_name,
            path=csproj_path,
            is_test_project=False,
            source_files=files,
            folders=["Data", "Repositories"],
            references=[domain_project]
        )
    
    def _create_test_project(self, solution_name: str, base_dir: str, 
                             domain_project: str, infra_project: str) -> FakeProject:
        """Create Test project."""
        project_name = f"{solution_name}.Tests"
        project_path = f"{base_dir}/{project_name}"
        files = []
        
        # Create directory
        (self.temp_dir / project_path).mkdir(parents=True, exist_ok=True)
        
        # Create .csproj
        csproj_path = self._create_file(
            f"{project_path}/{project_name}.csproj",
            self._generate_tests_csproj(project_name, domain_project, infra_project)
        )
        
        # Create tests
        files.append(self._create_file(
            f"{project_path}/CustomerRepositoryTests.cs",
            self._generate_repository_test(project_name, "Customer")
        ))
        files.append(self._create_file(
            f"{project_path}/ProductRepositoryTests.cs",
            self._generate_repository_test(project_name, "Product")
        ))
        
        return FakeProject(
            name=project_name,
            path=csproj_path,
            is_test_project=True,
            source_files=files,
            references=[domain_project, infra_project]
        )
    
    def _create_solution_file(self, name: str, projects: List[FakeProject]) -> Path:
        """Create a .sln file."""
        project_entries = []
        config_entries = []
        
        for proj in projects:
            guid = f"{{{proj.guid}}}"
            # Calculate relative path from solution to project
            rel_path = proj.path.relative_to(self.temp_dir)
            sln_path = str(rel_path).replace('/', '\\')
            
            project_entries.append(
                f'Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "{proj.name}", "{sln_path}", "{guid}"\n'
                f'EndProject'
            )
            
            config_entries.extend([
                f'\t\t{guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU',
                f'\t\t{guid}.Debug|Any CPU.Build.0 = Debug|Any CPU',
                f'\t\t{guid}.Release|Any CPU.ActiveCfg = Release|Any CPU',
                f'\t\t{guid}.Release|Any CPU.Build.0 = Release|Any CPU',
            ])
        
        content = f"""Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
{chr(10).join(project_entries)}
Global
\tGlobalSection(SolutionConfigurationPlatforms) = preSolution
\t\tDebug|Any CPU = Debug|Any CPU
\t\tRelease|Any CPU = Release|Any CPU
\tEndGlobalSection
\tGlobalSection(ProjectConfigurationPlatforms) = postSolution
{chr(10).join(config_entries)}
\tEndGlobalSection
\tGlobalSection(SolutionProperties) = preSolution
\t\tHideSolutionNode = FALSE
\tEndGlobalSection
EndGlobal
"""
        return self._create_file(f"{name}.sln", content)
    
    def _create_file(self, relative_path: str, content: str) -> Path:
        """Create a file with given content."""
        full_path = self.temp_dir / relative_path
        full_path.parent.mkdir(parents=True, exist_ok=True)
        full_path.write_text(content, encoding='utf-8')
        return full_path
    
    # === Content Generators ===
    
    def _generate_simple_csproj(self, project_name: str) -> str:
        return f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{project_name}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>'''
    
    def _generate_infra_csproj(self, project_name: str, domain_project: str) -> str:
        return f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{project_name}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\{domain_project}\\{domain_project}.csproj" />
  </ItemGroup>
</Project>'''
    
    def _generate_test_csproj(self, project_name: str, src_project: str) -> str:
        return f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{project_name}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\{src_project}\\{src_project}.csproj" />
  </ItemGroup>
</Project>'''
    
    def _generate_tests_csproj(self, project_name: str, domain_project: str, infra_project: str) -> str:
        return f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{project_name}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\\..\\src\\{domain_project}\\{domain_project}.csproj" />
    <ProjectReference Include="..\\..\\src\\{infra_project}\\{infra_project}.csproj" />
  </ItemGroup>
</Project>'''
    
    def _generate_simple_class(self, namespace: str, class_name: str) -> str:
        return f'''namespace {namespace};

/// <summary>
/// A simple class for testing.
/// </summary>
public class {class_name}
{{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
}}'''
    
    def _generate_test_class(self, namespace: str, class_name: str) -> str:
        return f'''using Xunit;

namespace {namespace};

/// <summary>
/// Test class for testing.
/// </summary>
public class {class_name}
{{
    [Fact]
    public void Add_ReturnsSum()
    {{
        Assert.Equal(5, 2 + 3);
    }}

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(0, 0, 0)]
    public void Add_WithTheoryData_ReturnsSum(int a, int b, int expected)
    {{
        Assert.Equal(expected, a + b);
    }}
}}'''
    
    def _generate_base_entity(self, namespace: str) -> str:
        return f'''namespace {namespace}.Entities;

public abstract class BaseEntity
{{
    public int Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
}}'''
    
    def _generate_entity(self, namespace: str, entity_name: str, properties: list) -> str:
        props = '\n    '.join(
            f'public {prop_type} {prop_name} {{ get; set; }}' + (' = string.Empty;' if prop_type == 'string' else '')
            for prop_name, prop_type in properties
        )
        return f'''namespace {namespace}.Entities;

public class {entity_name} : BaseEntity
{{
    {props}
}}'''
    
    def _generate_repository_interface(self, namespace: str) -> str:
        return f'''namespace {namespace}.Interfaces;

public interface IRepository<T> where T : class
{{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}}'''
    
    def _generate_db_context(self, infra_ns: str, domain_ns: str) -> str:
        return f'''using Microsoft.EntityFrameworkCore;
using {domain_ns}.Entities;

namespace {infra_ns}.Data;

public class AppDbContext : DbContext
{{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {{ }}
    
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Category> Categories => Set<Category>();
}}'''
    
    def _generate_base_repository(self, infra_ns: str, domain_ns: str) -> str:
        return f'''using Microsoft.EntityFrameworkCore;
using {domain_ns}.Entities;
using {domain_ns}.Interfaces;
using {infra_ns}.Data;

namespace {infra_ns}.Repositories;

public class BaseRepository<T> : IRepository<T> where T : BaseEntity
{{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public BaseRepository(AppDbContext context)
    {{
        _context = context;
        _dbSet = context.Set<T>();
    }}
    
    public virtual async Task<T?> GetByIdAsync(int id) 
        => await _dbSet.FindAsync(id);
    
    public virtual async Task<IEnumerable<T>> GetAllAsync() 
        => await _dbSet.ToListAsync();
    
    public virtual async Task<T> AddAsync(T entity)
    {{
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }}
    
    public virtual async Task UpdateAsync(T entity)
    {{
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }}
    
    public virtual async Task DeleteAsync(int id)
    {{
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {{
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }}
    }}
}}'''
    
    def _generate_repository(self, infra_ns: str, domain_ns: str, entity_name: str) -> str:
        return f'''using {domain_ns}.Entities;
using {infra_ns}.Data;

namespace {infra_ns}.Repositories;

public class {entity_name}Repository : BaseRepository<{entity_name}>
{{
    public {entity_name}Repository(AppDbContext context) : base(context) {{ }}
}}'''
    
    def _generate_repository_test(self, test_ns: str, entity_name: str) -> str:
        return f'''using Xunit;
using FluentAssertions;

namespace {test_ns};

public class {entity_name}RepositoryTests
{{
    [Fact]
    public void Repository_CanBeCreated()
    {{
        // This is a placeholder test
        true.Should().BeTrue();
    }}
}}'''
