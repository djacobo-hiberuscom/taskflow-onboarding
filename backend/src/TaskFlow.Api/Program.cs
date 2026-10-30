using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Projects;
using TaskFlow.Infrastructure.Persistence.Repositories;
using TaskFlow.Api.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddTaskFlowAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader().AllowAnyMethod()));




//Database
var connectionString = builder.Configuration.GetConnectionString("TaskFlowDb");

if(connectionString == null)
{
    throw new InvalidOperationException("Falta la conexión con TaskFlowDb.");
}

builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<TaskFlowDbContext>());

builder.Services.AddScoped<ProjectService>();

builder.Services.AddHealthChecks().AddDbContextCheck<TaskFlowDbContext>();




//Build app
var app = builder.Build();


//Problem details
app.UseExceptionHandler();
app.UseStatusCodePages();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LocalUserMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        if (context.Response.StatusCode ==
            StatusCodes.Status503ServiceUnavailable)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Servicio no disponible."
            ).ExecuteAsync(context);
        }
        else
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(report.Status.ToString());
        }
    }
}).AllowAnonymous();

app.Run();
