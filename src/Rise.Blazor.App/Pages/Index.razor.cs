using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Rise.Blazor.App.Pages
{
    public partial class Index
    {
        [Inject]
        private IJSRuntime? JSRuntime { get; set; }

        private async Task Search_OnSubmit(string query)
        {
            _ = JSRuntime ?? throw new NullReferenceException("JSRuntime cannot be null.");

            var encodedQuery = System.Web.HttpUtility.UrlEncode(query);
            await JSRuntime.InvokeAsync<string>("open", "https://google.com/search?q=" + encodedQuery, "_blank");
        }
    }
}