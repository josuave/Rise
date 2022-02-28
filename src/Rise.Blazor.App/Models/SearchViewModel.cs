using System.ComponentModel.DataAnnotations;

namespace Rise.Blazor.App.Models
{
    public class SearchViewModel
    {
        [MinLength(length: 1)]
        [MaxLength(length: 1024)]
        [Required(AllowEmptyStrings = false)]
        public string? Query { get; set; }
    }
}
