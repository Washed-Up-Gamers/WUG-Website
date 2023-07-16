using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WUG.Database.Models.Entities;
using WUG.Workers;
using WUG.Web;

namespace WUG.Database.Models.Economy;

public enum TransactionType
{
    Loan = 1,
    // also includes trading resources
    ItemTrade = 2,
    Paycheck = 3,

    [Obsolete]
    StockTrade = 4,
    // use this when the transaction does not fit the other types
    Payment = 5,
    // only issued by governmental bodies
    TaxCreditPayment = 6,
    TaxPayment = 7,
    FreeMoney = 8,
    LoanRepayment = 9,
    DividendPayment = 10,
    StockSale = 11,
    StockBrought = 12,
    ResourceSale = 13,
    ResourceBrought = 14,
    UBI = 15,
    NonTaxedOther = 16,
    IPO = 17,
    PowerPlantProducerPayment = 18,
    PowerPlantConsumerPayment = 19
}

public class Transaction
{
    [Key]
    public long Id {get; set; }
    public decimal Amount { get; set; }

    public DateTime Time { get; set; }

    public long FromId { get; set; }

    public long ToId { get; set; }
    public TransactionType transactionType { get; set; }

    [VarChar(1024)]
    public string Details { get; set; }
    public bool? IsAnExpense {get; set; }

    [NotMapped]

    public bool IsCompleted = false;

    [NotMapped]

    public TaskResult? Result = null;

    [NotMapped]

    public bool Force = false;

    [NotMapped]
    public BaseEntity FromEntity { get; set; }

    [NotMapped]
    public BaseEntity ToEntity { get; set; }

    public Transaction()
    {
        
    }

    public Transaction(BaseEntity fromEntity, BaseEntity toEntity, decimal credits, TransactionType TransactionType, string details)
    {
        Id = IdManagers.GeneralIdGenerator.Generate();
        Amount = credits;
        FromId = fromEntity.Id;
        ToId = toEntity.Id;
        Time = DateTime.UtcNow;
        transactionType = TransactionType;
        Details = details;
        FromEntity = fromEntity;
        ToEntity = toEntity;
    }

    public async Task<TaskResult> Execute(bool force = false)
    {
        Force = force;
        TransactionManager.transactionQueue.Enqueue(this);

        while (!IsCompleted) await Task.Delay(10);

        return Result!;
    }

    public void NonAsyncExecute(bool force = false)
    {
        Force = force;
        TransactionManager.transactionQueue.Enqueue(this);
    }

    public async Task<TaskResult> ExecuteFromManager(WashedUpDB dbctx, bool Force = false)
    {
        // general checking stuff
            
        if (!Force && Amount < 0)
            return new TaskResult(false, "Transaction must be positive.");
        if (Amount == 0)
            return new TaskResult(false, "Transaction must have a value.");
        if (FromId == ToId)
            return new TaskResult(false, $"An entity cannot send credits to itself.");

        if (!Force && (FromEntity.Money) < Amount)
            return new TaskResult(false, $"{FromEntity.Name} cannot afford to send ¢{Amount}");

        decimal totaltaxpaid = 0.0m;

        if (transactionType != TransactionType.TaxPayment && transactionType != TransactionType.Loan)
        {

            List<TaxPolicy> policies = DBCache.GetAll<TaxPolicy>().Where(x => x.NationId == null || x.NationId == FromEntity.NationId || x.NationId == ToEntity.NationId).ToList();

            // must do TAXES (don't let Etho see this)

            foreach (TaxPolicy policy in policies)
            {
                if (policy.Rate <= 0.01m)
                {
                    continue;
                }
                decimal amount = 0.0m;
                switch (policy.taxType)
                {
                    case TaxType.Transactional:
                        amount = policy.GetTaxAmount(Amount);
                        break;
                    case TaxType.Sales:
                        if (transactionType == TransactionType.ItemTrade) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.StockSale:
                        if (transactionType == TransactionType.StockSale) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.StockBought:
                        if (transactionType == TransactionType.StockBrought) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.ResourceSale:
                        if (transactionType == TransactionType.ResourceSale)
                        {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.ResourceBrought:
                        if (transactionType == TransactionType.ResourceBrought)
                        {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.Payroll:
                        if (transactionType == TransactionType.Paycheck) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                }
                // continue if transaction did not match the tax policy
                if (amount == 0.0m)
                {
                    continue;
                }
                if (policy.NationId == 100)
                {
                    long _FromId = FromId;
                    if (policy.taxType == TaxType.Sales || policy.taxType == TaxType.Transactional || policy.taxType == TaxType.Payroll)
                    {
                        _FromId = ToId;
                    }
                    Transaction taxtrans = new Transaction(FromEntity, BaseEntity.Find(100), amount, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType.ToString()}");
                    policy.Collected += amount;
                    totaltaxpaid += amount;
                    taxtrans.NonAsyncExecute(true);
                }
                else
                {
                    if (policy.NationId == FromEntity.NationId && policy.taxType != TaxType.Sales && policy.taxType != TaxType.Payroll && policy.taxType != TaxType.Transactional)
                    {
                        Transaction taxtrans = new Transaction(FromEntity, BaseEntity.Find(policy.NationId), amount, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType.ToString()}");
                        policy.Collected += amount;
                        totaltaxpaid += amount;
                        taxtrans.NonAsyncExecute(true);
                    }
                    else if (policy.NationId == ToEntity.NationId)
                    {
                        Transaction taxtrans = new Transaction(ToEntity, BaseEntity.Find(policy.NationId), amount, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType.ToString()}");
                        policy.Collected += amount;
                        totaltaxpaid += amount;
                        taxtrans.NonAsyncExecute(true);
                    }
                }
            }
        }

        FromEntity.Money -= Amount;
        ToEntity.Money += Amount;

        if (transactionType is TransactionType.Paycheck) 
        {
            FromEntity.TaxAbleBalance -= Amount;
            ToEntity.TaxAbleBalance += Amount;
            ToEntity.IncomeToday += Amount;
        }

        if (transactionType is TransactionType.PowerPlantProducerPayment) {
            ToEntity.IncomeToday += Amount;
            ToEntity.TaxAbleBalance += Amount;
        }

        if (transactionType is TransactionType.PowerPlantConsumerPayment) {
            FromEntity.TaxAbleBalance -= Amount;
        }

        if (transactionType is TransactionType.DividendPayment
            or TransactionType.ItemTrade
            or TransactionType.Payment
            or TransactionType.StockSale
            or TransactionType.ResourceSale)
        {
            if (IsAnExpense is not null and true)
                FromEntity.TaxAbleBalance -= Amount;
            ToEntity.TaxAbleBalance += Amount;
            ToEntity.IncomeToday += Amount;
        }

        else if (transactionType == TransactionType.ResourceBrought)
        {
            if (IsAnExpense is not null and true)
                FromEntity.TaxAbleBalance -= Amount;
            ToEntity.TaxAbleBalance += Amount;
            ToEntity.IncomeToday += Amount;
        }

        else if (transactionType == TransactionType.StockBrought)
        {
            ToEntity.TaxAbleBalance += Amount;
        }

        else if (transactionType == TransactionType.TaxPayment) {
            // we do this so that Nations, states, and province groups can have a "profit" for banks and loan brokers to use.
            ToEntity.TaxAbleBalance += Amount;
            ToEntity.IncomeToday += Amount;
        }

        dbctx.Transactions.Add(this);

        return new TaskResult(true, $"Successfully sent ¢{Amount} to {ToEntity.Name} with ¢{totaltaxpaid} tax.");
    }

    public async Task<TaskResult> OldExecuteFromManager(WashedUpDB dbctx, bool Force = false)
    {

        while (TransactionManager.ActiveSvids.Contains(FromId) || TransactionManager.ActiveSvids.Contains(ToId))
        {
            await Task.Delay(1);
        }

        if (!Force && Amount < 0)
        {
            return new TaskResult(false, "Transaction must be positive.");
        }
        if (Amount == 0)
        {
            return new TaskResult(false, "Transaction must have a value.");
        }
        if (FromId == ToId)
        {
            return new TaskResult(false, $"An entity cannot send credits to itself.");
        }

        BaseEntity? fromEntity = BaseEntity.Find(FromId);
        BaseEntity? toEntity = BaseEntity.Find(ToId);

        if (fromEntity == null) { return new TaskResult(false, $"Failed to find sender {FromId}."); }
        if (toEntity == null) { return new TaskResult(false, $"Failed to find reciever {ToId}."); }

        TransactionManager.ActiveSvids.Add(FromId);
        TransactionManager.ActiveSvids.Add(ToId);

        if (!Force && fromEntity.Money < Amount)
        {
            TransactionManager.ActiveSvids.Remove(FromId);
            TransactionManager.ActiveSvids.Remove(ToId);
            return new TaskResult(false, $"{fromEntity.Name} cannot afford to send ¢{Amount}");
        }

        decimal totaltaxpaid = 0.0m;

        if (transactionType != TransactionType.TaxPayment && transactionType != TransactionType.Loan) {

            List<TaxPolicy> policies = DBCache.GetAll<TaxPolicy>().Where(x => x.NationId == 100 || x.NationId == fromEntity.NationId || x.NationId == toEntity.NationId).ToList();

            // must do TAXES (don't let Etho see this)

            foreach (TaxPolicy policy in policies)
            {
                if (policy.Rate <= 0.01m)
                {
                    continue;
                }
                decimal amount = 0.0m;
                switch (policy.taxType)
                {
                    case TaxType.Transactional:
                        amount = policy.GetTaxAmount(Amount);
                        break;
                    case TaxType.Sales:
                        if (transactionType == TransactionType.ItemTrade) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.StockSale:
                        if (transactionType == TransactionType.StockSale) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.StockBought:
                        if (transactionType == TransactionType.StockBrought) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.ResourceSale:
                        if (transactionType == TransactionType.ResourceSale)
                        {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.ResourceBrought:
                        if (transactionType == TransactionType.ResourceBrought)
                        {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                    case TaxType.Payroll:
                        if (transactionType == TransactionType.Paycheck) {
                            amount = policy.GetTaxAmount(Amount);
                        }
                        break;
                }
                // continue if transaction did not match the tax policy
                if (amount == 0.0m) {
                    continue;
                }
                if (policy.NationId == 100) {
                    long _FromId = FromId;
                    if (policy.taxType == TaxType.Sales || policy.taxType == TaxType.Transactional || policy.taxType == TaxType.Payroll) {
                        _FromId = ToId;
                    }
                    Transaction taxtrans = new Transaction(FromEntity, BaseEntity.Find(100), amount, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType.ToString()}");
                    policy.Collected += amount;
                    totaltaxpaid += amount;
                    taxtrans.NonAsyncExecute(true);
                }
                else {
                    if (policy.NationId == fromEntity.NationId && policy.taxType != TaxType.Sales && policy.taxType != TaxType.Payroll && policy.taxType != TaxType.Transactional) {
                        Transaction taxtrans = new Transaction(FromEntity, BaseEntity.Find(policy.NationId), amount, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType.ToString()}");
                        policy.Collected += amount;
                        totaltaxpaid += amount;
                        taxtrans.NonAsyncExecute(true);
                    }
                    else if (policy.NationId == ToEntity.NationId) {
                        Transaction taxtrans = new Transaction(ToEntity, BaseEntity.Find(policy.NationId), amount, TransactionType.TaxPayment, $"Tax payment for transaction id: {Id}, Tax Id: {policy.Id}, Tax Type: {policy.taxType.ToString()}");
                        policy.Collected += amount;
                        totaltaxpaid += amount;
                        taxtrans.NonAsyncExecute(true);
                    }
                }
            }
        }

        //fromEntity.Credits -= Credits;
        //toEntity.Credits += Credits;

        if (transactionType is TransactionType.Paycheck) 
        {
            fromEntity.TaxAbleBalance -= Amount;
            toEntity.TaxAbleBalance += Amount;
        }

        if (transactionType is TransactionType.DividendPayment
            or TransactionType.ItemTrade
            or TransactionType.Payment
            or TransactionType.StockSale
            or TransactionType.ResourceSale)
        {
            if (IsAnExpense is not null and true)
                fromEntity.TaxAbleBalance -= Amount;
            toEntity.TaxAbleBalance += Amount;
        }

        else if (transactionType == TransactionType.ResourceBrought)
        {
            if (IsAnExpense is not null and true)
                fromEntity.TaxAbleBalance -= Amount;
            toEntity.TaxAbleBalance += Amount;
        }

        else if (transactionType == TransactionType.StockBrought)
        {
            toEntity.TaxAbleBalance += Amount;
        }

        else if (transactionType == TransactionType.TaxPayment) {
            // we do this so that Nations, states, and province groups can have a "profit" for banks and loan brokers to use.
            toEntity.TaxAbleBalance += Amount;
        }

        dbctx.Transactions.Add(this);

        TransactionManager.ActiveSvids.Remove(FromId);
        TransactionManager.ActiveSvids.Remove(ToId);

        return new TaskResult(true, $"Successfully sent ¢{Amount} to {toEntity.Name} with ¢{totaltaxpaid} tax.");

    }
}