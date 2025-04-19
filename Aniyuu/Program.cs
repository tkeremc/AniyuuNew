using Aniyuu.Authentication;
using Aniyuu.Middlewares;
using Aniyuu.Services;

var builder = WebApplication.CreateBuilder(args);
var logger = NLog.Web.NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
ConfigureServices(builder);

var app = builder.Build();

ConfigureApp(app);
logger.Info("Application started");

app.Run();

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsApi",
            policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });
    
    
    ServiceCaller.RegisterServices(builder.Services);
}

void ConfigureApp(WebApplication app)
{
    app.UseSwagger(); 
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Aniyuu API V2");
    });
    
    app.UseMiddleware<CountryRestrictionMiddleware>();
    app.UseMiddleware<TokenAuthenticationHandler>();
    app.UseMiddleware<RequestLogMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();
    
    
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    
    app.UseCors("CorsApi");
}