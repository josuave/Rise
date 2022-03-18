using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Rise.Infrastructure
{
    public partial class Component1 : IAsyncDisposable
    {
        [Inject]
        public IJSRuntime? JSRuntime { get; set; }

        private Lazy<Task<IJSObjectReference>>? moduleTask;

        public async Task SayHello()
        {
            if (moduleTask is null)
            {
                return;
            }

            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("sayHello");
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask is null)
            {
                return;
            }

            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                moduleTask = new(() => JSRuntime!.InvokeAsync<IJSObjectReference>(
                   "import", "./_content/Rise.Infrastructure/Component1.razor.js").AsTask());
            }

            return Task.CompletedTask;
        }

        protected async Task OnButtonClick(MouseEventArgs mouseEventArgs)
        {
            await SayHello();
        }
    }
}