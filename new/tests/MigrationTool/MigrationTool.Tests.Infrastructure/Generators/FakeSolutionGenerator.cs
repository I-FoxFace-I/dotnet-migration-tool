using MigrationTool.Tests.Infrastructure.Fixtures;

namespace MigrationTool.Tests.Infrastructure.Generators;

/// <summary>
/// Generates fake .NET solutions with projects and source files for testing.
/// </summary>
public static class FakeSolutionGenerator
{
    /// <summary>
    /// Creates a simple EF Core solution with entities and repositories.
    /// Ideal for testing migrations without unnecessary complexity.
    /// </summary>
    public static FakeSolution CreateEfCoreSolution(TempDirectoryFixture tempDir, string solutionName = "EfCoreApp")
    {
        var projects = new List<FakeProject>();
        var allGuids = new List<(string Name, string Path, Guid Guid)>();
        var folderGuids = new List<(string Name, Guid Guid)>();
        
        var srcFolderGuid = Guid.NewGuid();
        var testFolderGuid = Guid.NewGuid();
        folderGuids.Add(("src", srcFolderGuid));
        folderGuids.Add(("test", testFolderGuid));

        // === Domain Project (Entities only) ===
        var domainProject = CreateSimpleDomainProject(tempDir, solutionName, "src");
        projects.Add(domainProject);
        allGuids.Add((domainProject.Name, $"src/{domainProject.Name}/{domainProject.Name}.csproj", domainProject.Guid));
        
        // === Infrastructure Project (EF Core DbContext, Repositories) ===
        var infraProject = CreateSimpleInfrastructureProject(tempDir, solutionName, "src", domainProject.Name);
        projects.Add(infraProject);
        allGuids.Add((infraProject.Name, $"src/{infraProject.Name}/{infraProject.Name}.csproj", infraProject.Guid));

        // === Test Project ===
        var testsProject = CreateSimpleTestProject(tempDir, solutionName, "test", domainProject.Name, infraProject.Name);
        projects.Add(testsProject);
        allGuids.Add((testsProject.Name, $"test/{testsProject.Name}/{testsProject.Name}.csproj", testsProject.Guid));

        // Create solution file
        var slnContent = GenerateSlnWithNestedFolders(solutionName, folderGuids, allGuids, 
            srcFolderGuid, testFolderGuid, projects);
        var slnPath = tempDir.CreateFile($"{solutionName}.sln", slnContent);

        return new FakeSolution
        {
            Name = solutionName,
            Path = slnPath,
            Directory = tempDir.Path,
            Projects = projects,
            Folders = ["src", "test"]
        };
    }
    
    #region Simple EF Core Project Generators
    
    private static FakeProject CreateSimpleDomainProject(TempDirectoryFixture tempDir, string solutionName, string baseDir)
    {
        var projectName = $"{solutionName}.Domain";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateSimpleCsproj(projectName));
        
        // Entities folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Entities");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/BaseEntity.cs", 
            GenerateSimpleBaseEntity(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Customer.cs", 
            GenerateSimpleEntity(projectName, "Customer", ("Name", "string"), ("Email", "string"), ("IsActive", "bool"))));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Product.cs", 
            GenerateSimpleEntity(projectName, "Product", ("Name", "string"), ("Price", "decimal"), ("Stock", "int"))));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Order.cs", 
            GenerateSimpleEntity(projectName, "Order", ("CustomerId", "int"), ("Total", "decimal"), ("CreatedAt", "DateTime"))));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/OrderItem.cs", 
            GenerateSimpleEntity(projectName, "OrderItem", ("OrderId", "int"), ("ProductId", "int"), ("Quantity", "int"), ("UnitPrice", "decimal"))));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Category.cs", 
            GenerateSimpleEntity(projectName, "Category", ("Name", "string"), ("Description", "string?"))));
        
        // Interfaces folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Interfaces");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IRepository.cs", 
            GenerateSimpleRepositoryInterface(projectName)));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Entities", "Interfaces"]
        };
    }
    
    private static FakeProject CreateSimpleInfrastructureProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, string domainProject)
    {
        var projectName = $"{solutionName}.Infrastructure";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj with EF Core packages
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateSimpleInfraCsproj(projectName, domainProject));
        
        // Data folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Data");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/AppDbContext.cs", 
            GenerateSimpleDbContext(projectName, domainProject)));
        
        // Repositories folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Repositories");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/BaseRepository.cs", 
            GenerateSimpleBaseRepository(projectName, domainProject)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/CustomerRepository.cs", 
            GenerateSimpleRepository(projectName, domainProject, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/ProductRepository.cs", 
            GenerateSimpleRepository(projectName, domainProject, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/OrderRepository.cs", 
            GenerateSimpleRepository(projectName, domainProject, "Order")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Data", "Repositories"],
            References = [domainProject]
        };
    }
    
    private static FakeProject CreateSimpleTestProject(TempDirectoryFixture tempDir, string solutionName, string baseDir,
        string domainProject, string infraProject)
    {
        var projectName = $"{solutionName}.Tests";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateSimpleTestCsproj(projectName, domainProject, infraProject));
        
        // Tests
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/CustomerRepositoryTests.cs", 
            GenerateSimpleRepositoryTest(projectName, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/ProductRepositoryTests.cs", 
            GenerateSimpleRepositoryTest(projectName, "Product")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = true,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = [],
            References = [domainProject, infraProject]
        };
    }
    
    #endregion
    
    #region Simple EF Core Content Generators
    
    private static string GenerateSimpleCsproj(string projectName) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
    
    private static string GenerateSimpleInfraCsproj(string projectName, string domainProject) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Sqlite"" Version=""9.0.0"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{domainProject}\{domainProject}.csproj"" />
  </ItemGroup>
</Project>";

    private static string GenerateSimpleTestCsproj(string projectName, string domainProject, string infraProject) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.InMemory"" Version=""9.0.0"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\src\{domainProject}\{domainProject}.csproj"" />
    <ProjectReference Include=""..\..\src\{infraProject}\{infraProject}.csproj"" />
  </ItemGroup>
</Project>";

    private static string GenerateSimpleBaseEntity(string ns) => $@"namespace {ns}.Entities;

public abstract class BaseEntity
{{
    public int Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
}}";

    private static string GenerateSimpleEntity(string ns, string entityName, params (string Name, string Type)[] properties)
    {
        var props = string.Join(Environment.NewLine + "    ", 
            properties.Select(p => $"public {p.Type} {p.Name} {{ get; set; }}" + 
                (p.Type == "string" ? " = string.Empty;" : "")));
        
        return $@"namespace {ns}.Entities;

public class {entityName} : BaseEntity
{{
    {props}
}}";
    }

    private static string GenerateSimpleRepositoryInterface(string ns) => $@"namespace {ns}.Interfaces;

public interface IRepository<T> where T : class
{{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}}";

    private static string GenerateSimpleDbContext(string infraNs, string domainNs) => $@"using Microsoft.EntityFrameworkCore;
using {domainNs}.Entities;

namespace {infraNs}.Data;

public class AppDbContext : DbContext
{{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {{ }}
    
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Category> Categories => Set<Category>();
}}";

    private static string GenerateSimpleBaseRepository(string infraNs, string domainNs) => $@"using Microsoft.EntityFrameworkCore;
using {domainNs}.Entities;
using {domainNs}.Interfaces;
using {infraNs}.Data;

namespace {infraNs}.Repositories;

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
}}";

    private static string GenerateSimpleRepository(string infraNs, string domainNs, string entityName) => $@"using {domainNs}.Entities;
using {infraNs}.Data;

namespace {infraNs}.Repositories;

public class {entityName}Repository : BaseRepository<{entityName}>
{{
    public {entityName}Repository(AppDbContext context) : base(context) {{ }}
}}";

    private static string GenerateSimpleRepositoryTest(string testNs, string entityName) => $@"using Xunit;
using FluentAssertions;

namespace {testNs};

public class {entityName}RepositoryTests
{{
    [Fact]
    public void Repository_CanBeCreated()
    {{
        // This is a placeholder test
        true.Should().BeTrue();
    }}
}}";

    #endregion
    
    #region EF Core Project Generators
    
    private static FakeProject CreateDomainProject(TempDirectoryFixture tempDir, string solutionName, string baseDir)
    {
        var projectName = $"{solutionName}.Domain";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateDomainCsproj(projectName));
        
        // Entities folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Entities");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/BaseEntity.cs", GenerateBaseEntity(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Customer.cs", GenerateCustomerEntity(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Product.cs", GenerateProductEntity(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Order.cs", GenerateOrderEntity(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/OrderItem.cs", GenerateOrderItemEntity(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Entities/Category.cs", GenerateCategoryEntity(projectName)));
        
        // Value Objects folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/ValueObjects");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/ValueObjects/Money.cs", GenerateMoneyValueObject(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/ValueObjects/Address.cs", GenerateAddressValueObject(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/ValueObjects/Email.cs", GenerateEmailValueObject(projectName)));
        
        // Enums folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Enums");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Enums/OrderStatus.cs", GenerateOrderStatusEnum(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Enums/PaymentMethod.cs", GeneratePaymentMethodEnum(projectName)));
        
        // Events folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Events");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Events/IDomainEvent.cs", GenerateDomainEventInterface(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Events/OrderCreatedEvent.cs", GenerateOrderCreatedEvent(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Events/OrderStatusChangedEvent.cs", GenerateOrderStatusChangedEvent(projectName)));
        
        // Specifications folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Specifications");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Specifications/ISpecification.cs", GenerateSpecificationInterface(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Specifications/BaseSpecification.cs", GenerateBaseSpecification(projectName)));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Entities", "ValueObjects", "Enums", "Events", "Specifications"]
        };
    }
    
    private static FakeProject CreateEfApplicationProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, string domainProject)
    {
        var projectName = $"{solutionName}.Application";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateApplicationCsproj(projectName, domainProject));
        
        // Interfaces folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Interfaces");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IRepository.cs", GenerateGenericRepositoryInterface(projectName, domainProject)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IUnitOfWork.cs", GenerateUnitOfWorkInterface(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/ICustomerRepository.cs", GenerateEntityRepositoryInterface(projectName, domainProject, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IProductRepository.cs", GenerateEntityRepositoryInterface(projectName, domainProject, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IOrderRepository.cs", GenerateEntityRepositoryInterface(projectName, domainProject, "Order")));
        
        // DTOs folder  
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/DTOs");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/DTOs/CustomerDto.cs", GenerateCustomerDtos(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/DTOs/ProductDto.cs", GenerateProductDtos(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/DTOs/OrderDto.cs", GenerateOrderDtos(projectName)));
        
        // UseCases folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/UseCases");
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/UseCases/Customers");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/UseCases/Customers/CreateCustomer.cs", GenerateCreateCustomerUseCase(projectName, domainProject)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/UseCases/Customers/GetCustomerById.cs", GenerateGetByIdUseCase(projectName, domainProject, "Customer")));
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/UseCases/Orders");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/UseCases/Orders/CreateOrder.cs", GenerateCreateOrderUseCase(projectName, domainProject)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/UseCases/Orders/GetOrderById.cs", GenerateGetByIdUseCase(projectName, domainProject, "Order")));
        
        // Mappers folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Mappers");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Mappers/CustomerMapper.cs", GenerateEfMapper(projectName, domainProject, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Mappers/ProductMapper.cs", GenerateEfMapper(projectName, domainProject, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Mappers/OrderMapper.cs", GenerateEfMapper(projectName, domainProject, "Order")));
        
        // Validators folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Validators");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Validators/CustomerValidator.cs", GenerateValidator(projectName, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Validators/OrderValidator.cs", GenerateValidator(projectName, "Order")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Interfaces", "DTOs", "UseCases", "UseCases/Customers", "UseCases/Orders", "Mappers", "Validators"],
            References = [domainProject]
        };
    }
    
    private static FakeProject CreateEfInfrastructureProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, 
        string domainProject, string appProject)
    {
        var projectName = $"{solutionName}.Infrastructure";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj with EF Core packages
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateInfrastructureCsproj(projectName, domainProject, appProject));
        
        // Data folder (DbContext and configurations)
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Data");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/AppDbContext.cs", GenerateAppDbContext(projectName, domainProject)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/DesignTimeDbContextFactory.cs", GenerateDesignTimeFactory(projectName)));
        
        // Data/Configurations folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Data/Configurations");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/Configurations/CustomerConfiguration.cs", GenerateEntityConfiguration(projectName, domainProject, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/Configurations/ProductConfiguration.cs", GenerateEntityConfiguration(projectName, domainProject, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/Configurations/OrderConfiguration.cs", GenerateEntityConfiguration(projectName, domainProject, "Order")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/Configurations/OrderItemConfiguration.cs", GenerateEntityConfiguration(projectName, domainProject, "OrderItem")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/Configurations/CategoryConfiguration.cs", GenerateEntityConfiguration(projectName, domainProject, "Category")));
        
        // Repositories folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Repositories");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/BaseRepository.cs", GenerateEfBaseRepository(projectName, domainProject, appProject)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/CustomerRepository.cs", GenerateEfRepository(projectName, domainProject, appProject, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/ProductRepository.cs", GenerateEfRepository(projectName, domainProject, appProject, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/OrderRepository.cs", GenerateEfRepository(projectName, domainProject, appProject, "Order")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/UnitOfWork.cs", GenerateUnitOfWork(projectName, appProject)));
        
        // Migrations folder (empty but structured)
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Migrations");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Migrations/20240101000000_InitialCreate.cs", GenerateInitialMigration(projectName)));
        
        // Services folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Services");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/DateTimeService.cs", GenerateDateTimeService(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/EmailService.cs", GenerateEmailServiceImpl(projectName)));
        
        // Extensions folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Extensions");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Extensions/ServiceCollectionExtensions.cs", GenerateServiceCollectionExtensions(projectName, domainProject, appProject)));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Data", "Data/Configurations", "Repositories", "Migrations", "Services", "Extensions"],
            References = [domainProject, appProject]
        };
    }
    
    private static FakeProject CreateApiProject(TempDirectoryFixture tempDir, string solutionName, string baseDir,
        string domainProject, string appProject, string infraProject)
    {
        var projectName = $"{solutionName}.Api";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateApiCsproj(projectName, domainProject, appProject, infraProject));
        
        // Program.cs
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Program.cs", GenerateProgramCs(projectName, infraProject)));
        
        // Controllers folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Controllers");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Controllers/CustomersController.cs", GenerateApiController(projectName, appProject, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Controllers/ProductsController.cs", GenerateApiController(projectName, appProject, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Controllers/OrdersController.cs", GenerateApiController(projectName, appProject, "Order")));
        
        // Middleware folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Middleware");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Middleware/ExceptionHandlingMiddleware.cs", GenerateExceptionMiddleware(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Middleware/RequestLoggingMiddleware.cs", GenerateLoggingMiddleware(projectName)));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Controllers", "Middleware"],
            References = [domainProject, appProject, infraProject]
        };
    }
    
    private static FakeProject CreateEfTestProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, 
        string referencedProject, string projectType)
    {
        var projectName = $"{solutionName}.{projectType}.Tests";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateTestCsproj(projectName, "net9.0", referencedProject, $"../../src/{referencedProject}"));
        
        // Unit tests
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Unit");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Unit/{projectType}Tests.cs", GenerateTestClass($"{projectName}.Unit", $"{projectType}Tests")));
        
        // Mocks folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Mocks");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Mocks/MockRepository.cs", GenerateMockRepository(projectName)));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = true,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Unit", "Mocks"],
            References = [referencedProject]
        };
    }
    
    private static FakeProject CreateIntegrationTestProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, 
        string[] referencedProjects)
    {
        var projectName = $"{solutionName}.IntegrationTests";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateIntegrationTestCsproj(projectName, referencedProjects));
        
        // Tests folder
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/CustomerIntegrationTests.cs", GenerateIntegrationTest(projectName, "Customer")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/OrderIntegrationTests.cs", GenerateIntegrationTest(projectName, "Order")));
        
        // Fixtures folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Fixtures");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Fixtures/DatabaseFixture.cs", GenerateDatabaseFixture(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Fixtures/WebApplicationFixture.cs", GenerateWebAppFixture(projectName)));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = true,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Fixtures"],
            References = [.. referencedProjects]
        };
    }
    
    #endregion
    
    #region EF Core Content Generators
    
    private static string GenerateDomainCsproj(string projectName) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";

    private static string GenerateApplicationCsproj(string projectName, string domainProject) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{domainProject}\{domainProject}.csproj"" />
  </ItemGroup>
</Project>";

    private static string GenerateInfrastructureCsproj(string projectName, string domainProject, string appProject) => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Sqlite"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection.Abstractions"" Version=""9.0.0"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{domainProject}\{domainProject}.csproj"" />
    <ProjectReference Include=""..\{appProject}\{appProject}.csproj"" />
  </ItemGroup>
</Project>";

    private static string GenerateApiCsproj(string projectName, string domainProject, string appProject, string infraProject) => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{domainProject}\{domainProject}.csproj"" />
    <ProjectReference Include=""..\{appProject}\{appProject}.csproj"" />
    <ProjectReference Include=""..\{infraProject}\{infraProject}.csproj"" />
  </ItemGroup>
</Project>";

    private static string GenerateIntegrationTestCsproj(string projectName, string[] referencedProjects)
    {
        var refs = string.Join(Environment.NewLine + "    ", 
            referencedProjects.Select(r => $@"<ProjectReference Include=""..\..\src\{r}\{r}.csproj"" />"));
        
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc.Testing"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.InMemory"" Version=""9.0.0"" />
  </ItemGroup>
  <ItemGroup>
    {refs}
  </ItemGroup>
</Project>";
    }
    
    private static string GenerateBaseEntity(string ns) => $@"namespace {ns}.Entities;

/// <summary>
/// Base entity with common properties.
/// </summary>
public abstract class BaseEntity
{{
    public int Id {{ get; set; }}
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? UpdatedAt {{ get; set; }}
    public bool IsDeleted {{ get; set; }}
    
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    
    public void AddDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}}";

    private static string GenerateCustomerEntity(string ns) => $@"using {ns}.ValueObjects;

namespace {ns}.Entities;

/// <summary>
/// Customer entity representing a customer in the system.
/// </summary>
public class Customer : BaseEntity
{{
    public string FirstName {{ get; set; }} = string.Empty;
    public string LastName {{ get; set; }} = string.Empty;
    public Email Email {{ get; set; }} = null!;
    public Address? ShippingAddress {{ get; set; }}
    public Address? BillingAddress {{ get; set; }}
    public string? Phone {{ get; set; }}
    public bool IsActive {{ get; set; }} = true;
    
    public virtual ICollection<Order> Orders {{ get; set; }} = new List<Order>();
    
    public string FullName => $""{{FirstName}} {{LastName}}"";
}}";

    private static string GenerateProductEntity(string ns) => $@"using {ns}.ValueObjects;

namespace {ns}.Entities;

/// <summary>
/// Product entity representing a product in the catalog.
/// </summary>
public class Product : BaseEntity
{{
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public string Sku {{ get; set; }} = string.Empty;
    public Money Price {{ get; set; }} = null!;
    public int StockQuantity {{ get; set; }}
    public int? CategoryId {{ get; set; }}
    public bool IsAvailable {{ get; set; }} = true;
    
    public virtual Category? Category {{ get; set; }}
    public virtual ICollection<OrderItem> OrderItems {{ get; set; }} = new List<OrderItem>();
}}";

    private static string GenerateOrderEntity(string ns) => $@"using {ns}.Enums;
using {ns}.ValueObjects;

namespace {ns}.Entities;

/// <summary>
/// Order entity representing a customer order.
/// </summary>
public class Order : BaseEntity
{{
    public string OrderNumber {{ get; set; }} = string.Empty;
    public int CustomerId {{ get; set; }}
    public OrderStatus Status {{ get; set; }} = OrderStatus.Pending;
    public Money SubTotal {{ get; set; }} = null!;
    public Money Tax {{ get; set; }} = null!;
    public Money Total {{ get; set; }} = null!;
    public Address ShippingAddress {{ get; set; }} = null!;
    public PaymentMethod PaymentMethod {{ get; set; }}
    public DateTime? ShippedAt {{ get; set; }}
    public DateTime? DeliveredAt {{ get; set; }}
    
    public virtual Customer Customer {{ get; set; }} = null!;
    public virtual ICollection<OrderItem> Items {{ get; set; }} = new List<OrderItem>();
    
    public void AddItem(Product product, int quantity)
    {{
        var item = new OrderItem {{ ProductId = product.Id, Quantity = quantity, UnitPrice = product.Price }};
        Items.Add(item);
        RecalculateTotal();
    }}
    
    public void RecalculateTotal()
    {{
        // Implementation
    }}
}}";

    private static string GenerateOrderItemEntity(string ns) => $@"using {ns}.ValueObjects;

namespace {ns}.Entities;

/// <summary>
/// Order item entity representing a line item in an order.
/// </summary>
public class OrderItem : BaseEntity
{{
    public int OrderId {{ get; set; }}
    public int ProductId {{ get; set; }}
    public int Quantity {{ get; set; }}
    public Money UnitPrice {{ get; set; }} = null!;
    
    public virtual Order Order {{ get; set; }} = null!;
    public virtual Product Product {{ get; set; }} = null!;
    
    public Money LineTotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}}";

    private static string GenerateCategoryEntity(string ns) => $@"namespace {ns}.Entities;

/// <summary>
/// Category entity for product categorization.
/// </summary>
public class Category : BaseEntity
{{
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public int? ParentCategoryId {{ get; set; }}
    public string? ImageUrl {{ get; set; }}
    public int DisplayOrder {{ get; set; }}
    
    public virtual Category? ParentCategory {{ get; set; }}
    public virtual ICollection<Category> SubCategories {{ get; set; }} = new List<Category>();
    public virtual ICollection<Product> Products {{ get; set; }} = new List<Product>();
}}";

    private static string GenerateMoneyValueObject(string ns) => $@"namespace {ns}.ValueObjects;

/// <summary>
/// Value object representing monetary values.
/// </summary>
public record Money
{{
    public decimal Amount {{ get; init; }}
    public string Currency {{ get; init; }} = ""USD"";
    
    public Money(decimal amount, string currency = ""USD"")
    {{
        if (amount < 0) throw new ArgumentException(""Amount cannot be negative"");
        Amount = amount;
        Currency = currency;
    }}
    
    public static Money Zero => new(0);
    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount, a.Currency);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount, a.Currency);
    public static Money operator *(Money a, decimal multiplier) => new(a.Amount * multiplier, a.Currency);
}}";

    private static string GenerateAddressValueObject(string ns) => $@"namespace {ns}.ValueObjects;

/// <summary>
/// Value object representing a postal address.
/// </summary>
public record Address
{{
    public string Street {{ get; init; }} = string.Empty;
    public string City {{ get; init; }} = string.Empty;
    public string State {{ get; init; }} = string.Empty;
    public string PostalCode {{ get; init; }} = string.Empty;
    public string Country {{ get; init; }} = string.Empty;
    
    public string FullAddress => $""{{Street}}, {{City}}, {{State}} {{PostalCode}}, {{Country}}"";
}}";

    private static string GenerateEmailValueObject(string ns) => $@"using System.Text.RegularExpressions;

namespace {ns}.ValueObjects;

/// <summary>
/// Value object representing an email address.
/// </summary>
public partial record Email
{{
    public string Value {{ get; }}
    
    public Email(string value)
    {{
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(""Email cannot be empty"");
        if (!EmailRegex().IsMatch(value))
            throw new ArgumentException(""Invalid email format"");
        Value = value.ToLowerInvariant();
    }}
    
    [GeneratedRegex(@""^[^@\s]+@[^@\s]+\.[^@\s]+$"")]
    private static partial Regex EmailRegex();
    
    public static implicit operator string(Email email) => email.Value;
}}";

    private static string GenerateOrderStatusEnum(string ns) => $@"namespace {ns}.Enums;

/// <summary>
/// Order status enumeration.
/// </summary>
public enum OrderStatus
{{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Refunded = 6
}}";

    private static string GeneratePaymentMethodEnum(string ns) => $@"namespace {ns}.Enums;

/// <summary>
/// Payment method enumeration.
/// </summary>
public enum PaymentMethod
{{
    CreditCard = 0,
    DebitCard = 1,
    BankTransfer = 2,
    PayPal = 3,
    Cash = 4
}}";

    private static string GenerateDomainEventInterface(string ns) => $@"namespace {ns}.Events;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{{
    DateTime OccurredOn {{ get; }}
}}";

    private static string GenerateOrderCreatedEvent(string ns) => $@"namespace {ns}.Events;

/// <summary>
/// Event raised when a new order is created.
/// </summary>
public record OrderCreatedEvent(int OrderId, int CustomerId, decimal Total) : IDomainEvent
{{
    public DateTime OccurredOn {{ get; }} = DateTime.UtcNow;
}}";

    private static string GenerateOrderStatusChangedEvent(string ns) => $@"using {ns}.Enums;

namespace {ns}.Events;

/// <summary>
/// Event raised when order status changes.
/// </summary>
public record OrderStatusChangedEvent(int OrderId, OrderStatus OldStatus, OrderStatus NewStatus) : IDomainEvent
{{
    public DateTime OccurredOn {{ get; }} = DateTime.UtcNow;
}}";

    private static string GenerateSpecificationInterface(string ns) => $@"using System.Linq.Expressions;

namespace {ns}.Specifications;

/// <summary>
/// Specification pattern interface.
/// </summary>
public interface ISpecification<T>
{{
    Expression<Func<T, bool>> Criteria {{ get; }}
    List<Expression<Func<T, object>>> Includes {{ get; }}
    List<string> IncludeStrings {{ get; }}
    Expression<Func<T, object>>? OrderBy {{ get; }}
    Expression<Func<T, object>>? OrderByDescending {{ get; }}
    int Take {{ get; }}
    int Skip {{ get; }}
    bool IsPagingEnabled {{ get; }}
}}";

    private static string GenerateBaseSpecification(string ns) => $@"using System.Linq.Expressions;

namespace {ns}.Specifications;

/// <summary>
/// Base specification implementation.
/// </summary>
public abstract class BaseSpecification<T> : ISpecification<T>
{{
    public Expression<Func<T, bool>> Criteria {{ get; protected set; }} = _ => true;
    public List<Expression<Func<T, object>>> Includes {{ get; }} = new();
    public List<string> IncludeStrings {{ get; }} = new();
    public Expression<Func<T, object>>? OrderBy {{ get; protected set; }}
    public Expression<Func<T, object>>? OrderByDescending {{ get; protected set; }}
    public int Take {{ get; protected set; }}
    public int Skip {{ get; protected set; }}
    public bool IsPagingEnabled {{ get; protected set; }}
    
    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);
    protected void ApplyPaging(int skip, int take) {{ Skip = skip; Take = take; IsPagingEnabled = true; }}
}}";

    private static string GenerateGenericRepositoryInterface(string ns, string domainNs) => $@"using {domainNs}.Entities;
using {domainNs}.Specifications;

namespace {ns}.Interfaces;

/// <summary>
/// Generic repository interface for data access.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T?> GetFirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}}";

    private static string GenerateUnitOfWorkInterface(string ns) => $@"namespace {ns}.Interfaces;

/// <summary>
/// Unit of Work pattern interface.
/// </summary>
public interface IUnitOfWork : IDisposable
{{
    ICustomerRepository Customers {{ get; }}
    IProductRepository Products {{ get; }}
    IOrderRepository Orders {{ get; }}
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}}";

    private static string GenerateEntityRepositoryInterface(string ns, string domainNs, string entity) => $@"using {domainNs}.Entities;

namespace {ns}.Interfaces;

/// <summary>
/// Repository interface for {entity} entity.
/// </summary>
public interface I{entity}Repository : IRepository<{entity}>
{{
    Task<{entity}?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<{entity}>> SearchAsync(string query, CancellationToken cancellationToken = default);
}}";

    private static string GenerateCustomerDtos(string ns) => $@"namespace {ns}.DTOs;

public record CustomerDto(int Id, string FirstName, string LastName, string Email, bool IsActive);
public record CreateCustomerDto(string FirstName, string LastName, string Email, string? Phone);
public record UpdateCustomerDto(int Id, string FirstName, string LastName, string? Phone, bool IsActive);";

    private static string GenerateProductDtos(string ns) => $@"namespace {ns}.DTOs;

public record ProductDto(int Id, string Name, string? Description, string Sku, decimal Price, int StockQuantity, bool IsAvailable);
public record CreateProductDto(string Name, string? Description, string Sku, decimal Price, int StockQuantity, int? CategoryId);
public record UpdateProductDto(int Id, string Name, string? Description, decimal Price, int StockQuantity, bool IsAvailable);";

    private static string GenerateOrderDtos(string ns) => $@"namespace {ns}.DTOs;

public record OrderDto(int Id, string OrderNumber, int CustomerId, string Status, decimal Total, DateTime CreatedAt);
public record CreateOrderDto(int CustomerId, List<OrderItemDto> Items, string ShippingStreet, string ShippingCity, string ShippingPostalCode);
public record OrderItemDto(int ProductId, int Quantity);";

    private static string GenerateCreateCustomerUseCase(string ns, string domainNs) => $@"using {ns}.DTOs;
using {ns}.Interfaces;
using {domainNs}.Entities;
using {domainNs}.ValueObjects;

namespace {ns}.UseCases.Customers;

public class CreateCustomer
{{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateCustomer(ICustomerRepository repository, IUnitOfWork unitOfWork)
    {{
        _repository = repository;
        _unitOfWork = unitOfWork;
    }}
    
    public async Task<CustomerDto> ExecuteAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {{
        var customer = new Customer
        {{
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = new Email(dto.Email),
            Phone = dto.Phone
        }};
        
        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new CustomerDto(customer.Id, customer.FirstName, customer.LastName, customer.Email.Value, customer.IsActive);
    }}
}}";

    private static string GenerateGetByIdUseCase(string ns, string domainNs, string entity) => $@"using {ns}.DTOs;
using {ns}.Interfaces;

namespace {ns}.UseCases.{entity}s;

public class Get{entity}ById
{{
    private readonly I{entity}Repository _repository;
    
    public Get{entity}ById(I{entity}Repository repository)
    {{
        _repository = repository;
    }}
    
    public async Task<{entity}Dto?> ExecuteAsync(int id, CancellationToken cancellationToken = default)
    {{
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;
        
        // Map to DTO (simplified)
        return null; // Actual mapping would go here
    }}
}}";

    private static string GenerateCreateOrderUseCase(string ns, string domainNs) => $@"using {ns}.DTOs;
using {ns}.Interfaces;
using {domainNs}.Entities;
using {domainNs}.ValueObjects;
using {domainNs}.Events;

namespace {ns}.UseCases.Orders;

public class CreateOrder
{{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateOrder(
        IOrderRepository orderRepository, 
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {{
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }}
    
    public async Task<OrderDto> ExecuteAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {{
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException(""Customer not found"");
        
        var order = new Order
        {{
            OrderNumber = GenerateOrderNumber(),
            CustomerId = dto.CustomerId,
            ShippingAddress = new Address 
            {{ 
                Street = dto.ShippingStreet, 
                City = dto.ShippingCity, 
                PostalCode = dto.ShippingPostalCode 
            }}
        }};
        
        foreach (var item in dto.Items)
        {{
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product != null) order.AddItem(product, item.Quantity);
        }}
        
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.CustomerId, order.Total.Amount));
        
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new OrderDto(order.Id, order.OrderNumber, order.CustomerId, order.Status.ToString(), order.Total.Amount, order.CreatedAt);
    }}
    
    private static string GenerateOrderNumber() => $""ORD-{{DateTime.UtcNow:yyyyMMdd}}-{{Guid.NewGuid().ToString()[..8].ToUpper()}}"";
}}";

    private static string GenerateEfMapper(string ns, string domainNs, string entity)
    {
        var mappingBody = entity switch
        {
            "Customer" => "new CustomerDto(entity.Id, entity.FirstName, entity.LastName, entity.Email.Value, entity.IsActive)",
            "Product" => "new ProductDto(entity.Id, entity.Name, entity.Description, entity.Sku, entity.Price.Amount, entity.StockQuantity, entity.IsAvailable)",
            "Order" => "new OrderDto(entity.Id, entity.OrderNumber, entity.CustomerId, entity.Status.ToString(), entity.Total.Amount, entity.CreatedAt)",
            _ => "throw new NotSupportedException()"
        };
        
        return $@"using {domainNs}.Entities;
using {ns}.DTOs;

namespace {ns}.Mappers;

public static class {entity}Mapper
{{
    public static {entity}Dto ToDto(this {entity} entity) => {mappingBody};
    
    public static IEnumerable<{entity}Dto> ToDtos(this IEnumerable<{entity}> entities) 
        => entities.Select(e => e.ToDto());
}}";
    }

    private static string GenerateValidator(string ns, string entity) => $@"using {ns}.DTOs;

namespace {ns}.Validators;

public class {entity}Validator
{{
    public (bool IsValid, List<string> Errors) Validate(Create{entity}Dto dto)
    {{
        var errors = new List<string>();
        
        // Add validation rules
        
        return (errors.Count == 0, errors);
    }}
}}";

    private static string GenerateAppDbContext(string infraNs, string domainNs) => $@"using Microsoft.EntityFrameworkCore;
using {domainNs}.Entities;

namespace {infraNs}.Data;

/// <summary>
/// Application database context.
/// </summary>
public class AppDbContext : DbContext
{{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {{ }}
    
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Category> Categories => Set<Category>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }}
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {{
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {{
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }}
        return base.SaveChangesAsync(cancellationToken);
    }}
}}";

    private static string GenerateDesignTimeFactory(string ns) => $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace {ns}.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{{
    public AppDbContext CreateDbContext(string[] args)
    {{
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(""Data Source=app.db"");
        return new AppDbContext(optionsBuilder.Options);
    }}
}}";

    private static string GenerateEntityConfiguration(string infraNs, string domainNs, string entity) => $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {domainNs}.Entities;

namespace {infraNs}.Data.Configurations;

public class {entity}Configuration : IEntityTypeConfiguration<{entity}>
{{
    public void Configure(EntityTypeBuilder<{entity}> builder)
    {{
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.HasQueryFilter(e => !e.IsDeleted);
        
        // Entity-specific configuration
    }}
}}";

    private static string GenerateEfBaseRepository(string infraNs, string domainNs, string appNs) => $@"using Microsoft.EntityFrameworkCore;
using {appNs}.Interfaces;
using {domainNs}.Entities;
using {domainNs}.Specifications;
using {infraNs}.Data;

namespace {infraNs}.Repositories;

/// <summary>
/// Base repository implementation using EF Core.
/// </summary>
public class BaseRepository<T> : IRepository<T> where T : BaseEntity
{{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public BaseRepository(AppDbContext context)
    {{
        _context = context;
        _dbSet = context.Set<T>();
    }}
    
    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync(new object[] {{ id }}, cancellationToken);
    
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);
    
    public virtual async Task<IReadOnlyList<T>> GetAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        => await ApplySpecification(spec).ToListAsync(cancellationToken);
    
    public virtual async Task<T?> GetFirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        => await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    
    public virtual async Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        => await ApplySpecification(spec).CountAsync(cancellationToken);
    
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {{
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }}
    
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {{
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }}
    
    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {{
        entity.IsDeleted = true;
        return UpdateAsync(entity, cancellationToken);
    }}
    
    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {{
        var query = _dbSet.Where(spec.Criteria);
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        if (spec.OrderBy != null) query = query.OrderBy(spec.OrderBy);
        if (spec.OrderByDescending != null) query = query.OrderByDescending(spec.OrderByDescending);
        if (spec.IsPagingEnabled) query = query.Skip(spec.Skip).Take(spec.Take);
        return query;
    }}
}}";

    private static string GenerateEfRepository(string infraNs, string domainNs, string appNs, string entity) => $@"using Microsoft.EntityFrameworkCore;
using {appNs}.Interfaces;
using {domainNs}.Entities;
using {infraNs}.Data;

namespace {infraNs}.Repositories;

public class {entity}Repository : BaseRepository<{entity}>, I{entity}Repository
{{
    public {entity}Repository(AppDbContext context) : base(context) {{ }}
    
    public async Task<{entity}?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(e => EF.Property<string>(e, ""Email"") == email, cancellationToken);
    
    public async Task<IReadOnlyList<{entity}>> SearchAsync(string query, CancellationToken cancellationToken = default)
        => await _dbSet.Where(e => EF.Functions.Like(EF.Property<string>(e, ""Name""), $""%{{query}}%"")).ToListAsync(cancellationToken);
}}";

    private static string GenerateUnitOfWork(string infraNs, string appNs) => $@"using Microsoft.EntityFrameworkCore.Storage;
using {appNs}.Interfaces;
using {infraNs}.Data;

namespace {infraNs}.Repositories;

public class UnitOfWork : IUnitOfWork
{{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    
    public ICustomerRepository Customers {{ get; }}
    public IProductRepository Products {{ get; }}
    public IOrderRepository Orders {{ get; }}
    
    public UnitOfWork(AppDbContext context, ICustomerRepository customers, IProductRepository products, IOrderRepository orders)
    {{
        _context = context;
        Customers = customers;
        Products = products;
        Orders = orders;
    }}
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {{
        if (_transaction != null) await _transaction.CommitAsync(cancellationToken);
    }}
    
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {{
        if (_transaction != null) await _transaction.RollbackAsync(cancellationToken);
    }}
    
    public void Dispose()
    {{
        _transaction?.Dispose();
        _context.Dispose();
    }}
}}";

    private static string GenerateInitialMigration(string ns) => $@"using Microsoft.EntityFrameworkCore.Migrations;

namespace {ns}.Migrations;

public partial class InitialCreate : Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        // Initial migration created by EF Core
        // Tables: Customers, Products, Orders, OrderItems, Categories
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
        // Rollback migration
    }}
}}";

    private static string GenerateDateTimeService(string ns) => $@"namespace {ns}.Services;

public interface IDateTimeService
{{
    DateTime UtcNow {{ get; }}
}}

public class DateTimeService : IDateTimeService
{{
    public DateTime UtcNow => DateTime.UtcNow;
}}";

    private static string GenerateEmailServiceImpl(string ns) => $@"namespace {ns}.Services;

public interface IEmailService
{{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}}

public class EmailService : IEmailService
{{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {{
        // Implementation
        return Task.CompletedTask;
    }}
}}";

    private static string GenerateServiceCollectionExtensions(string infraNs, string domainNs, string appNs) => $@"using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using {appNs}.Interfaces;
using {infraNs}.Data;
using {infraNs}.Repositories;
using {infraNs}.Services;

namespace {infraNs}.Extensions;

public static class ServiceCollectionExtensions
{{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {{
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IEmailService, EmailService>();
        
        return services;
    }}
}}";

    private static string GenerateProgramCs(string apiNs, string infraNs) => $@"using {infraNs}.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString(""DefaultConnection"") ?? ""Data Source=app.db"");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();";

    private static string GenerateApiController(string apiNs, string appNs, string entity) => $@"using Microsoft.AspNetCore.Mvc;
using {appNs}.DTOs;
using {appNs}.Interfaces;

namespace {apiNs}.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class {entity}sController : ControllerBase
{{
    private readonly I{entity}Repository _repository;
    
    public {entity}sController(I{entity}Repository repository)
    {{
        _repository = repository;
    }}
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<{entity}Dto>>> GetAll(CancellationToken cancellationToken)
    {{
        var items = await _repository.GetAllAsync(cancellationToken);
        return Ok(items);
    }}
    
    [HttpGet(""{{id}}"")]
    public async Task<ActionResult<{entity}Dto>> GetById(int id, CancellationToken cancellationToken)
    {{
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return Ok(item);
    }}
}}";

    private static string GenerateExceptionMiddleware(string ns) => $@"namespace {ns}.Middleware;

public class ExceptionHandlingMiddleware
{{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {{
        _next = next;
        _logger = logger;
    }}
    
    public async Task InvokeAsync(HttpContext context)
    {{
        try
        {{
            await _next(context);
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Unhandled exception"");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new {{ error = ""Internal server error"" }});
        }}
    }}
}}";

    private static string GenerateLoggingMiddleware(string ns) => $@"namespace {ns}.Middleware;

public class RequestLoggingMiddleware
{{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {{
        _next = next;
        _logger = logger;
    }}
    
    public async Task InvokeAsync(HttpContext context)
    {{
        _logger.LogInformation(""Request: {{Method}} {{Path}}"", context.Request.Method, context.Request.Path);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _next(context);
        sw.Stop();
        _logger.LogInformation(""Response: {{StatusCode}} in {{ElapsedMs}}ms"", context.Response.StatusCode, sw.ElapsedMilliseconds);
    }}
}}";

    private static string GenerateMockRepository(string ns) => $@"namespace {ns}.Mocks;

public class MockRepository<T> where T : class
{{
    private readonly List<T> _items = new();
    
    public Task<T?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault());
    public Task<IReadOnlyList<T>> GetAllAsync() => Task.FromResult<IReadOnlyList<T>>(_items);
    public Task<T> AddAsync(T entity) {{ _items.Add(entity); return Task.FromResult(entity); }}
    public Task UpdateAsync(T entity) => Task.CompletedTask;
    public Task DeleteAsync(T entity) {{ _items.Remove(entity); return Task.CompletedTask; }}
}}";

    private static string GenerateIntegrationTest(string ns, string entity) => $@"using Xunit;
using FluentAssertions;
using {ns}.Fixtures;

namespace {ns};

public class {entity}IntegrationTests : IClassFixture<WebApplicationFixture>
{{
    private readonly WebApplicationFixture _fixture;
    
    public {entity}IntegrationTests(WebApplicationFixture fixture)
    {{
        _fixture = fixture;
    }}
    
    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {{
        // Arrange
        var client = _fixture.CreateClient();
        
        // Act
        var response = await client.GetAsync(""/api/{entity.ToLower()}s"");
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }}
}}";

    private static string GenerateDatabaseFixture(string ns) => $@"using Microsoft.EntityFrameworkCore;

namespace {ns}.Fixtures;

public class DatabaseFixture : IDisposable
{{
    public string ConnectionString {{ get; }}
    
    public DatabaseFixture()
    {{
        ConnectionString = $""Data Source={{Guid.NewGuid()}}.db"";
    }}
    
    public void Dispose()
    {{
        // Cleanup database
    }}
}}";

    private static string GenerateWebAppFixture(string ns) => $@"using Microsoft.AspNetCore.Mvc.Testing;

namespace {ns}.Fixtures;

public class WebApplicationFixture : IDisposable
{{
    // Simplified - actual implementation would use WebApplicationFactory
    public HttpClient CreateClient() => new HttpClient();
    public void Dispose() {{ }}
}}";
    
    #endregion

    /// <summary>
    /// Creates a rich solution with multiple projects, folders, and files.
    /// Suitable for comprehensive migration testing.
    /// </summary>
    public static FakeSolution CreateRichSolution(TempDirectoryFixture tempDir, string solutionName = "RichSolution")
    {
        var projects = new List<FakeProject>();
        var allGuids = new List<(string Name, string Path, Guid Guid)>();
        var folderGuids = new List<(string Name, Guid Guid)>();
        
        var srcFolderGuid = Guid.NewGuid();
        var testFolderGuid = Guid.NewGuid();
        folderGuids.Add(("src", srcFolderGuid));
        folderGuids.Add(("test", testFolderGuid));

        // === Source Projects ===
        
        // 1. Core project with Models, Services, Helpers
        var coreProject = CreateCoreProject(tempDir, solutionName, "src");
        projects.Add(coreProject);
        allGuids.Add((coreProject.Name, $"src/{coreProject.Name}/{coreProject.Name}.csproj", coreProject.Guid));
        
        // 2. Infrastructure project
        var infraProject = CreateInfrastructureProject(tempDir, solutionName, "src", coreProject.Name);
        projects.Add(infraProject);
        allGuids.Add((infraProject.Name, $"src/{infraProject.Name}/{infraProject.Name}.csproj", infraProject.Guid));
        
        // 3. Application project  
        var appProject = CreateApplicationProject(tempDir, solutionName, "src", coreProject.Name, infraProject.Name);
        projects.Add(appProject);
        allGuids.Add((appProject.Name, $"src/{appProject.Name}/{appProject.Name}.csproj", appProject.Guid));

        // === Test Projects ===
        
        // 4. Core.Tests
        var coreTestsProject = CreateTestProject(tempDir, solutionName, "test", coreProject.Name, "Core");
        projects.Add(coreTestsProject);
        allGuids.Add((coreTestsProject.Name, $"test/{coreTestsProject.Name}/{coreTestsProject.Name}.csproj", coreTestsProject.Guid));
        
        // 5. Infrastructure.Tests
        var infraTestsProject = CreateTestProject(tempDir, solutionName, "test", infraProject.Name, "Infrastructure");
        projects.Add(infraTestsProject);
        allGuids.Add((infraTestsProject.Name, $"test/{infraTestsProject.Name}/{infraTestsProject.Name}.csproj", infraTestsProject.Guid));

        // Create solution file
        var slnContent = GenerateSlnWithNestedFolders(solutionName, folderGuids, allGuids, 
            srcFolderGuid, testFolderGuid, projects);
        var slnPath = tempDir.CreateFile($"{solutionName}.sln", slnContent);

        return new FakeSolution
        {
            Name = solutionName,
            Path = slnPath,
            Directory = tempDir.Path,
            Projects = projects,
            Folders = ["src", "test"]
        };
    }
    
    private static FakeProject CreateCoreProject(TempDirectoryFixture tempDir, string solutionName, string baseDir)
    {
        var projectName = $"{solutionName}.Core";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateCsproj(projectName, "net9.0", isTestProject: false));
        
        // Models folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Models");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Models/User.cs", GenerateModelClass(projectName, "User", 
            ("Id", "int"), ("Name", "string"), ("Email", "string"), ("CreatedAt", "DateTime"))));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Models/Product.cs", GenerateModelClass(projectName, "Product",
            ("Id", "int"), ("Name", "string"), ("Price", "decimal"), ("Category", "string"))));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Models/Order.cs", GenerateModelClass(projectName, "Order",
            ("Id", "int"), ("UserId", "int"), ("Total", "decimal"), ("Status", "string"))));
        
        // Interfaces folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Interfaces");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IRepository.cs", GenerateRepositoryInterface(projectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IUserService.cs", GenerateServiceInterface(projectName, "User")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Interfaces/IProductService.cs", GenerateServiceInterface(projectName, "Product")));
        
        // Helpers folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Helpers");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Helpers/StringExtensions.cs", GenerateExtensionsClass(projectName, "StringExtensions")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Helpers/DateTimeHelpers.cs", GenerateHelperClass(projectName, "DateTimeHelpers")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Helpers/ValidationHelper.cs", GenerateHelperClass(projectName, "ValidationHelper")));
        
        // Exceptions folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Exceptions");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Exceptions/NotFoundException.cs", GenerateExceptionClass(projectName, "NotFoundException")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Exceptions/ValidationException.cs", GenerateExceptionClass(projectName, "ValidationException")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Models", "Interfaces", "Helpers", "Exceptions"]
        };
    }
    
    private static FakeProject CreateInfrastructureProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, string coreProjectName)
    {
        var projectName = $"{solutionName}.Infrastructure";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj with reference to Core
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateCsprojWithReference(projectName, "net9.0", coreProjectName));
        
        // Repositories folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Repositories");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/BaseRepository.cs", GenerateBaseRepository(projectName, coreProjectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/UserRepository.cs", GenerateRepository(projectName, coreProjectName, "User")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Repositories/ProductRepository.cs", GenerateRepository(projectName, coreProjectName, "Product")));
        
        // Data folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Data");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/AppDbContext.cs", GenerateDbContext(projectName, coreProjectName)));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Data/DbInitializer.cs", GenerateDbInitializer(projectName)));
        
        // Services folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Services");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/EmailService.cs", GenerateInfraService(projectName, "EmailService")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/CacheService.cs", GenerateInfraService(projectName, "CacheService")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Repositories", "Data", "Services"],
            References = [coreProjectName]
        };
    }
    
    private static FakeProject CreateApplicationProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, 
        string coreProjectName, string infraProjectName)
    {
        var projectName = $"{solutionName}.Application";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj with references
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateCsprojWithReferences(projectName, "net9.0", [coreProjectName, infraProjectName]));
        
        // Services folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Services");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/UserService.cs", GenerateAppService(projectName, coreProjectName, "User")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/ProductService.cs", GenerateAppService(projectName, coreProjectName, "Product")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Services/OrderService.cs", GenerateAppService(projectName, coreProjectName, "Order")));
        
        // DTOs folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/DTOs");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/DTOs/UserDto.cs", GenerateDto(projectName, "User")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/DTOs/ProductDto.cs", GenerateDto(projectName, "Product")));
        
        // Mappers folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Mappers");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Mappers/UserMapper.cs", GenerateMapper(projectName, coreProjectName, "User")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Mappers/ProductMapper.cs", GenerateMapper(projectName, coreProjectName, "Product")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = false,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Services", "DTOs", "Mappers"],
            References = [coreProjectName, infraProjectName]
        };
    }
    
    private static FakeProject CreateTestProject(TempDirectoryFixture tempDir, string solutionName, string baseDir, 
        string referencedProject, string projectType)
    {
        var projectName = $"{solutionName}.{projectType}.Tests";
        var projectGuid = Guid.NewGuid();
        var files = new List<string>();
        
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}");
        
        // Create .csproj
        var csprojPath = tempDir.CreateFile($"{baseDir}/{projectName}/{projectName}.csproj", 
            GenerateTestCsproj(projectName, "net9.0", referencedProject, $"../../src/{referencedProject}"));
        
        // Unit tests
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Unit");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Unit/SampleTests.cs", GenerateTestClass(projectName + ".Unit", $"{projectType}Tests")));
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Unit/HelperTests.cs", GenerateTestClass(projectName + ".Unit", "HelperTests")));
        
        // Integration tests folder
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Integration");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Integration/IntegrationTests.cs", GenerateTestClass(projectName + ".Integration", "IntegrationTests")));
        
        // Fixtures
        tempDir.CreateSubdirectory($"{baseDir}/{projectName}/Fixtures");
        files.Add(tempDir.CreateFile($"{baseDir}/{projectName}/Fixtures/TestFixture.cs", GenerateTestFixture(projectName + ".Fixtures", "TestFixture")));
        
        return new FakeProject
        {
            Name = projectName,
            Path = csprojPath,
            IsTestProject = true,
            Guid = projectGuid,
            SourceFiles = files,
            Folders = ["Unit", "Integration", "Fixtures"],
            References = [referencedProject]
        };
    }

    /// <summary>
    /// Creates a minimal solution with one project.
    /// </summary>
    public static FakeSolution CreateMinimalSolution(TempDirectoryFixture tempDir, string solutionName = "TestSolution")
    {
        var projectName = $"{solutionName}.Core";
        var projectGuid = Guid.NewGuid();

        // Create project directory
        var projectDir = tempDir.CreateSubdirectory(projectName);

        // Create .csproj file
        var csprojContent = GenerateCsproj(projectName, "net9.0", isTestProject: false);
        var csprojPath = tempDir.CreateFile($"{projectName}/{projectName}.csproj", csprojContent);

        // Create a simple C# file
        var csContent = GenerateSimpleClass(projectName, "SampleClass");
        tempDir.CreateFile($"{projectName}/SampleClass.cs", csContent);

        // Create .sln file
        var slnContent = GenerateSln(solutionName, [(projectName, $"{projectName}/{projectName}.csproj", projectGuid)]);
        var slnPath = tempDir.CreateFile($"{solutionName}.sln", slnContent);

        return new FakeSolution
        {
            Name = solutionName,
            Path = slnPath,
            Directory = tempDir.Path,
            Projects = [new FakeProject { Name = projectName, Path = csprojPath, IsTestProject = false }]
        };
    }

    /// <summary>
    /// Creates a solution with source and test projects.
    /// </summary>
    public static FakeSolution CreateSolutionWithTests(TempDirectoryFixture tempDir, string solutionName = "TestSolution")
    {
        var srcProjectName = $"{solutionName}.Core";
        var testProjectName = $"{solutionName}.Core.Tests";
        var srcGuid = Guid.NewGuid();
        var testGuid = Guid.NewGuid();

        // Create source project
        tempDir.CreateSubdirectory(srcProjectName);
        var srcCsproj = GenerateCsproj(srcProjectName, "net9.0", isTestProject: false);
        var srcCsprojPath = tempDir.CreateFile($"{srcProjectName}/{srcProjectName}.csproj", srcCsproj);
        tempDir.CreateFile($"{srcProjectName}/Calculator.cs", GenerateSimpleClass(srcProjectName, "Calculator"));

        // Create test project
        tempDir.CreateSubdirectory(testProjectName);
        var testCsproj = GenerateTestCsproj(testProjectName, "net9.0", srcProjectName);
        var testCsprojPath = tempDir.CreateFile($"{testProjectName}/{testProjectName}.csproj", testCsproj);
        tempDir.CreateFile($"{testProjectName}/CalculatorTests.cs", GenerateTestClass(testProjectName, "CalculatorTests"));

        // Create .sln file
        var slnContent = GenerateSln(solutionName, [
            (srcProjectName, $"{srcProjectName}/{srcProjectName}.csproj", srcGuid),
            (testProjectName, $"{testProjectName}/{testProjectName}.csproj", testGuid)
        ]);
        var slnPath = tempDir.CreateFile($"{solutionName}.sln", slnContent);

        return new FakeSolution
        {
            Name = solutionName,
            Path = slnPath,
            Directory = tempDir.Path,
            Projects = [
                new FakeProject { Name = srcProjectName, Path = srcCsprojPath, IsTestProject = false },
                new FakeProject { Name = testProjectName, Path = testCsprojPath, IsTestProject = true }
            ]
        };
    }

    /// <summary>
    /// Creates a solution with solution folders.
    /// </summary>
    public static FakeSolution CreateSolutionWithFolders(TempDirectoryFixture tempDir, string solutionName = "TestSolution")
    {
        var projectName = $"{solutionName}.Core";
        var projectGuid = Guid.NewGuid();
        var folderGuid = Guid.NewGuid();

        // Create project
        tempDir.CreateSubdirectory($"src/{projectName}");
        var csprojContent = GenerateCsproj(projectName, "net9.0", isTestProject: false);
        var csprojPath = tempDir.CreateFile($"src/{projectName}/{projectName}.csproj", csprojContent);
        tempDir.CreateFile($"src/{projectName}/Class1.cs", GenerateSimpleClass(projectName, "Class1"));

        // Create .sln with solution folder
        var slnContent = GenerateSlnWithFolders(solutionName, 
            [("src", folderGuid)],
            [(projectName, $"src/{projectName}/{projectName}.csproj", projectGuid)]);
        var slnPath = tempDir.CreateFile($"{solutionName}.sln", slnContent);

        return new FakeSolution
        {
            Name = solutionName,
            Path = slnPath,
            Directory = tempDir.Path,
            Projects = [new FakeProject { Name = projectName, Path = csprojPath, IsTestProject = false }],
            Folders = ["src"]
        };
    }

    #region Content Generators

    private static string GenerateCsproj(string projectName, string targetFramework, bool isTestProject)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
    }

    private static string GenerateTestCsproj(string projectName, string targetFramework, string referencedProject)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{referencedProject}\{referencedProject}.csproj"" />
  </ItemGroup>
</Project>";
    }

    private static string GenerateSimpleClass(string namespaceName, string className)
    {
        return $@"namespace {namespaceName};

/// <summary>
/// A simple class for testing.
/// </summary>
public class {className}
{{
    public int Add(int a, int b) => a + b;
    
    public int Subtract(int a, int b) => a - b;
}}";
    }

    private static string GenerateTestClass(string namespaceName, string className)
    {
        return $@"using Xunit;

namespace {namespaceName};

/// <summary>
/// Test class for testing.
/// </summary>
public class {className}
{{
    [Fact]
    public void Add_ReturnsSum()
    {{
        // Arrange & Act & Assert
        Assert.Equal(5, 2 + 3);
    }}

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(0, 0, 0)]
    public void Add_WithTheoryData_ReturnsSum(int a, int b, int expected)
    {{
        Assert.Equal(expected, a + b);
    }}
}}";
    }
    
    private static string GenerateModelClass(string namespaceName, string className, params (string Name, string Type)[] properties)
    {
        var props = string.Join(Environment.NewLine + "    ", 
            properties.Select(p => $"public {p.Type} {p.Name} {{ get; set; }}"));
        
        return $@"namespace {namespaceName}.Models;

/// <summary>
/// {className} entity model.
/// </summary>
public class {className}
{{
    {props}
}}";
    }
    
    private static string GenerateRepositoryInterface(string namespaceName)
    {
        return $@"namespace {namespaceName}.Interfaces;

/// <summary>
/// Generic repository interface.
/// </summary>
public interface IRepository<T> where T : class
{{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}}";
    }
    
    private static string GenerateServiceInterface(string namespaceName, string entityName)
    {
        return $@"using {namespaceName}.Models;

namespace {namespaceName}.Interfaces;

/// <summary>
/// Service interface for {entityName} operations.
/// </summary>
public interface I{entityName}Service
{{
    Task<{entityName}?> GetByIdAsync(int id);
    Task<IEnumerable<{entityName}>> GetAllAsync();
    Task<{entityName}> CreateAsync({entityName} entity);
    Task UpdateAsync({entityName} entity);
    Task DeleteAsync(int id);
}}";
    }
    
    private static string GenerateExtensionsClass(string namespaceName, string className)
    {
        return $@"namespace {namespaceName}.Helpers;

/// <summary>
/// String extension methods.
/// </summary>
public static class {className}
{{
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);
    
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);
    
    public static string ToTitleCase(this string value)
    {{
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0]) + value[1..].ToLower();
    }}
    
    public static string Truncate(this string value, int maxLength)
    {{
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + ""..."";
    }}
}}";
    }
    
    private static string GenerateHelperClass(string namespaceName, string className)
    {
        return $@"namespace {namespaceName}.Helpers;

/// <summary>
/// Helper class: {className}.
/// </summary>
public static class {className}
{{
    public static bool Validate(object? value) => value != null;
    
    public static T ThrowIfNull<T>(T? value, string paramName) where T : class
    {{
        return value ?? throw new ArgumentNullException(paramName);
    }}
    
    public static void EnsureNotEmpty(string value, string paramName)
    {{
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(""Value cannot be empty"", paramName);
    }}
}}";
    }
    
    private static string GenerateExceptionClass(string namespaceName, string className)
    {
        return $@"namespace {namespaceName}.Exceptions;

/// <summary>
/// Custom exception: {className}.
/// </summary>
public class {className} : Exception
{{
    public {className}() : base() {{ }}
    
    public {className}(string message) : base(message) {{ }}
    
    public {className}(string message, Exception innerException) 
        : base(message, innerException) {{ }}
}}";
    }
    
    private static string GenerateCsprojWithReference(string projectName, string targetFramework, string referencedProject)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\{referencedProject}\{referencedProject}.csproj"" />
  </ItemGroup>
</Project>";
    }
    
    private static string GenerateCsprojWithReferences(string projectName, string targetFramework, string[] referencedProjects)
    {
        var refs = string.Join(Environment.NewLine + "    ", 
            referencedProjects.Select(r => $@"<ProjectReference Include=""..\{r}\{r}.csproj"" />"));
        
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    {refs}
  </ItemGroup>
</Project>";
    }
    
    private static string GenerateTestCsproj(string projectName, string targetFramework, string referencedProject, string refPath)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.12.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""Moq"" Version=""4.20.70"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""{refPath}\{referencedProject}.csproj"" />
  </ItemGroup>
</Project>";
    }
    
    private static string GenerateBaseRepository(string namespaceName, string coreProjectName)
    {
        return $@"using {coreProjectName}.Interfaces;

namespace {namespaceName}.Repositories;

/// <summary>
/// Base repository implementation.
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{{
    protected readonly List<T> _items = new();
    
    public virtual Task<T?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault());
    
    public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(_items.AsEnumerable());
    
    public virtual Task<T> AddAsync(T entity)
    {{
        _items.Add(entity);
        return Task.FromResult(entity);
    }}
    
    public virtual Task UpdateAsync(T entity) => Task.CompletedTask;
    
    public virtual Task DeleteAsync(int id)
    {{
        var item = _items.FirstOrDefault();
        if (item != null) _items.Remove(item);
        return Task.CompletedTask;
    }}
}}";
    }
    
    private static string GenerateRepository(string namespaceName, string coreProjectName, string entityName)
    {
        return $@"using {coreProjectName}.Models;

namespace {namespaceName}.Repositories;

/// <summary>
/// Repository for {entityName} entities.
/// </summary>
public class {entityName}Repository : BaseRepository<{entityName}>
{{
    public Task<{entityName}?> GetByEmailAsync(string email)
    {{
        // Implementation would query by email
        return Task.FromResult<{entityName}?>(null);
    }}
    
    public Task<IEnumerable<{entityName}>> SearchAsync(string query)
    {{
        // Implementation would search
        return Task.FromResult(Enumerable.Empty<{entityName}>());
    }}
}}";
    }
    
    private static string GenerateDbContext(string namespaceName, string coreProjectName)
    {
        return $@"using {coreProjectName}.Models;

namespace {namespaceName}.Data;

/// <summary>
/// Application database context.
/// </summary>
public class AppDbContext
{{
    public List<User> Users {{ get; set; }} = new();
    public List<Product> Products {{ get; set; }} = new();
    public List<Order> Orders {{ get; set; }} = new();
    
    public Task<int> SaveChangesAsync() => Task.FromResult(1);
}}";
    }
    
    private static string GenerateDbInitializer(string namespaceName)
    {
        return $@"namespace {namespaceName}.Data;

/// <summary>
/// Database initializer for seeding data.
/// </summary>
public static class DbInitializer
{{
    public static async Task InitializeAsync(AppDbContext context)
    {{
        // Seed initial data
        await context.SaveChangesAsync();
    }}
    
    public static void SeedTestData(AppDbContext context)
    {{
        // Add test data
    }}
}}";
    }
    
    private static string GenerateInfraService(string namespaceName, string serviceName)
    {
        return $@"namespace {namespaceName}.Services;

/// <summary>
/// Infrastructure service: {serviceName}.
/// </summary>
public class {serviceName}
{{
    public Task SendAsync(string to, string subject, string body)
    {{
        // Implementation
        return Task.CompletedTask;
    }}
    
    public Task<T?> GetAsync<T>(string key) where T : class
    {{
        return Task.FromResult<T?>(null);
    }}
    
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {{
        return Task.CompletedTask;
    }}
}}";
    }
    
    private static string GenerateAppService(string namespaceName, string coreProjectName, string entityName)
    {
        return $@"using {coreProjectName}.Interfaces;
using {coreProjectName}.Models;

namespace {namespaceName}.Services;

/// <summary>
/// Application service for {entityName} operations.
/// </summary>
public class {entityName}Service : I{entityName}Service
{{
    private readonly IRepository<{entityName}> _repository;
    
    public {entityName}Service(IRepository<{entityName}> repository)
    {{
        _repository = repository;
    }}
    
    public Task<{entityName}?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    
    public Task<IEnumerable<{entityName}>> GetAllAsync() => _repository.GetAllAsync();
    
    public Task<{entityName}> CreateAsync({entityName} entity) => _repository.AddAsync(entity);
    
    public Task UpdateAsync({entityName} entity) => _repository.UpdateAsync(entity);
    
    public Task DeleteAsync(int id) => _repository.DeleteAsync(id);
}}";
    }
    
    private static string GenerateDto(string namespaceName, string entityName)
    {
        return $@"namespace {namespaceName}.DTOs;

/// <summary>
/// Data transfer object for {entityName}.
/// </summary>
public record {entityName}Dto
{{
    public int Id {{ get; init; }}
    public string Name {{ get; init; }} = string.Empty;
    public DateTime CreatedAt {{ get; init; }}
}}

public record Create{entityName}Dto
{{
    public required string Name {{ get; init; }}
}}

public record Update{entityName}Dto
{{
    public required int Id {{ get; init; }}
    public required string Name {{ get; init; }}
}}";
    }
    
    private static string GenerateMapper(string namespaceName, string coreProjectName, string entityName)
    {
        return $@"using {coreProjectName}.Models;
using {namespaceName}.DTOs;

namespace {namespaceName}.Mappers;

/// <summary>
/// Mapper for {entityName} entity.
/// </summary>
public static class {entityName}Mapper
{{
    public static {entityName}Dto ToDto(this {entityName} entity) => new()
    {{
        Id = entity.Id,
        Name = entity.Name,
        CreatedAt = DateTime.UtcNow
    }};
    
    public static {entityName} ToEntity(this Create{entityName}Dto dto) => new()
    {{
        Name = dto.Name
    }};
    
    public static IEnumerable<{entityName}Dto> ToDtos(this IEnumerable<{entityName}> entities) 
        => entities.Select(e => e.ToDto());
}}";
    }
    
    private static string GenerateTestFixture(string namespaceName, string className)
    {
        return $@"using Xunit;

namespace {namespaceName};

/// <summary>
/// Test fixture for shared test context.
/// </summary>
public class {className} : IDisposable
{{
    public string TestData {{ get; }}
    
    public {className}()
    {{
        TestData = ""Initialized"";
    }}
    
    public void Dispose()
    {{
        // Cleanup
    }}
}}

[CollectionDefinition(""Test Collection"")]
public class TestCollection : ICollectionFixture<{className}> {{ }}";
    }
    
    private static string GenerateSlnWithNestedFolders(
        string solutionName,
        List<(string Name, Guid Guid)> folders,
        List<(string Name, string Path, Guid Guid)> projects,
        Guid srcFolderGuid,
        Guid testFolderGuid,
        List<FakeProject> fakeProjects)
    {
        var folderLines = string.Join(Environment.NewLine, folders.Select(f =>
            $"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"{f.Name}\", \"{f.Name}\", \"{{{f.Guid.ToString().ToUpperInvariant()}}}\"\r\nEndProject"));

        var projectLines = string.Join(Environment.NewLine, projects.Select(p =>
            $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{p.Name}\", \"{p.Path}\", \"{{{p.Guid.ToString().ToUpperInvariant()}}}\"\r\nEndProject"));

        var configLines = string.Join(Environment.NewLine, projects.SelectMany(p => new[]
        {
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Release|Any CPU.Build.0 = Release|Any CPU"
        }));

        // Nest projects - src projects under src folder, test projects under test folder
        var nestedLines = string.Join(Environment.NewLine, fakeProjects.Select(p =>
        {
            var parentGuid = p.IsTestProject ? testFolderGuid : srcFolderGuid;
            return $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}} = {{{parentGuid.ToString().ToUpperInvariant()}}}";
        }));

        return $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
{folderLines}
{projectLines}
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
{configLines}
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
{nestedLines}
	EndGlobalSection
EndGlobal";
    }

    private static string GenerateSln(string solutionName, List<(string Name, string Path, Guid Guid)> projects)
    {
        var projectLines = string.Join(Environment.NewLine, projects.Select(p =>
            $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{p.Name}\", \"{p.Path}\", \"{{{p.Guid.ToString().ToUpperInvariant()}}}\"\r\nEndProject"));

        var configLines = string.Join(Environment.NewLine, projects.SelectMany(p => new[]
        {
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Release|Any CPU.Build.0 = Release|Any CPU"
        }));

        return $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
{projectLines}
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
{configLines}
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal";
    }

    private static string GenerateSlnWithFolders(
        string solutionName,
        List<(string Name, Guid Guid)> folders,
        List<(string Name, string Path, Guid Guid)> projects)
    {
        var folderLines = string.Join(Environment.NewLine, folders.Select(f =>
            $"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"{f.Name}\", \"{f.Name}\", \"{{{f.Guid.ToString().ToUpperInvariant()}}}\"\r\nEndProject"));

        var projectLines = string.Join(Environment.NewLine, projects.Select(p =>
            $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{p.Name}\", \"{p.Path}\", \"{{{p.Guid.ToString().ToUpperInvariant()}}}\"\r\nEndProject"));

        var configLines = string.Join(Environment.NewLine, projects.SelectMany(p => new[]
        {
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
            $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}}.Release|Any CPU.Build.0 = Release|Any CPU"
        }));

        // Nest projects under folders (assuming first folder contains all projects)
        var nestedLines = folders.Count > 0 && projects.Count > 0
            ? string.Join(Environment.NewLine, projects.Select(p =>
                $"\t\t{{{p.Guid.ToString().ToUpperInvariant()}}} = {{{folders[0].Guid.ToString().ToUpperInvariant()}}}"))
            : "";

        return $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
{folderLines}
{projectLines}
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
{configLines}
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
{nestedLines}
	EndGlobalSection
EndGlobal";
    }

    #endregion
}

/// <summary>
/// Represents a generated fake solution.
/// </summary>
public class FakeSolution
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string Directory { get; init; }
    public IReadOnlyList<FakeProject> Projects { get; init; } = [];
    public IReadOnlyList<string> Folders { get; init; } = [];
}

/// <summary>
/// Represents a generated fake project.
/// </summary>
public class FakeProject
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public bool IsTestProject { get; init; }
    public Guid Guid { get; init; } = Guid.NewGuid();
    public IReadOnlyList<string> SourceFiles { get; init; } = [];
    public IReadOnlyList<string> Folders { get; init; } = [];
    public IReadOnlyList<string> References { get; init; } = [];
}
