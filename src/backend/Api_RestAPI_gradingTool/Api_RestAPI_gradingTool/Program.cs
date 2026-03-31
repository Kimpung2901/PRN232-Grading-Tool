using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Api_RestAPI_gradingTool.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<Api_RestAPI_gradingTool.Swagger.FileUploadOperationFilter>();
    options.MapType<Microsoft.AspNetCore.Http.IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});

var connectionString = Environment.GetEnvironmentVariable("GRADING_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing GRADING_DB_CONNECTION or ConnectionStrings:GradingDb.");
}

builder.Services.AddDbContext<Infrastructure.Persistence.GradingDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<Application.Contracts.Grading.IGradingDbContext>(sp =>
    sp.GetRequiredService<Infrastructure.Persistence.GradingDbContext>());
builder.Services.AddScoped<Application.Contracts.Grading.IRunnerStorage, Infrastructure.Runner.RunnerStorage>();

builder.Services.AddScoped<Application.Contracts.Management.IExamService, Application.Services.ExamService>();
builder.Services.AddScoped<Application.Contracts.Management.ITestCaseService, Application.Services.TestCaseService>();
builder.Services.AddScoped<Application.Contracts.Management.ISubmissionService, Application.Services.SubmissionService>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE", p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.GradingDbContext>();
    var conn = db.Database.GetConnectionString();
    Console.WriteLine($"?? Connection string: {conn}");

    var pending = db.Database.GetPendingMigrations().ToList();
    Console.WriteLine($"?? Pending migrations: {pending.Count}");

    if (pending.Any())
    {
        Console.WriteLine("?? Running database migrations...");
        db.Database.Migrate();
        Console.WriteLine("? Migration completed.");
    }
    else
    {
        Console.WriteLine("? No pending migrations.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFE");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<TestHub>("/testHub");

app.Run();
