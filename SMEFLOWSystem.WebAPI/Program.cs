using SMEFLOWSystem.Application.Extensions;
using SMEFLOWSystem.Infrastructure.Extensions;
using SMEFLOWSystem.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebApi(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseWebApi();
app.Run();
