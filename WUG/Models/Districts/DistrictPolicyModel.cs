using System.ComponentModel.DataAnnotations.Schema;


namespace WUG.Models.Nations;

public class NationPolicyModel
{
    public List<TaxPolicy> TaxPolicies { get; set; }
    public List<UBIPolicy> UBIPolicies { get; set; }

    public long NationId { get; set; }

    [NotMapped]
    public Nation Nation {
        get {
            return DBCache.Get<Nation>(NationId)!;
        }
    }

    public NationPolicyModel()
    {
        
    }

    public void AddUBIPolicy(Rank? rank, long NationId)
    {
        UBIPolicy pol = new();
        pol.NationId = NationId;
        if (rank is null) {
            pol.Anyone = true;
        }
        else {
            pol.ApplicableRank = rank;
        }
        pol.Id = IdManagers.GeneralIdGenerator.Generate();

        UBIPolicy? oldpol = DBCache.GetAll<UBIPolicy>().FirstOrDefault(x => x.NationId == NationId && x.ApplicableRank == rank);
        if (oldpol is not null) {
            pol.Rate = oldpol.Rate;
        }

        UBIPolicies.Add(pol);
    }

    public void AddTaxPolicy(long NationId, TaxType type, decimal min = 0.0m, decimal max = 99999999.0m)
    {
        TaxPolicy pol = new();
        pol.Id = IdManagers.GeneralIdGenerator.Generate();
        pol.NationId = NationId;
        pol.Rate = 0.0m;
        pol.taxType = type;
        pol.Minimum = min;
        pol.Maximum = max;
        pol.Collected = 0.0m;
        TaxPolicies.Add(pol);
    }

    public NationPolicyModel(Nation Nation)
    {
        NationId = Nation.Id;
        TaxPolicies = new();
        UBIPolicies = new();
        AddUBIPolicy(null, Nation.Id);
        AddUBIPolicy(Rank.Unranked, Nation.Id);
        AddUBIPolicy(Rank.Noob, Nation.Id);
        AddUBIPolicy(Rank.Fan, Nation.Id);
        AddUBIPolicy(Rank.Enjoyer, Nation.Id);
        AddUBIPolicy(Rank.Expert, Nation.Id);
        AddUBIPolicy(Rank.WashedUp, Nation.Id);
        UBIPolicies.Reverse();

        IEnumerable<TaxPolicy> oldpols = DBCache.GetAll<TaxPolicy>().Where(x => x.NationId == Nation.Id).OrderBy(x => x.taxType);
        if (oldpols.Count() > 0) {
            foreach(TaxPolicy pol in oldpols) {
                TaxPolicies.Add(pol);
            }
        }
        else {
            // Do other taxe
             
            AddTaxPolicy(Nation.Id, TaxType.Payroll);
            AddTaxPolicy(Nation.Id, TaxType.Sales);
            AddTaxPolicy(Nation.Id, TaxType.StockBought);
            AddTaxPolicy(Nation.Id, TaxType.StockSale);
            AddTaxPolicy(Nation.Id, TaxType.Transactional);
            AddTaxPolicy(Nation.Id, TaxType.UserBalance);
            AddTaxPolicy(Nation.Id, TaxType.GroupBalance);
            AddTaxPolicy(Nation.Id, TaxType.UserWealth);
            AddTaxPolicy(Nation.Id, TaxType.GroupWealth);
            AddTaxPolicy(Nation.Id, TaxType.ResourceMined);

            // do personal tax brackets

            for (int i = 0; i < 5; i++)
            {
                AddTaxPolicy(Nation.Id, TaxType.PersonalIncome);
            }

            // do corporate tax brackets

            for (int i = 0; i < 5; i++)
            {
                AddTaxPolicy(Nation.Id, TaxType.CorporateIncome);
            }
        }
    }
}