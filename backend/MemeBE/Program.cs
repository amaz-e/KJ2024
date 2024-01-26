using MemeBE.hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:63342") // URL innego serwera
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Ważne dla SignalR
    });
});
builder.Services.AddSignalR();

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
app.UseRouting();
app.UseCors("MyCorsPolicy");
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<TestHub>("/TestHub");
});

app.Run();
