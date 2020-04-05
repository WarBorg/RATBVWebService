using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using RATBVData.Models.Enums;
using RATBVData.Models.Models;
using RATBVWebService.Data.Interfaces;

namespace RATBVWebService.Data.Services
{
    public class BusWebCrawlerService : IBusDataService
    {
        #region Bus Lines Methods

        // TODO Get busses from TRANSPORT METROPOLITAN
        public async Task<List<BusLineModel>> GetBusLinesAsync()
        {
            var id = 1;

            var busLines = new List<BusLineModel>();

            string url = "http://www.ratbv.ro/trasee-si-orare/";

            try
            {
                var httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);

                var response = (HttpWebResponse)await httpWebRequest.GetResponseAsync();

                Stream responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                var div = doc.DocumentNode
                             .Descendants("div")
                             .Where(x => x.Attributes
                                          .Contains("class") &&
                                         x.Attributes["class"]
                                          .Value
                                          .Contains("box continut-pagina"))
                             .SingleOrDefault();

                var table = div.Element("table");

                // Skip one because of the bus type titles on first row
                var busLinesRows = table.Element("tbody")
                                        .Elements("tr")
                                        .Skip(1);

                foreach (var row in busLinesRows)
                {
                    var items = row.Elements("td")
                                   .ToArray();

                    var line = string.Empty;
                    var route = string.Empty;
                    var type = BusTypes.Bus;

                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i].InnerText
                                    .Trim()
                                    .Replace("&nbsp;", string.Empty)
                                    .Length == 0)
                        {
                            continue;
                        }

                        var str = items[i].InnerText
                                          .Trim()
                                          .Replace("&nbsp;", " ")
                                          .Replace("&acirc;", "â");

                        Console.WriteLine(str);

                        // If there are cells which dont contain the a href element it means we are at the
                        // TRANSPORT METROPOLITAN line so we break
                        if (items[i].Descendants("a")
                                    .FirstOrDefault() == null)
                        {
                            break;
                        }

                        string linkNormalWay = items[i].Descendants("a")
                                                       .FirstOrDefault()
                                                       .Attributes["href"]
                                                       .Value;

                        string linkReverseWay = linkNormalWay.Replace("dus", "intors");

                        CleanUpBusLinesText(ref line, ref route, ref type, i, str);

                        busLines.Add(new BusLineModel
                        {
                            Id = id++,
                            Name = line,
                            Route = route,
                            Type = type.ToString(),
                            LinkNormalWay = linkNormalWay,
                            LinkReverseWay = linkReverseWay
                        });
                    }
                }

                return busLines;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void CleanUpBusLinesText(ref string line, ref string route, ref BusTypes type, int i, string str)
        {
            // Add a space between the eventual letter after the number (ex 5B)
            str = Regex.Replace(str,
                                "(?<=[0-9])(?=[A-Za-z])|(?<=[A-Za-z])(?=[0-9])",
                                " ");

            var line_route = str.Split(' ')
                                .ToList();

            // In case the number contains a letter connate it back to the number (ex 5B)
            if (line_route[2].Length == 1)
            {
                line_route[1] += line_route[2];
                line_route.RemoveAt(2);
            }

            line = $"{line_route[0]} {line_route[1]}";

            // When creating the route skip the first two items as they are the line number
            route = line_route.Skip(2)
                              .Aggregate((k, l) => $"{k} {l}");

            if (i == 0)
            {
                type = BusTypes.Bus;
            }
            else if (i == 1)
            {
                type = BusTypes.Midibus;
            }
            else if (i == 2)
            {
                type = BusTypes.Trolleybus;
            }
        }

        #endregion

        public Task<List<BusStationModel>> GetBusStationsAsync(string lineNumberLink)
        {
            throw new NotImplementedException();
        }

        public Task<List<BusTimeTableModel>> GetBusTimeTableAsync(string schedualLink)
        {
            throw new NotImplementedException();
        }
    }
}
