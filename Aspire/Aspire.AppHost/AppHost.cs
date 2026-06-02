using Aspire.Hosting.JavaScript;

var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.MinimalApiJwt>("weatherapi");

builder.AddProject<Projects.BlazorWebAppEntra>("blazorfrontend")
    .WithReference(weatherApi);

var frontend = builder.AddJavaScriptApp("WebAppClient", "../../WebAppClient")
    .WithHttpEndpoint(env: "PORT") // Passes the Aspire-allocated port to Next.js
    .WithExternalHttpEndpoints()   // Allows you to open it in your browser
    .WaitFor(weatherApi)
    .WithReference(weatherApi);    // Injects the backend's environment variables

builder.Build().Run();
