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

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("GRADING_DB_CONNECTION");
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});


var app = builder.Build();

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
