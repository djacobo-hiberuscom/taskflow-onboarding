var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TaskFlow_Api>("api");

builder.Build().Run();
