using System.ComponentModel.DataAnnotations.Schema;

namespace WUG.Database.Models.Government;

public enum ElectionType
{
    Senate = 1,
    PM = 2,
    President = 3,
    NationGovernor = 4
}

public class ResultData
{
    public User Candidate { get; set; }
    public int Votes { get; set; }

    public ResultData(User cand, int votes)
    {
        this.Candidate = cand;
        this.Votes = votes;
    }
}

public class Election
{
    public long Id {get; set; }

    // Nation the election is for
    public long NationId { get; set; }

    [NotMapped]
    public Nation Nation {
        get {
            return DBCache.Get<Nation>(NationId);
        }
    }

    // Time the election began
    public DateTime Start_Date { get; set; }

    // Time the election ended
    public DateTime End_Date { get; set; }

    // The resulting winner of the election
    public long WinnerId { get; set; }

    [NotMapped]
    public User Winner { 
        get {
            return DBCache.Get<User>(WinnerId)!;
        }
    }

    [Column(TypeName = "bigint[]")]
    public List<long> ChoiceIds { get; set; }

    // False if the election has been ended
    [NotMapped]
    public bool Active { 
        get {
            return DateTime.UtcNow < End_Date;
        }
    }

    // The kind of election this is
    public ElectionType Type { get; set; }

    public string GetElectionTitle()
    {
        if (NationId != 100) {
            return $"The {Nation.Name} Senate Election";
        }

        else if (Type == ElectionType.PM) {
            return $"The Prime Minister Election";
        }

        else if (Type == ElectionType.President) {
           return $"The Presidential Election";
        }
        return "";
    }

    public Election()
    {

    }

    public Election(DateTime start_date, DateTime end_date, List<long> choiceids, long Nationid, ElectionType type)
    {
        Id = IdManagers.GeneralIdGenerator.Generate();
        NationId = Nationid;
        Start_Date = start_date;
        End_Date = end_date;
        ChoiceIds = choiceids;
        Type = type;
    }
}