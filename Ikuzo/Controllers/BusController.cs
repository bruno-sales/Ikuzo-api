﻿using System;
using System.Web.Http;
using Ikuzo.Application.Interfaces;
using Ikuzo.RequestModels;

namespace Ikuzo.Controllers
{
    [RoutePrefix("v1/api/buses")]
    public class BusController : ApiController
    {
        private readonly IBusApp _busApp;

        public BusController(IBusApp busApp)
        {
            _busApp = busApp;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetBuses()
        {
            var buses = _busApp.GetBuses();

            return Ok(buses);
        }

        [HttpGet]
        [Route("{busId}")]
        public IHttpActionResult GetBus([FromUri] string busId)
        {
            var bus = _busApp.GetBus(busId);

            if(bus != null)
                return Ok(bus);

            return NotFound();
        }

        [HttpGet]
        [Route("nearby")]
        public IHttpActionResult NearbyBuses([FromUri] NerbyBusesRequest request)
        {
            if (request == null)
                return BadRequest();

            decimal variance;

            try
            {
                if (request.Precision == null || request.Precision > 100 || request.Precision < 0)
                    variance = new decimal(0.0035); //0.0035 1060 m -> 0.01 3027 m
                else
                {
                    //a*X + b
                    variance = (request.Precision.Value * new decimal(-0.65) + new decimal(100)) / (decimal) 10000.0;
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }

            var buses = _busApp.GetNearbyBuses(request.Lat, request.Lon, variance, request.Line);

            if (buses != null)
                return Ok(buses);

            return NotFound();
        }
    }
}
