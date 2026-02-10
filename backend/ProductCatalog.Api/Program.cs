using ProductCatalog.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProductCatalog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ProductCatalog.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseMiddleware<ProductCatalog.Api.Middleware.RequestCorrelationMiddleware>();

app.UseCors("frontend");

app.UseHttpsRedirection();

await app.Services.SeedAsync();

app.MapControllers();

app.Run();
