using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WUG.Database.Models.Notifications;

[Index(nameof(UserId))]
public class Notification
{
    [Key]
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string? Button1Text { get; set; }
    public string? Button1Link { get; set; }
    public string? Button2Text { get; set; }
    public string? Button2Link { get; set; }
    public string? Button3Text { get; set; }
    public string? Button3Link { get; set; }
    public DateTime TimeSent { get; set; }
    public bool Seen { get; set; }
}