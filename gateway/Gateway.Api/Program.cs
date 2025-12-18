using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// CORS для веб-интерфейса
builder.Services.AddCors(o =>
{
    o.AddPolicy("ui", p =>
        p.WithOrigins("http://localhost:8090")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// маршрутизация
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(t =>
    {
        // сохранить заголовок пользователя
        t.AddRequestTransform(_ => ValueTask.CompletedTask);
    });

var app = builder.Build();

app.UseCors("ui");

app.MapGet("/", () => Results.Text("gozon gateway alive"));
app.MapReverseProxy();

app.Run();