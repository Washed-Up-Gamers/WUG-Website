﻿using System.ComponentModel.DataAnnotations;

namespace WUG.Database.Models.Misc;

public class CurrentTime
{
    [Key]
    public long Id { get; set; } = 100;

    public DateTime Time { get; set; }
}