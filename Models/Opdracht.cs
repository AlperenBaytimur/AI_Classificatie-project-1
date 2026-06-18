public class Opdracht
{
    public string? RelatieCode { get; set; }
    public int? Aantal { get; set; }
    public string? ContainerType { get; set; }
    public decimal? BrutoGewicht { get; set; }

    public List<string>? ZegelNummers { get; set; }
    public List<Activiteit>? Activiteiten { get; set; }

    public Financieel? Financieel { get; set; }
}