using Microsoft.AspNetCore.Server.Kestrel.Core;
using MigrationTool.GrpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/2 for gRPC on port 5001
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
    
    // HTTP/1.1 for gRPC-Web on port 5000
    options.ListenLocalhost(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

// Add gRPC services
builder.Services.AddGrpc();

// Add CORS for gRPC-Web
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

// TODO: Register your services here
// builder.Services.AddSingleton<ISolutionAnalyzer, SolutionAnalyzer>();
// builder.Services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
// builder.Services.AddSingleton<IMigrationExecutor, MigrationExecutor>();

var app = builder.Build();

// Enable CORS
app.UseCors("AllowAll");

// Enable gRPC-Web (allows browsers to call gRPC)
app.UseGrpcWeb();

// Map gRPC services
app.MapGrpcService<MigrationServiceImpl>()
   .EnableGrpcWeb()
   .RequireCors("AllowAll");

// Health check endpoint
app.MapGet("/", () => "Migration Tool gRPC Server is running. Use gRPC client to connect.");

app.Run();
