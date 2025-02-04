

using Engine_API.Interfaces;
using Engine_API.Services;

var builder = WebApplication.CreateBuilder(args);



// Adding controllers 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register engine background service 
builder.Services.AddTransient<EngineHostService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EngineHostService>());

// Register engine Service 
builder.Services.AddScoped<IEngineService, EngineService>(); 



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
