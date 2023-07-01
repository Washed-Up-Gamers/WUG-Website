using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WUG.Database.Models.OAuth2;

public class OauthApp
{
    [Key]
    public long Id {get; set; }
    public string Secret { get; set; }

    public long OwnerId { get; set; }
    public int Uses { get; set; }
    public string Name { get; set; }
    public string Image_Url { get; set; }
}