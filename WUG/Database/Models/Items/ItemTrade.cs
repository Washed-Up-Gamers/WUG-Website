using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WUG.Database.Models.Entities;
using WUG.Workers;
using WUG.Database.Models.Items;
using WUG.Web;
using WUG.Managers;

public enum ItemTradeType 
{
    Normal = 0,
    Server = 1
}

public class ItemTrade
{
    [Key]
    public long Id {get; set; }
    public double Amount { get; set; }
    
    [NotMapped]
    public ItemDefinition Definition => DBCache.Get<ItemDefinition>(DefinitionId)!;
    public long DefinitionId { get; set; }

    public DateTime Time { get; set; }

    public long? FromId { get; set; }

    public long ToId { get; set; }

    [VarChar(1024)]
    public string Details { get; set; }

    public ItemTradeType TradeType { get; set; }

    [NotMapped]

    public bool IsCompleted = false;

    [NotMapped]

    public TaskResult? Result = null;

    [NotMapped]

    public bool Force = false;

    public ItemTrade()
    {
        
    }

    public ItemTrade(ItemTradeType tradetype, long? fromId, long toId, double amount, long definitionid, string details)
    {
        Id = IdManagers.GeneralIdGenerator.Generate();
        Amount = amount;
        FromId = fromId;
        ToId = toId;
        Time = DateTime.UtcNow;
        DefinitionId = definitionid;
        Details = details;
        TradeType = tradetype;
    }

    public async Task<TaskResult> Execute(bool force = false)
    {
        Force = force;
        ItemTradeManager.itemTradeQueue.Enqueue(this);

        while (!IsCompleted) await Task.Delay(5);

        return Result!;
    }

    public void NonAsyncExecute(bool force = false)
    {
        Force = force;
        ItemTradeManager.itemTradeQueue.Enqueue(this);
    }

    public async Task<TaskResult> ExecuteFromManager(WashedUpDB dbctx, bool Force = false)
    {
        if (!Force && Amount < 0)
        {
            return new TaskResult(false, "Amount must be positive.");
        }
        if (Amount == 0)
        {
            return new TaskResult(false, "Amount must be above 0");
        }

        BaseEntity? fromEntity = BaseEntity.Find(FromId);
        BaseEntity? toEntity = BaseEntity.Find(ToId);

        if (fromEntity == null && TradeType != ItemTradeType.Server) { return new TaskResult(false, $"Failed to find sender {FromId}."); }
        if (toEntity == null) { return new TaskResult(false, $"Failed to find reciever {ToId}."); }

        SVItemOwnership? fromitem = DBCache.GetAll<SVItemOwnership>().FirstOrDefault(x => x.OwnerId == FromId && x.DefinitionId == DefinitionId);
        SVItemOwnership? toitem = DBCache.GetAll<SVItemOwnership>().FirstOrDefault(x => x.OwnerId == ToId && x.DefinitionId == DefinitionId);

        if (fromitem is null && TradeType != ItemTradeType.Server) {
            return new TaskResult(false, $"{fromEntity.Name} lacks any {Definition.Name} to give {Amount} to {toEntity.Name}");
        }

        if (!Force && fromitem.Amount < Amount)
        {
            return new TaskResult(false, $"{fromEntity.Name} lacks the enough of {Definition.Name} to give {Amount} to {toEntity.Name}");
        }

        // check if the entity we are sending already has this TradeItem        
        // if null then create one

        if (toitem is null) 
        {
            toitem = new SVItemOwnership() {
                Id = IdManagers.GeneralIdGenerator.Generate(),
                OwnerId = toEntity.Id,
                DefinitionId = DefinitionId,
                Amount = 0
            };
            DBCache.AddNew(toitem.Id, toitem);
            //DBCache.dbctx.SVItemOwnerships.Add(toitem);
            toEntity.SVItemsOwnerships[toitem.DefinitionId] = toitem;
        }

        // do tariffs

        if (!(TradeType == ItemTradeType.Server) && GameDataManager.Resources.ContainsKey(toitem.Definition.Name) && toitem.Definition.IsSVItem)
        {
            TaxPolicy? FromNationTaxPolicy = DBCache.GetAll<TaxPolicy>().FirstOrDefault(x => x.NationId == fromEntity.NationId && x.Target == toitem.Definition.Name && (x.taxType == TaxType.ImportTariff || x.taxType == TaxType.ExportTariff));
            TaxPolicy? ToNationTaxPolicy = DBCache.GetAll<TaxPolicy>().FirstOrDefault(x => x.NationId == toEntity.NationId && x.Target == toitem.Definition.Name && (x.taxType == TaxType.ImportTariff || x.taxType == TaxType.ExportTariff));

            // fun fact, the entity IRL that imports or exports pays the tariff

            if (FromNationTaxPolicy is not null) {
                decimal taxamount = FromNationTaxPolicy.GetTaxAmountForResource((decimal)Amount);
                string detail = $"Tax payment for item id: {Id}, Tax Id: {FromNationTaxPolicy.Id}, Tax Type: {FromNationTaxPolicy.taxType}";
                var tran = new Transaction(fromEntity, BaseEntity.Find(FromNationTaxPolicy!.NationId!), taxamount, TransactionType.TaxPayment, detail);
                tran.NonAsyncExecute(true);
            }
            if (ToNationTaxPolicy is not null) {
                decimal taxamount = ToNationTaxPolicy.GetTaxAmountForResource((decimal)Amount);
                string detail = $"Tax payment for item trade id: {Id}, Tax Id: {ToNationTaxPolicy.Id}, Tax Type: {ToNationTaxPolicy.taxType}";
                var tran = new Transaction(toEntity, BaseEntity.Find(ToNationTaxPolicy!.NationId!), taxamount, TransactionType.TaxPayment, detail);
                tran.NonAsyncExecute(true);
            }
        }
        
        toitem.Amount += Amount;
        if (FromId is not null)
            fromitem.Amount -= Amount;

        dbctx.ItemTrades.Add(this);

        return new TaskResult(true, $"Successfully gave {Amount} of {toitem.Definition.Name} to {toEntity!.Name}.");
    }
}