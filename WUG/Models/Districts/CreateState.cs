using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace WUG.Models.Nations;
public class CreateStateModel {
    public long NationId { get; set; }

    [Display(Name = "Name")]
    [Required]
    public string Name { get; set; }

    [Display(Name = "Description")]
    [Required]
    public string Description { get; set; }

    [Display(Name = "Color on map", Description = "The color to be used when displaying this state on the Nation map. Must be in hex format")]
    [Required]
    public string MapColor { get; set; }
}