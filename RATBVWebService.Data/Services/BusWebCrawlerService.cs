using System;
using System.Collections.Generic;
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
        #region Constants

        private const string WebSiteRoot = "https://www.ratbv.ro/";

        #endregion

        #region Bus Lines Methods

        // TODO Get busses from TRANSPORT METROPOLITAN
        public async Task<List<BusLineModel>> GetBusLinesAsync()
        {
            var id = 1;

            var busLines = new List<BusLineModel>();

            string url = $"{WebSiteRoot}trasee-si-orare/";

            try
            {
                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                var div = doc.DocumentNode
                             .Descendants("div")
                             .Where(x => x.Attributes
                                          .Contains("class")
                                      && x.Attributes["class"]
                                          .Value
                                          .Contains("box continut-pagina"))
                             .SingleOrDefault();

                var table = div?.Element("table");

                // Skip one because of the bus type titles on first row
                var busLinesRows = table.Element("tbody")
                                        .Elements("tr")
                                        .Skip(1);

                // HACK to get out of the foreachloop for the TRANSPORT METROPOLITAN section
                var breakLoop = false;

                foreach (var row in busLinesRows)
                {
                    if (breakLoop)
                    {
                        break;
                    }

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

                        // If there are cells which dont contain the a href element it means we are at the
                        // TRANSPORT METROPOLITAN line so we break
                        if (items[i].Descendants("a")
                                    .FirstOrDefault() == null)
                        {
                            breakLoop = true;

                            break;
                        }

                        string linkNormalWay = items[i]?.Descendants("a")
                                                       ?.FirstOrDefault()
                                                       ?.Attributes["href"]
                                                       ?.Value
                                                       ?.Remove(0, 1)
                                                       // Replace the / to a more common HTML frendly carachter 
                                                       ?.Replace("/", "___");

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
            catch (Exception ex)
            {
                throw ex;
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

            switch (i)
            {
                case 0:
                    type = BusTypes.Bus;
                    break;
                case 1:
                    type = BusTypes.Midibus;
                    break;
                case 2:
                    type = BusTypes.Trolleybus;
                    break;
            }
        }

        #endregion

        #region Bus Station Methods

        public async Task<List<BusStationModel>> GetBusStationsAsync(string lineNumberLink)
        {
            try
            {
                var busStations = new List<BusStationModel>();

                var lineNumberStationsLink = await GetBusMainDisplayAsync(lineNumberLink);

                var url = $"{WebSiteRoot}afisaje/{lineNumberStationsLink}";

                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                var div = doc.DocumentNode
                        .Descendants("div")
                        .Where(x => x.Attributes
                                     .Contains("id")
                                 && x.Attributes["id"]
                                     .Value
                                     .Contains("div_center_"))
                        .ToList();

                bool isFirstScheduleLink = true;
                string firstScheduleLink = string.Empty;

                foreach (HtmlNode station in div)
                {
                    string stationName = station.InnerText
                                                .Trim();
                    string scheduleLink = station.Descendants("a")
                                                 .FirstOrDefault()
                                                 .Attributes["href"]
                                                 .Value;
                    string lineLink = lineNumberStationsLink.Substring(0, lineNumberStationsLink.IndexOf('/'));
                    string fullSchedualLink = $"{lineLink}/{scheduleLink}";

                    // Save the first schedule link 
                    if (isFirstScheduleLink)
                    {
                        firstScheduleLink = scheduleLink;

                        isFirstScheduleLink = false;
                    }

                    if (fullSchedualLink.Contains("/../"))
                    {
                        string reverseScheduleLink = firstScheduleLink;
                        string reverseLineLink = fullSchedualLink.Substring(fullSchedualLink.LastIndexOf("/") + 1)
                                                                 .Replace(".html", string.Empty);

                        if (reverseScheduleLink.Contains("_cl1_"))
                        {
                            reverseScheduleLink = reverseScheduleLink.Replace("_cl1_", "_cl2_");
                        }
                        else if (reverseScheduleLink.Contains("_cl2_"))
                        {
                            reverseScheduleLink = reverseScheduleLink.Replace("_cl2_", "_cl1_");
                        }

                        fullSchedualLink = $"{reverseLineLink}/{reverseScheduleLink}";
                    }

                    busStations.Add(new BusStationModel
                    {
                        Name = stationName,
                        SchedualLink = fullSchedualLink.Replace("/", "___")
                    });
                }

                return busStations;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> GetBusMainDisplayAsync(string lineNumberLink)
        {
            try
            {
                var formattedLineNumberLink = lineNumberLink.Replace("___", "/");

                string url = $"{WebSiteRoot}{formattedLineNumberLink}";

                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                HtmlNode frameStations = doc.DocumentNode
                                            .Descendants("frame")
                                            .Where(x => x.Attributes
                                                         .Contains("name")
                                                     && x.Attributes
                                                         .Contains("noresize")
                                                     && x.Attributes["name"]
                                                         .Value
                                                         .Equals("frTabs")
                                                     && x.Attributes["noresize"]
                                                         .Value
                                                         .Equals("noresize"))
                                            .SingleOrDefault();

                return frameStations.Attributes["src"]
                                    .Value;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Bus Time Table Methods

        public async Task<List<BusTimeTableModel>> GetBusTimeTableAsync(string schedualLink)
        {
            try
            {
                var busTimeTable = new List<BusTimeTableModel>();

                string formattedSchedualLink = schedualLink.Replace("___", "/");

                string url = $"{WebSiteRoot}afisaje/{formattedSchedualLink}";

                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                var tableWeekdays = doc.GetElementbyId("tabele")
                                       .ChildNodes[1];
                var tableWeekend = doc.GetElementbyId("tabele")
                                      .ChildNodes[3];

                // Get the time of week time table
                GetTimeTablePerTimeofWeek(busTimeTable, tableWeekdays, TimeOfTheWeek.WeekDays);
                // Get the weekend time table
                GetTimeTablePerTimeofWeek(busTimeTable, tableWeekend, TimeOfTheWeek.Saturday);
                GetTimeTablePerTimeofWeek(busTimeTable, tableWeekend, TimeOfTheWeek.Sunday);

                return busTimeTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void GetTimeTablePerTimeofWeek(List<BusTimeTableModel> busTimeTable,
                                               HtmlNode tableWeekdays,
                                               TimeOfTheWeek timeOfWeek)
        {
            var hour = string.Empty;
            var minutes = string.Empty;

            // Skip first three items because of time of week div, hour div and minutes div
            foreach (HtmlNode node in tableWeekdays.Descendants("div")
                                                   .ToList()
                                                   .Skip(3))
            {
                if (node.Id == "web_class_hours")
                {
                    hour = node.InnerText
                               .Replace('\n', ' ')
                               .Trim();
                }
                else if (node.Id == "web_class_minutes")
                {
                    foreach (var minuteNode in node.Descendants("div").ToList())
                    {
                        minutes += $" {minuteNode.InnerText.Trim()}";
                    }
                }

                if (!string.IsNullOrEmpty(hour) && !string.IsNullOrEmpty(minutes))
                {
                    busTimeTable.Add(new BusTimeTableModel
                    {
                        Hour = hour,
                        Minutes = minutes.Substring(1),
                        TimeOfWeek = timeOfWeek.ToString()
                    });

                    hour = minutes = string.Empty;
                }
            }
        }

        #endregion
    }
}