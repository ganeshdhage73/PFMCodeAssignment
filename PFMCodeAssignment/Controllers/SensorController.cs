using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using PFMCodeAssignment.Interfaces;
using PFMCodeAssignment.Models;
using System.Data;
using System.Data.Entity.SqlServer;
using System.Diagnostics.Metrics;

namespace PFMCodeAssignment.Controllers
{
    public class SensorController : Controller
    {
        private readonly ICSVService _csvService;

        public SensorController(ICSVService csvService)
        {
            _csvService = csvService;
        }

        //Provide aggregations like (hourly,daily,weekly) and upload provided csv file 
        [HttpPost("GetSensorCSVData")]
        public IActionResult GetSensorCSVData([FromForm] IFormFileCollection file, string aggregations)
        {
            //here i am considering we are uploading the provided file in email. so no checks added.
            List<DateGroup> dateGroups = new List<DateGroup>();
            var excelData = _csvService.ReadCSV<SensorData>(file[0].OpenReadStream()).ToList();
            int hours = 0; ;
            switch (aggregations.ToLower())
            {
                case "hourly":
                    hours = 1;
                    break;
                case "daily":
                    hours = 24;
                    break;
                case "weekly":
                    hours = 84;
                    break;
                default:
                    hours = 0;
                    break;
            }
                
            //find min & max dates
            DateTime minDate = excelData.MinBy(x => x.DateFrom).DateFrom;
            DateTime maxDate = excelData.MaxBy(x => x.DateTo).DateTo;
            DateTime startDateTime = minDate;
            DateTime endDateTime = startDateTime;

            //prepare list of date groups based on input hours
            while (endDateTime < maxDate)
            {
                endDateTime = startDateTime.AddHours(hours);
                DateGroup dateGroup = new DateGroup();
                dateGroup.DateFrom = startDateTime;
                dateGroup.DateTo = endDateTime;
                dateGroups.Add(dateGroup);

                startDateTime = endDateTime;
            }

            // loop through groups and prepare response list
            List<SensorData> responseList = new List<SensorData>();
            foreach (var group in dateGroups)
            {
                var list = excelData.Where(x => x.DateFrom >= group.DateFrom && x.DateTo <= group.DateTo).GroupBy(g => g.SensorId)
                    .Select(g => new SensorData { SensorId = g.Key, DateFrom = group.DateFrom, DateTo = group.DateTo, Count = g.Sum(x => x.Count) })
                    .ToList();
                responseList.AddRange(list);
            }
              
            return Ok(responseList.OrderBy(x => x.SensorId));
        }
    }
}