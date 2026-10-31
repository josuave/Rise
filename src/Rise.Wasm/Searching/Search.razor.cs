using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Rise.Wasm.Searching
{
    public partial class Search
    {
        [Inject]
        private IJSRuntime? JS { get; set; }

        public string? SearchString { get; set; } = string.Empty;

        public async Task SearchOnEnter(KeyboardEventArgs args)
        {
            if (args.Code == "Enter" || args.Code == "NumpadEnter")
            {
                await RunSearch();
            }
        }

        public async Task RunSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchString) || JS is null)
            {
                return;
            }

            var url = $"https://www.google.com/search?q={Uri.EscapeDataString(SearchString)}";
            try
            {
                await JS.InvokeVoidAsync("open", url, "_blank");
            }
            catch (JSException)
            {
                // Opening the search tab failed (e.g. popup blocked); there is nothing useful to show for a start-page search box, so swallow it.
            }
        }
    }
}
