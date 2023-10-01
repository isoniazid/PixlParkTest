using Registrator;


var builder = WebApplication.CreateBuilder(args);

/* Starter.LoadConfigs(builder); */

Starter.RegisterServices(builder);

var app = builder.Build();

Starter.Configure(app);

Starter.DefineEndpoints(app);

app.Run();
