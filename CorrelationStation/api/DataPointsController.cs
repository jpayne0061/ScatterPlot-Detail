﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CorrelationStation.Models;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using System.Web.Helpers;
using System.Data.Entity;
using CorrelationStation.Dal;

namespace CorrelationStation.Controllers
{
    public class DataPointsController : ApiController
    {
        private ApplicationDbContext _context;

        public DataPointsController()
        {
            _context = new ApplicationDbContext();
        }


        [HttpGet]
        public List<dynamic> GetScatterPlot(int id)
        {
            Blobs blob = new Blobs();

            List<string> mappedColumns = blob.GetMap(id).Split(',').ToList();

            string data1 = mappedColumns[0];
            string data2 = mappedColumns[1];

            mappedColumns.RemoveAt(0);
            mappedColumns.RemoveAt(0);

            string columns = string.Join(",", mappedColumns);

            Dictionary<int, string> map = Methods.MapToDict(columns);

            List<string> rows = blob.GetBlobs(id);

            List<dynamic> jsonList = Methods.MakeJsonFromBlobAndMap(map, rows);

            jsonList.Insert(0, data1);
            jsonList.Insert(0, data2);

            return jsonList;

        }




        [HttpPost]
        public List<double[]> ReturnPearson([FromBody] ScatterPlotRequest scatterPlotRequest)
        {
            int statId = scatterPlotRequest.StatId;

            PearsonCorr pearsonCorr = _context.PearsonCorrs.SingleOrDefault(ss => ss.Id == statId);
            List<double[]> dataPairs;

            if (scatterPlotRequest.Switch == "false")
            {
                dataPairs = Methods.ProcessScatterPlotRequest(pearsonCorr.Variable1Data, pearsonCorr.Variable2Data);
            }
            else
            {
                dataPairs = Methods.ProcessScatterPlotRequest(pearsonCorr.Variable2Data, pearsonCorr.Variable1Data);
            }

            return dataPairs;
        }


        [HttpPost]
        public List<double[]> ReturnPearsonWithMeta([FromBody] ScatterPlotRequest scatterPlotRequest)
        {
            int statId = scatterPlotRequest.StatId;

            PearsonCorr pearsonCorr = _context.PearsonCorrs.SingleOrDefault(ss => ss.Id == statId);
            List<double[]> dataPairs;

            if (scatterPlotRequest.Switch == "false")
            {
                dataPairs = Methods.ProcessScatterPlotRequestWithMeta(pearsonCorr.Variable1Data, pearsonCorr.Variable2Data);
            }
            else
            {
                dataPairs = Methods.ProcessScatterPlotRequestWithMeta(pearsonCorr.Variable2Data, pearsonCorr.Variable1Data);
            }

            return dataPairs;
        }




        [HttpGet]
        public List<DateAndNumeralValues> GetNumeralLinePlot(int id)
        {
            DateAndNumeral dn = _context.DateAndNumerals.SingleOrDefault(x => x.Id == id);

            List<string> dates = dn.DateData.Split(',').ToList();
            List<string> nums = dn.NumeralData.Split(',').ToList();

            List<DateAndNumeralValues> dns = Methods.ProcessDateNumerals(dates, nums).OrderBy(x => x.Date).ToList();

            return dns;
        }

        [HttpGet]
        public List<List<KeyValue>> GetChiTablesData(int id)
        {
            var values = new List<List<KeyValue>>();
            var compare = new List<List<KeyValue>>();
            var significant = new List<KeyValue>();

            var chiStat = _context.ChiStats.Include(c => c.ExpectedValues)
                                            .Include(c => c.ObservedValues)
                                            .Include(c => c.VariableCategories)
                                            .Include(c => c.Variable2Categories)
                                            .SingleOrDefault(c => c.Id == id);

            int count = chiStat.ExpectedValues.Count;

            for(var i = 0; i < count; i++)
            {
                compare.Add(new List<KeyValue> { chiStat.ExpectedValues[i], chiStat.ObservedValues[i] });
            }

            List<List<KeyValue>> compareDescending = compare.OrderByDescending(ls => (Math.Abs(ls[0].Value - ls[1].Value))).Take((int)(count*.05)).ToList();
            foreach(var kv in compareDescending)
            {
                significant.Add(kv[0]);
            }


            values.Add(chiStat.ExpectedValues);
            values.Add(chiStat.ObservedValues);
            values.Add(chiStat.VariableCategories);
            values.Add(chiStat.Variable2Categories);
            values.Add(significant);


            return values;
        }

        public List<List<KeyValue>> GetChiPercentages(int id)
        {

            //THIS IS BROKEN-----OBSERVEDPER
            var percentages = new List<List<KeyValue>>();

            var chiStat = _context.ChiStats.Include(c => c.ObservedPercentage)
                                            .Include(c => c.ExpectedPercentage)
                                            .SingleOrDefault(c => c.Id == id);

            int count = chiStat.ObservedPercentage.Count; 

            for(var i = 0; i < count; i++)
            {
                percentages.Add(new List<KeyValue> { chiStat.ObservedPercentage[i], chiStat.ExpectedPercentage[i] });
            }

            List<List<KeyValue>> percentagesDescending = percentages.OrderByDescending(ls => (Math.Abs(ls[0].Value - ls[1].Value))).ToList();

            //percentages.Add(chiStat.ObservedPercentage);
            //percentages.Add(chiStat.ExpectedPercentage);

            return percentagesDescending;
        }

        public List<KeyValue> GetAnovaMeans(int id)
        {
            var anovaStat = _context.AnovaStats.Include(a => a.Means)
                                                .SingleOrDefault(a => a.Id == id);

            return anovaStat.Means;
        }


        //GetDateCategoryLinePlot
        [HttpGet]
        public ICollection<LinePlotCategory> GetDateCategoryLinePlot(int id)
        {
            //var dateCat = _context.DateAndCategories.Include(d => d.TimePeriods.Select(t => t.CategoryCounts))
            //                                    .SingleOrDefault(a => a.Id == id);

            var dateCat = _context.DateAndCategories.Include(d => d.LinePlotCategories.Select(t => t.DateAndCounts))
                                                .SingleOrDefault(a => a.Id == id);

            //dateCat.TimePeriods.ToList().Sort((x, y) => DateTime.Compare(x.Date, y.Date));
            foreach(LinePlotCategory lineCat in dateCat.LinePlotCategories)
            {
                //lineCat.DateAndCounts.ToList().Sort((x, y) => DateTime.Compare(x.DateTime, y.DateTime));
                lineCat.DateAndCounts = lineCat.DateAndCounts.OrderBy(x => x.DateTime).ToList();
            }
            return dateCat.LinePlotCategories;
        }



        [HttpGet]
        public IHttpActionResult SaveToReports(int id)
        {
            string userId = User.Identity.GetUserId();

            ApplicationUser user = _context.Users.Include(u => u.StatSummaries).SingleOrDefault(u => u.Id == userId);
            StatSummaryVM statSummary = _context.StatSummaryVMs.SingleOrDefault(ss => ss.Id == id);
            //statSummary.ApplicationUser = user;

            if(!Methods.CheckIfReportSaved(userId, statSummary.Id))
            {
                statSummary.ApplicationUsers.Add(user);
                user.StatSummaries.Add(statSummary);
                _context.SaveChanges();
            }

            return Ok();
        }

        //[HttpGet]
        //public IHttpActionResult RemoveStatSummary(int id)
        //{
        //    StatSummaryVM ss = _context.StatSummaryVMs.Include(x => x.AnovaStats.Select(a => a.Means))
        //                                              .Include(x => x.ChiStats.Select(c => c.ExpectedPercentage))
        //                                              .Include(x => x.ChiStats.Select(c => c.ExpectedValues))
        //                                              .Include(x => x.ChiStats.Select(c => c.ObservedPercentage))
        //                                              .Include(x => x.ChiStats.Select(c => c.ObservedValues))
        //                                                .Include(x => x.PearsonCorrs)
        //                                                .Include(x => x.DateAndCatories.Select(dc => dc.LinePlotCategories.Select(lp => lp.DateAndCounts)))
        //                                                .Include(x => x.DateAndNumerals)
        //                                                .SingleOrDefault(x => x.Id == id);


        //    //foreach(var anova in ss.AnovaStats)
        //    //{
        //    //    _context.AnovaStats.
        //    //}

        //    //deleteMe.Prices.ToList().ForEach(p => db.ItemPrices.Remove(p));
        //    //var itemsToDelete = _context.Set<KeyValue>().Where(kv => kv.);
        //    List <AnovaStats> anovas = ss.AnovaStats.ToList();
        //    List<ChiStats> chis = ss.ChiStats.ToList();
        //    List<PearsonCorr> pcs = ss.PearsonCorrs.ToList();
        //    List<DateAndNumeral> dns = ss.DateAndNumerals.ToList();
        //    List<DateAndCategory> dcs = ss.DateAndCatories.ToList();

        //    foreach (AnovaStats anova in anovas)
        //    {

        //       _context.KeyValues.RemoveRange(anova.Means);
                
        //    }
        //    ss.AnovaStats.ToList().ForEach(a => _context.AnovaStats.Remove(a));

        //    foreach (ChiStats chi in chis)
        //    {

        //        _context.KeyValues.RemoveRange(chi.ExpectedPercentage);
        //        _context.KeyValues.RemoveRange(chi.ExpectedValues);
        //        _context.KeyValues.RemoveRange(chi.ObservedPercentage);
        //        _context.KeyValues.RemoveRange(chi.ObservedValues);

        //    }
            
        //    foreach(DateAndCategory dc in dcs)
        //    {
        //        foreach(LinePlotCategory lpc in dc.LinePlotCategories)
        //        {
        //            _context.DateAndCounts.RemoveRange(lpc.DateAndCounts);
        //        }
        //        _context.LinePlotCategories.RemoveRange(dc.LinePlotCategories);
        //    }

        //    ss.DateAndCatories.ToList().ForEach(dc => _context.DateAndCategories.Remove(dc));

        //    ss.DateAndNumerals.ToList().ForEach(dn => _context.DateAndNumerals.Remove(dn));

        //    ss.ChiStats.ToList().ForEach(c => _context.ChiStats.Remove(c));

        //    ss.PearsonCorrs.ToList().ForEach(p => _context.PearsonCorrs.Remove(p));


        //    _context.Entry(ss).State = EntityState.Deleted;

        //    //_context.StatSummaryVMs.Remove(ss);
        //    _context.SaveChanges();
        //    return Ok();
        //}

    }
}
