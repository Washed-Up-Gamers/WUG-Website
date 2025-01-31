﻿
namespace WUG.Mappings.Entities;

public static class BaseEntityMapper
{
    public static BaseEntity ToModel(this Shared.Models.Entities.BaseEntity entity)
    {
        if (entity is null)
            return null;

        return null;
    }

    public static Shared.Models.Entities.BaseEntity ToDatabase(this BaseEntity entity)
    {
        if (entity is null)
            return null;

        var newentity =  new Shared.Models.Entities.Entity()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Credits = 0.0m,//entity.Credits,
            TaxAbleBalance = entity.TaxAbleBalance,
            ApiKey = entity.ApiKey,
            ImageUrl = entity.ImageUrl,
            NationId = entity.NationId,
            EntityType = (Shared.Models.Entities.EntityType)entity.EntityType
        };

        foreach (var key in entity.SVItemsOwnerships)
        {

        }

        return newentity;
    }
}