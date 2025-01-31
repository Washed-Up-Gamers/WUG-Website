using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WUG.Models.Manage;

public class UserManageModel
{
    public long Id { get; set; }
    public string Name { get; set; }

    [NotMapped]
    [JsonIgnore]
    public User user { get; set; }
}