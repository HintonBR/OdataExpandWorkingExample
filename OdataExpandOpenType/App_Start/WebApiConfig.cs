namespace OdataExpandOpenType
{
    using System.Web.Http;
    using System.Web.OData.Builder;
    using System.Web.OData.Extensions;

    using OdataExpandOpenType.Controllers;
    using System.Web.OData.Formatter;
    using System.Web.OData.Formatter.Deserialization;

    using OdataExpandOpenType.App_Start;

    internal static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            // OData routes
            ODataModelBuilder builder = new ODataConventionModelBuilder
            {
                Namespace = "OdataExpandOpenType",
                ContainerName = "OdataExpandOpenTypeContainer",
            };
            builder.EntitySet<Person>("Persons");

            config.MapODataServiceRoute("ODataRoute", "api", builder.GetEdmModel());
            //var odataFormatters = ODataMediaTypeFormatters.Create(new CustomODataSerializerProvider(), new DefaultODataDeserializerProvider());
            //config.Formatters.InsertRange(0, odataFormatters);
            
            config.EnsureInitialized();
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        }
    }
}
