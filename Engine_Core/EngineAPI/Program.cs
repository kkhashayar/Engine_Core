

using Engine_API.Enumes;
using Engine_API.Interfaces;
using Engine_API.Services;
using Microsoft.OpenApi.Any;

var builder = WebApplication.CreateBuilder(args);

// Adding controllers 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen( option =>
{
    option.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Engine Api", Version = "v1" });

    // To show enumes values
    option.MapType<CECPCommands>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames(typeof(CECPCommands))
                   .Select(name => new OpenApiString(name))
                   .Cast<IOpenApiAny>()
                   .ToList()    
    });     
});


// Register engine background service //
builder.Services.AddSingleton<EngineHostService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EngineHostService>());

// Register engine Service 
builder.Services.AddTransient<IEngineService, EngineService>(); 



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();



app.Run();
