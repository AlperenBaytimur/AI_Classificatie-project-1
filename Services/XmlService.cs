using System.Xml.Linq;

public class XmlService
{
    public string ToXml(Opdracht o)
    {
        return ToOpdrachtElement(o).ToString();
    }

    public string ToXml(ExtractionOutcome result)
    {
        var xml = ToOpdrachtElement(result.Data);
        xml.Add(new XElement("ConfidenceScore", result.ConfidenceScore));

        return xml.ToString();
    }

    private static XElement ToOpdrachtElement(Opdracht o)
    {
        return new XElement("Opdracht",
            new XElement("RelatieCode", o.RelatieCode),
            new XElement("Aantal", o.Aantal),
            new XElement("ContainerType", o.ContainerType),
            new XElement("BrutoGewicht", o.BrutoGewicht),
            new XElement("ZegelNummers", o.ZegelNummers?.Select(z => new XElement("Zegel", z))
            ),

            new XElement("Activiteiten",
                o.Activiteiten?.Select(a =>
                    new XElement("Activiteit",
                        new XAttribute("type", a.Type ?? string.Empty),
                        new XElement("Terminal", a.Terminal),
                        new XElement("Bedrijf", a.Bedrijf),
                        new XElement("Straat", a.Straat),
                        new XElement("Postcode", a.Postcode),
                        new XElement("Plaats", a.Plaats)
                    )
                )
            ),

            new XElement("Financieel",
                new XElement("Object", o.Financieel?.Object),
                new XElement("EenheidsPrijs", o.Financieel?.EenheidsPrijs),
                new XElement("ValutaCode", o.Financieel?.ValutaCode),
                new XElement("BTWCode", o.Financieel?.BTWCode),
                new XElement("Fin_Code", o.Financieel?.Fin_Code)
            )
        );
    }
}
