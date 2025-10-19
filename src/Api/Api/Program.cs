using Product.Template.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationCore();
builder.Services.AddUseCases();
builder.Services.AddAppConnections(builder.Configuration);
builder.Services.AddControllersConfigurations();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseDocumentation();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
