using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using vatsys;
using vatsys.Plugin;

namespace AirportsPlugin
{
    [Export(typeof(IPlugin))]
    public class Plugin : IPlugin
    {
        public string Name => "More Airports";
        public static string DisplayName => "More Airports";

        private static readonly string AsaUrl = "https://www.airservicesaustralia.com/aip/current/ersa/LND__07SEP2023.pdf";

        private static readonly string VatspyUrl = "https://raw.githubusercontent.com/vatsimnetwork/vatspy-data-project/master/VATSpy.dat";
        private static HttpClient HttpClient { get; set; } = new HttpClient();
        public static string DatasetPath { get; set; }
        public static List<string> ValidICAO { get; set; } = new List<string>();

        public Plugin()
        {
            GetSettings();

            _ = BorrowData();

            _ = GoGoGadget();
        }

        public async Task BorrowData()
        {
            var regex = "(Y[A-Z]{3})/";

            try
            {
                var response = await HttpClient.GetAsync(AsaUrl);

                var responseContent = await response.Content.ReadAsStreamAsync();

                using (PdfDocument document = PdfDocument.Open(responseContent))
                {
                    foreach (Page page in document.GetPages())
                    {
                        string pageText = page.Text;

                        foreach (Word word in page.GetWords())
                        {
                            var match = Regex.Match(word.Text, regex);

                            if (!match.Success) continue;

                            var icao = word.Text.Substring(0, 4);

                            if (ValidICAO.Contains(icao)) continue;

                            ValidICAO.Add(icao);
                        }
                    }
                }
            }
            catch { } 
        }

        public async Task GoGoGadget()
        {
            var regex = "(Y[A-Z]{3}\\|.+\\|0)";

            var response = await HttpClient.GetAsync(VatspyUrl);

            var responseContent = await response.Content.ReadAsStringAsync();

            var matches = Regex.Matches(responseContent, regex);

            var vatspyData = new List<string>();

            foreach (Match match in matches)
            {
                vatspyData.Add(match.Value);
            }

            // Errors.Add(new Exception($"{vatspyData.Count} airports found."), DisplayName);

            var toAdd = new List<string>();

            foreach (var data in vatspyData.Skip(2))
            {
                var icao = data.Substring(0, 4);

                var existing = Airspace2.GetAirport(icao);

                if (existing != null) continue;

                //var valid = ValidICAO.Any(x => x == icao);

                //if (!valid) continue;

                var split = data.Split('|');

                try
                {
                    var airport = new Airport(split[0], split[1], split[2], split[3]);

                    toAdd.Add($"<Airport ICAO=\"{airport.ICAO}\" FullName=\"{airport.Name}\" Position=\"{airport.IsoString}\" />");
                }
                catch { }
            }

            if (!toAdd.Any()) return;

            try
            {
                var file = DatasetPath + "\\Airspace.xml";

                string updated = File.ReadAllText(file);

                updated = updated.Replace("</Airports>", "");
                updated = updated.Replace("</Airspace>", "");
                updated += "<!-- MORE AIRPORTS -->";

                foreach (var line in toAdd)
                {
                    updated += line;
                }

                updated += "</Airports>";
                updated += "</Airspace>";

                File.WriteAllText(file, updated);

                Errors.Add(new Exception($"{toAdd.Count} airport(s) added. Restart vatSys to apply changes."), DisplayName);
            }

            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not save changes to Airspace.xml: {ex.Message}"), DisplayName);
            }
        }

        private void GetSettings()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

            if (!configuration.HasFile) return;

            if (!File.Exists(configuration.FilePath)) return;

            var config = File.ReadAllText(configuration.FilePath);

            XmlDocument doc = new XmlDocument();

            doc.LoadXml(config);

            XmlElement root = doc.DocumentElement;

            var userSettings = root.SelectSingleNode("userSettings");

            var settings = userSettings.SelectSingleNode("vatsys.Properties.Settings");

            foreach (XmlNode node in settings.ChildNodes)
            {
                if (node.Attributes.GetNamedItem("name").Value == "DatasetPath")
                {
                    DatasetPath = node.InnerText;
                    break;
                }
            }
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            return;
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            return;
        }
    }
}
