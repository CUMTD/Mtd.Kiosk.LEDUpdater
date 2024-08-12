using System.Xml.Serialization;

namespace Mtd.Kiosk.LEDUpdater.IpDisplaysApi.Models;

/*
 <DataItems>
<DataItem name="name1">42</DataItem>
<DataItem name="name2">21</DataItem>
</DataItems>
 */

[XmlRoot("DataItems")]
public class UpdateDataItemValuesXml
{
	[XmlElement("DataItem")]
	public List<DataItem> DataItems { get; set; }

	public UpdateDataItemValuesXml()
	{
		DataItems = new List<DataItem>();
	}

	public UpdateDataItemValuesXml(List<DataItem> dataItems)
	{
		DataItems = dataItems;
	}
}

public class DataItem
{
	[XmlAttribute("name")]
	public string Name { get; set; }
	[XmlText]
	public string Value { get; set; }
}
