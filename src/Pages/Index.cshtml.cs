using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RecipeCollector.Pages
{
    public class IndexModel : PageModel
    {
        public Recipe[] Recipes { get; private set; } = Array.Empty<Recipe>();

        public async Task OnGet()
        {
            var files = Conesoft.Hosting.Host.LocalStorage.Filtered("*.recipe.json", allDirectories: false);

            var recipes = await Task.WhenAll(files.Select(async file => (await file.ReadFromJson<Recipe>(new System.Text.Json.JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            }))!));

            Recipes = recipes.OrderByDescending(r => r.MadeAt.Any() ? r.MadeAt.Max() : DateTime.MinValue).ToArray();
        }
    }
}