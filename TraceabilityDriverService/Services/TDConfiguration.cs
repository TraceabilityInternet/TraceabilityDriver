using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TraceabilityDriverService.Services.Interfaces;
using TraceabilityEngine.Interfaces.Driver;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Service.Util;
using TraceabilityEngine.Service.Util.DB;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityDriverService.Services
{
    public class TDConfiguration : ITDConfiguration
    {
        private IConfiguration _configuration;
         
        public TDConfiguration()
        {

        }

        public TDConfiguration(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            this.URL = configuration["URL"];
            this.ConnectionString = _configuration["ConnectionString"];
            this.DirectoryURL = _configuration["DirectoryURL"];
            if (!string.IsNullOrWhiteSpace(_configuration["ServiceProviderDID"]))
            {
                this.ServiceProviderDID = DIDFactory.Parse(_configuration["ServiceProviderDID"]);
            }
            if (!string.IsNullOrWhiteSpace(_configuration["ServiceProviderPGLN"]))
            {
                this.ServiceProviderPGLN = IdentifierFactory.ParsePGLN(_configuration["ServiceProviderPGLN"]);
            }
            this.APIKey = _configuration["APIKey"];
            this.RequiresTradingPartnerAuthorization = _configuration.GetValue<bool>("RequiresTradingPartnerAuthorization");
            this.EventURLTemplate = _configuration["EventURLTemplate"];
            this.LocationURLTemplate = _configuration["LocationURLTemplate"];
            this.TradeItemURLTemplate = _configuration["TradeItemURLTemplate"];
            this.TradingPartnerURLTemplate = _configuration["TradingPartnerURLTemplate"];

            MapperDLLPath = _configuration.GetValue<string>("MapperDLLPath");
            MapperClassName = _configuration.GetValue<string>("MapperClassName");
            this.Mapper = DriverUtil.LoadMapper(MapperDLLPath, MapperClassName);
        }

        public ITETraceabilityMapper Mapper { get; set; }
        public string ConnectionString { get; set; }
        public string URL { get; set; }
        public string DirectoryURL { get; set; }
        public IDID ServiceProviderDID { get; set; }
        public IPGLN ServiceProviderPGLN { get; set; }
        public string APIKey { get; set; }
        public string MapperDLLPath { get; set; }
        public string MapperClassName { get; set; }
        public bool RequiresTradingPartnerAuthorization { get; set; }
        public string EventURLTemplate { get; set; }
        public string TradeItemURLTemplate { get; set; }
        public string LocationURLTemplate { get; set; }
        public string TradingPartnerURLTemplate { get; set; }

        public ITEDriverDB GetDB()
        {
            return new TEDriverDB(this.ConnectionString);
        }
    }
}
