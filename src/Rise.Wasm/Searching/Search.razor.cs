using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Rise.Wasm.Searching
{
    public partial class Search
    {
        [Inject]
        private NavigationManager? NavigationManager { get; set; }

        [Inject]
        private IJSRuntime? JS { get; set; }

        public string? SearchString { get; set; } = string.Empty;

        public async Task SearchOnEnter(KeyboardEventArgs args)
        {
            if ((args.Code == "Enter" || args.Code == "NumpadEnter") && SearchString is not null && JS is not null)
            {
                var url = $"https://www.google.com/search?q={SearchString.Replace(' ', '+')}";
                await JS.InvokeVoidAsync("open", url, "_blank");
            }
        }
    }
}