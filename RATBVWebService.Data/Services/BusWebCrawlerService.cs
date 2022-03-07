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

        // TODO Get buses from TRANSPORT METROPOLITAN
        // TODO Get buses from TRANSPORT ELEVI
        public async Task<List<BusLineModel>> GetBusLinesAsync()
        {
            var id = 1;

            var busLines = new List<BusLineModel>();

            var url = $"{WebSiteRoot}trasee-si-orare/";

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

                if (table == null)
                {
                    throw new Exception("Bus Lines div element table could not be found.");
                }

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

                    if (items == null)
                    {
                        throw new Exception("Bus Lines td elements could not be found.");
                    }

                    var line = string.Empty;
                    var route = string.Empty;
                    var type = BusTypes.Bus;

                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i] == null)
                        {
                            continue;
                        }

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

                        // There is a new bus line "Transport Elevi" which was added as a special item
                        // so this will give us error so we skip
                        if (str.Contains("Elevi"))
                        {
                            breakLoop = true;

                            break;
                        }

                        // If there are cells which dont contain the a href element it means we are at the
                        // TRANSPORT METROPOLITAN line so we break
                        if (items[i].Descendants("a")
                                    .FirstOrDefault() == null)
                        {
                            breakLoop = true;

                            break;
                        }

                        var linkNormalWay = items[i].Descendants("a")
                                                   ?.FirstOrDefault()
                                                   ?.Attributes["href"]
                                                   ?.Value
                                                   ?.Remove(0, 1)
                                                   // Replace the / to a more common HTML frendly carachter 
                                                   ?.Replace("/", "___");

                        if (linkNormalWay == null)
                        {
                            throw new Exception("Bus Line link normal way could be corrupted or not found.");
                        }

                        var linkReverseWay = linkNormalWay.Replace("dus", "intors");

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
            try
            {
                // Add a space between the eventual letter after the number (ex 5B)
                str = Regex.Replace(str.Trim(),
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
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{nameof(CleanUpBusLinesText)} has thrown an error while cleaning up text for: {str}");
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

                if (lineNumberStationsLink == null)
                {
                    throw new Exception("Bus Stations links could be corrupted or not found.");
                }

                var url = $"{WebSiteRoot}afisaje/{lineNumberStationsLink}";

                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                var div = doc?.DocumentNode
                             ?.Descendants("div")
                             ?.Where(x => x.Attributes
                                           .Contains("id")
                                      && x.Attributes["id"]
                                          .Value
                                          .Contains("div_center_"))
                             ?.ToList();

                if (div == null)
                {
                    throw new Exception("Bus Stations div element could not be found.");
                }

                var isFirstScheduleLink = true;
                var firstScheduleLink = string.Empty;

                foreach (HtmlNode station in div)
                {
                    var stationName = station?.InnerText
                                             ?.Trim()
                                             ?? "Corrupted station";
                    var scheduleLink = station?.Descendants("a")
                                              ?.FirstOrDefault()
                                              ?.Attributes["href"]
                                              ?.Value;

                    var lineLink = lineNumberStationsLink.Substring(0, lineNumberStationsLink.IndexOf('/'));

                    var fullSchedualLink = $"{lineLink}/{scheduleLink}";

                    // Save the first schedule link 
                    if (isFirstScheduleLink)
                    {
                        firstScheduleLink = scheduleLink;

                        isFirstScheduleLink = false;
                    }

                    if (firstScheduleLink == null)
                    {
                        throw new Exception($"Bus Station {stationName} first schedule link is corrupt or could not be found.");
                    }

                    if (fullSchedualLink.Contains("/../"))
                    {
                        var reverseScheduleLink = firstScheduleLink;
                        var reverseLineLink = fullSchedualLink?.Substring(fullSchedualLink.LastIndexOf("/") + 1)
                                                              ?.Replace(".html", string.Empty);

                        if (reverseScheduleLink.Contains("_cl1_"))
                        {
                            reverseScheduleLink = reverseScheduleLink.Replace("_cl1_", "_cl2_");
                        }
                        else if (reverseScheduleLink.Contains("_cl2_"))
                        {
                            reverseScheduleLink = reverseScheduleLink.Replace("_cl2_", "_cl1_");
                        }

                        if (reverseLineLink == null)
                        {
                            throw new Exception($"Bus Station {stationName} reverse schedule link is corrupt or could not be found.");
                        }

                        fullSchedualLink = $"{reverseLineLink}/{reverseScheduleLink}";
                    }

                    busStations.Add(new BusStationModel
                    {
                        Name = stationName,
                        ScheduleLink = fullSchedualLink.Replace("/", "___")
                    });
                }

                return busStations;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string?> GetBusMainDisplayAsync(string lineNumberLink)
        {
            try
            {
                var formattedLineNumberLink = lineNumberLink.Replace("___", "/");

                var url = $"{WebSiteRoot}{formattedLineNumberLink}";

                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                HtmlNode? frameStations = doc.DocumentNode
                                            ?.Descendants("frame")
                                            ?.Where(x => x.Attributes
                                                          .Contains("name")
                                                      && x.Attributes
                                                          .Contains("noresize")
                                                      && x.Attributes["name"]
                                                          .Value
                                                          .Equals("frTabs")
                                                      && x.Attributes["noresize"]
                                                          .Value
                                                          .Equals("noresize"))
                                            ?.SingleOrDefault();

                return frameStations?.Attributes["src"]
                                    ?.Value;
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

                var formattedSchedualLink = schedualLink.Replace("___", "/");

                var url = $"{WebSiteRoot}afisaje/{formattedSchedualLink}";

                var httpWebRequest = WebRequest.Create(url);

                var response = await httpWebRequest.GetResponseAsync();

                var responseStream = response.GetResponseStream();

                var doc = new HtmlDocument();

                doc.Load(responseStream);

                var tableWeekdays = doc?.GetElementbyId("tabele")
                                       ?.ChildNodes[1];
                var tableWeekend = doc?.GetElementbyId("tabele")
                                      ?.ChildNodes[3];

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
                                               HtmlNode? timetable,
                                               TimeOfTheWeek timeOfWeek)
        {
            if (timetable == null)
            {
                throw new Exception($"Bus timetable is corrupt or could not be found.");
            }

            var hour = string.Empty;
            var minutes = string.Empty;

            // Skip first three items because of time of week div, hour div and minutes div
            var timetableHtmlNodes = timetable.Descendants("div")
                                                ?.ToList()
                                                ?.Skip(3);

            if (timetableHtmlNodes == null)
            {
                throw new Exception($"Bus timetable rows are corrupt or could not be found.");
            }

            foreach (HtmlNode node in timetableHtmlNodes)
            {
                if (node.Id == "web_class_hours")
                {
                    hour = node.InnerText
                               .Replace('\n', ' ')
                               .Trim();
                }
                else if (node.Id == "web_class_minutes")
                {
                    var minuteNodes = node?.Descendants("div")
                                          ?.ToList();

                    if (minuteNodes == null)
                    {
                        throw new Exception($"Bus timetable minute rows are corrupt or could not be found.");
                    }

                    foreach (var minuteNode in minuteNodes)
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