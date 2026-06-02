using Aspire.Hosting.JavaScript;

var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.MinimalApiJwt>("weatherapi");

var webAppBff = builder.AddProject<Projects.WebAppBff>("webappbff")
    .WithReference(weatherApi) // Adds a project reference to the Weather API, allowing it to be built and run together with the BFF
    .WaitFor(weatherApi);    // Ensures the BFF waits for the Weather API to be running before it starts

builder.AddProject<Projects.BlazorWebAppEntra>("blazorfrontend")
    .WithReference(weatherApi);

var frontend = builder.AddJavaScriptApp("WebAppClient", "../../WebAppClient")
    .WithHttpEndpoint(env: "PORT") // Passes the Aspire-allocated port to Next.js
    .WithExternalHttpEndpoints()   // Allows you to open it in your browser
    .WaitFor(webAppBff)
    .WithReference(webAppBff);    // Injects the backend's environment variables

builder.Build().Run();
