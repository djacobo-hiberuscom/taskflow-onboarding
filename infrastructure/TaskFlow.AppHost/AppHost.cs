var builder = DistributedApplication.CreateBuilder(args);

//Base de datos
var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5432)
    .WithDataVolume()
    .WithPgAdmin();

var taskFlowDb = postgres.AddDatabase("TaskFlowDb");
var keycloakDb = postgres.AddDatabase("KeyCloakDb");

// KeyCloak

// Parametros secretos de keycloak
var kcApiSecret = builder.AddParameter("taskflow-api-client-secret", secret: true);
var demoPassword = builder.AddParameter("taskflow-demo-password", secret:true);

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
   .WithoutHttpsCertificate()
   .WithPostgres(keycloakDb).WaitFor(keycloakDb)
   .WithRealmImport("../keycloak/realm")
   .WithEnvironment("TASKFLOW_API_CLIENT_SECRET", kcApiSecret)
   .WithEnvironment("TASKFLOW_DEMO_PASSWORD", demoPassword);
#pragma warning restore ASPIRECERTIFICATES001

// BACK
var back = builder.AddProject<Projects.TaskFlow_Api>("api", launchProfileName: "http")
    .WithReference(taskFlowDb).WaitFor(taskFlowDb)
    .WithReference(keycloak).WaitFor(keycloak)
    .WithExternalHttpEndpoints();

// FRONT
const string KEYCLOAK_REALM = "taskflow"; // realm
const string KEYCLOAK_CLIENT_ID = "taskflow-spa"; // CLient id

var front = builder.AddViteApp("front", "../../front")
    .WithReference(back).WaitFor(back)
    .WithEnvironment("VITE_API_BASE_URL", back.GetEndpoint("http"))
    .WithEnvironment("VITE_KEYCLOAK_URL", keycloak.GetEndpoint("http"))
    .WithEnvironment("VITE_KEYCLOAK_REALM", KEYCLOAK_REALM)
    .WithEnvironment("VITE_KEYCLOAK_CLIENT_ID", KEYCLOAK_CLIENT_ID)
    .WithEndpoint("http", e => e.Port = 5173);

builder.Build().Run();
