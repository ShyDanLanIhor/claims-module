var builder = DistributedApplication.CreateBuilder(args);

// SQL Server (containerised) with a persistent data volume so data survives restarts.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume();

var claimsDb = sql.AddDatabase("claimsdb");

// Claims Management API — references the database (injects ConnectionStrings:claimsdb) and waits
// for it to be ready before starting. The API applies EF migrations on startup in Development.
var api = builder.AddProject<Projects.ClaimsModule_API>("claims-api")
    .WithReference(claimsDb)
    .WaitFor(claimsDb);

// Angular frontend dev server (ng serve on :4200). WithReference(api) injects the API's address so
// the dev-server proxy (proxy.conf.js) can forward /api to it.
builder.AddNpmApp("claims-web", "../../clients/web", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
