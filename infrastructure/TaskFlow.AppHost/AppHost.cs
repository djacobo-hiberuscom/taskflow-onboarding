var builder = DistributedApplication.CreateBuilder(args);using YamlDotNet.Serialization;

var pgPassword = builder.AddParameter("pgPassword", secret: true);
var appPassword = builder.AddParameter("appPassword", secret: true);
var migratorPassword = builder.AddParameter("migratorPassword", secret: true);
var keycloakDbPassword = builder.AddParameter("keycloakDbPassword", secret: true);


//Base de datos
var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5432)
    .WithDataVolume()
    .WithPgAdmin();

var taskFlowDb = postgres.AddDatabase("TaskFlowDb");
var keycloakDb = postgres.AddDatabase("KeyCloakDb");

// KeyCloak
#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
   .WithoutHttpsCertificate()
   .WithPostgres(keycloakDb).WaitFor(keycloakDb);
#pragma warning restore ASPIRECERTIFICATES001

// BACK
var back = builder.AddProject<Projects.TaskFlow_Api>("api", launchProfileName: "http")
    .WithReference(taskFlowDb).WaitFor(taskFlowDb)
    .WithReference(keycloakDb).WaitFor(keycloakDb)
    .WithExternalHttpEndpoints();

// FRONT
var front = builder.AddViteApp("front", "../../front")
    .WithReference(back).WaitFor(back)
    .WithEndpoint("http", e => e.Port = 5173);

builder.Build().Run();
