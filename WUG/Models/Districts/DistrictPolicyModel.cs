using System.ComponentModel.DataAnnotations.Schema;


namespace WUG.Models.Districts;

public class DistrictPolicyModel
{
    public List<TaxPolicy> TaxPolicies { get; set; }
    public List<UBIPolicy> UBIPolicies { get; set; }

    public long DistrictId { get; set; }

    [NotMapped]
    public Nation District {
        get {
            return DBCache.Get<Nation>(DistrictId)!;
        }
    }

    public DistrictPolicyModel()
    {
        
    }

    public void AddUBIPolicy(Rank? rank, long DistrictId)
    {
        UBIPolicy pol = new();
        pol.NationId = DistrictId;
        if (rank is null) {
            pol.Anyone = true;
        }
        else {
            pol.ApplicableRank = rank;
        }
        pol.Id = IdManagers.GeneralIdGenerator.Generate();

        UBIPolicy? oldpol = DBCache.GetAll<UBIPolicy>().FirstOrDefault(x => x.NationId == DistrictId && x.ApplicableRank == rank);
        if (oldpol is not null) {
            pol.Rate = oldpol.Rate;
        }

        UBIPolicies.Add(pol);
    }

    public void AddTaxPolicy(long DistrictId, TaxType type, decimal min = 0.0m, decimal max = 99999999.0m)
    {
        TaxPolicy pol = new();
        pol.Id = IdManagers.GeneralIdGenerator.Generate();
        pol.NationId = DistrictId;
        pol.Rate = 0.0m;
        pol.taxType = type;
        pol.Minimum = min;
        pol.Maximum = max;
        pol.Collected = 0.0m;
        TaxPolicies.Add(pol);
    }

    public DistrictPolicyModel(Nation district)
    {
        DistrictId = district.Id;
        TaxPolicies = new();
        UBIPolicies = new();
        AddUBIPolicy(null, district.Id);
        AddUBIPolicy(Rank.Unranked, district.Id);
        AddUBIPolicy(Rank.Noob, district.Id);
        AddUBIPolicy(Rank.Fan, district.Id);
        AddUBIPolicy(Rank.Enjoyer, district.Id);
        AddUBIPolicy(Rank.Expert, district.Id);
        AddUBIPolicy(Rank.WashedUp, district.Id);
        UBIPolicies.Reverse();

        IEnumerable<TaxPolicy> oldpols = DBCache.GetAll<TaxPolicy>().Where(x => x.NationId == district.Id).OrderBy(x => x.taxType);
        if (oldpols.Count() > 0) {
            foreach(TaxPolicy pol in oldpols) {
                TaxPolicies.Add(pol);
            }
        }
        else {
            // Do other taxe
             
            AddTaxPolicy(district.Id, TaxType.Payroll);
            AddTaxPolicy(district.Id, TaxType.Sales);
            AddTaxPolicy(district.Id, TaxType.StockBought);
            AddTaxPolicy(district.Id, TaxType.StockSale);
            AddTaxPolicy(district.Id, TaxType.Transactional);
            AddTaxPolicy(district.Id, TaxType.UserBalance);
            AddTaxPolicy(district.Id, TaxType.GroupBalance);
            AddTaxPolicy(district.Id, TaxType.UserWealth);
            AddTaxPolicy(district.Id, TaxType.GroupWealth);
            AddTaxPolicy(district.Id, TaxType.ResourceMined);

            // do personal tax brackets

            for (int i = 0; i < 5; i++)
            {
                AddTaxPolicy(district.Id, TaxType.PersonalIncome);
            }

            // do corporate tax brackets

            for (int i = 0; i < 5; i++)
            {
                AddTaxPolicy(district.Id, TaxType.CorporateIncome);
            }
        }
    }
}