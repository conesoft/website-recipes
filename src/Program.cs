using Conesoft.Files;
using Conesoft.Users;
using System.Text.Json;
using System.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddUsers("chocolate-chip", (Conesoft.Hosting.Host.GlobalStorage / "Users").Path);

var app = builder.Build();

app.UseStaticFiles();

app.MapUsers();

app.MapRazorPages();

app.MapGet("/image/{title}", async (string title) =>
{
    var _ = HttpUtility.UrlDecode(title);
    var image = Conesoft.Hosting.Host.LocalStorage.Filtered(HttpUtility.UrlDecode(title) + ".*", allDirectories: false).FirstOrDefault(f => f.Extension != ".json");
    return image != null
        ? Results.File((await image.ReadBytes())!, "image/" + image.Extension)
        : Results.NotFound();
});

app.MapPost("/addrecipe", async (HttpContext context) =>
{
    var editors = await (Conesoft.Hosting.Host.LocalStorage / Filename.From("editors", "json")).ReadFromJson<string[]>(null);
    if (editors?.Contains(context?.User?.Identity?.Name) ?? false)
    {
        var form = await context!.Request.ReadFormAsync();
        var url = form["recipe-url"].First();
        var addedby = form["user"].First();

        string? image = null;
        string? title = null;
        try
        {
            var content = (await (await new HttpClient().GetAsync(url)).Content.ReadAsStringAsync());
            image = content.Split("\"og:image\" content=\"")[1].Split("\"")[0];
            title = content.Split("<title>")[1].Split("</")[0].Split(" - ").First().Trim();

            foreach(var c in System.IO.Path.GetInvalidFileNameChars())
            {
                title = title.Replace(c, '-');
            }

            title = title.Replace("-", " - ");
            title = title.Replace("  ", " ");

            var imageFile = Conesoft.Hosting.Host.LocalStorage / Conesoft.Files.Filename.From(title, image.Split(".").Last());

            await imageFile.WriteBytes(await new HttpClient().GetByteArrayAsync(image));
        }
        catch
        {
            title = url.Split('/').Last();
        }

        Recipe recipe = new(title, url, addedby, image, Array.Empty<DateTime>());

        var file = Conesoft.Hosting.Host.LocalStorage / Filename.From(title, "recipe.json");
        await file.WriteAsJson(recipe, new JsonSerializerOptions() { WriteIndented = true });

        return Results.LocalRedirect("/");
    }
    return Results.Unauthorized();
});

app.MapPost("/addtimestamp", async (HttpContext context) =>
{
    var editors = await (Conesoft.Hosting.Host.LocalStorage / Filename.From("editors", "json")).ReadFromJson<string[]>(null);
    if (editors?.Contains(context?.User?.Identity?.Name) ?? false)
    {
        var form = await context!.Request.ReadFormAsync();
        var title = form["recipe-title"].First();
        var when = int.Parse(form["days-ago"].First());

        var file = Conesoft.Hosting.Host.LocalStorage / Filename.From(title, "recipe.json");

        var recipe = (await file.ReadFromJson<Recipe>())!;

        await file.WriteAsJson(recipe with
        {
            MadeAt = recipe.MadeAt.Append(DateTime.Today - TimeSpan.FromDays(when)).ToArray()
        }, new JsonSerializerOptions() { WriteIndented = true });

        return Results.LocalRedirect("/");
    }
    return Results.Unauthorized();
});

app.Run();