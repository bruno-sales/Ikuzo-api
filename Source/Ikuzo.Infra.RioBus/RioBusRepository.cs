﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Text;
using Ikuzo.Domain.Entities;
using Ikuzo.Domain.Interfaces.CrossCuttings;
using Ikuzo.Domain.ValueObjects.RioBus;
using Newtonsoft.Json;
using RestSharp;

namespace Ikuzo.Infra.RioBus
{
    public class RioBusRepository : IRioBusRepository
    {
        private readonly IRestClient _client;
        private readonly IDataRioRepository _datario;

        public RioBusRepository(IDataRioRepository datario)
        {
            _datario = datario;
            _client = new RestClient(ConfigurationManager.AppSettings["RioBusUrl"]);
        }

        public IEnumerable<RbLine> GetAllLines()
        {
            var request = new RestRequest("itinerary", Method.GET)
            {
                JsonSerializer = new MySerializer()
            };

            var response = _client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
                return new List<RbLine>();

            var lines = JsonConvert.DeserializeObject<List<RbLine>>(response.Content);

            return lines;
        }


        public IEnumerable<RbBus> GetBusesInfoFromLine(string lineId)
        {
            var request = new RestRequest("search/{lineId}", Method.GET)
            {
                JsonSerializer = new MySerializer()
            };

            request.AddUrlSegment("lineId", lineId);

            var response = _client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
                return new List<RbBus>();

            var lines = JsonConvert.DeserializeObject<List<RbBus>>(response.Content);

            return lines;
        }

        public IEnumerable<Gps> GetGpsInfoFromLines(List<string> lineIds)
        {
            var sbLines = new StringBuilder();

            foreach (var lineId in lineIds)
            {
                sbLines.Append(lineId + ",");
            }

            var lines = sbLines.ToString(0, sbLines.Length - 1);

            var request = new RestRequest("search/{lineIds}", Method.GET)
            {
                JsonSerializer = new MySerializer()
            };

            request.AddUrlSegment("lineIds", lines);

            var response = _client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return _datario.GetGpsInformation();
            }

            var gps = JsonConvert.DeserializeObject<List<Gps>>(response.Content);

            return gps;
        }

        public RbItinerary GetItineraryInfoFromLine(string lineId)
        {
            var request = new RestRequest("itinerary/{lineId}", Method.GET)
            {
                JsonSerializer = new MySerializer()
            };

            request.AddUrlSegment("lineId", lineId);

            var response = _client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
                return new RbItinerary();

            var itinerary = JsonConvert.DeserializeObject<RbItinerary>(response.Content);

            return itinerary;
        }

        public void Dispose()
        {

            GC.Collect();
        }
    }
}
