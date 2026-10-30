namespace TaskFlow.Api.Authentication;

public sealed class KeycloakOptions
{
    public string Authority { get; set; } = "";
    public string Audience { get; set; } = "taskflow-api";
    public IntrospectionOptions Introspection { get; set; } = new();

    public sealed class IntrospectionOptions
    {
        public string ClientId { get; set; } = "taskflow-api";
        public string ClientSecret { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 5;
    }
}
