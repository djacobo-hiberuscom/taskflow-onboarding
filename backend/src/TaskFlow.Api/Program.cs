using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();




//Database
var connectionString = builder.Configuration.GetConnectionString("TaskFlowDb");

if(connectionString == null)
{
    throw new InvalidOperationException("Falta la conexión con TaskFlowDb.");
}

builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks().AddDbContextCheck<TaskFlowDbContext>();




//Build app
var app = builder.Build();


//Problem details
app.UseExceptionHandler();
app.UseStatusCodePages();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

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
});

app.Run();
