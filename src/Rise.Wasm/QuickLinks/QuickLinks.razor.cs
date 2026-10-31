using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Rise.Domain;

namespace Rise.Wasm.QuickLinks
{
    public partial class QuickLinks
    {
        private const string StorageKey = "rise.quicklinks.v1";

        [Inject]
        private IJSRuntime? JS { get; set; }

        private List<QuickLink> Links { get; set; } = Defaults();

        private bool Editing { get; set; }
        private string NewName { get; set; } = string.Empty;
        private string NewUrl { get; set; } = string.Empty;
        private string? FormError { get; set; }

        private static List<QuickLink> Defaults() => new()
        {
            new QuickLink("Google", "https://www.google.com"),
            new QuickLink("GitHub", "https://github.com"),
            new QuickLink("YouTube", "https://www.youtube.com"),
            new QuickLink("Wikipedia", "https://www.wikipedia.org"),
        };

        protected override async Task OnInitializedAsync()
        {
            if (JS is null)
            {
                return;
            }

            try
            {
                var stored = await JS.InvokeAsync<string?>("riseStorage.get", StorageKey);
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    var parsed = JsonSerializer.Deserialize<List<QuickLink>>(stored);
                    if (parsed is not null)
                    {
                        Links = parsed;
                    }
                }
            }
            catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
            {
                // Corrupt or unavailable storage: fall back to the defaults rather than breaking the page.
                Links = Defaults();
            }
        }

        private async Task AddAsync()
        {
            FormError = null;
            var name = NewName.Trim();
            var url = NewUrl.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                FormError = "Give the link a name and a URL.";
                return;
            }

            if (!url.Contains("://", StringComparison.Ordinal))
            {
                url = "https://" + url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                FormError = "That doesn't look like a valid web address.";
                return;
            }

            Links.Add(new QuickLink(name, uri.ToString()));
            NewName = string.Empty;
            NewUrl = string.Empty;
            await SaveAsync();
        }

        private async Task RemoveAsync(QuickLink link)
        {
            Links.Remove(link);
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            if (JS is null)
            {
                return;
            }

            try
            {
                await JS.InvokeVoidAsync("riseStorage.set", StorageKey, JsonSerializer.Serialize(Links));
            }
            catch (JSException)
            {
                // Storage unavailable (e.g. private mode): the page still works for this session.
            }
        }

        private static string Initials(string name)
        {
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return "?";
            }
            if (words.Length == 1)
            {
                return words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant();
            }
            return string.Concat(words[0][..1], words[1][..1]).ToUpperInvariant();
        }
    }
}
