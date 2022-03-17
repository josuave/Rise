namespace Rise.Blazor.App.Dashboard
{
    public class Panel
    {
        public string Title { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();

    }
}