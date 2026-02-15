using CloudFileManager.Presentation.Website;

var builder = WebApplication.CreateBuilder(args);
DependencyRegister.Register(builder);

var app = builder.Build();

// 設定 HTTP request pipeline。
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // 預設 HSTS 為 30 天；正式環境可依需求調整。
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
