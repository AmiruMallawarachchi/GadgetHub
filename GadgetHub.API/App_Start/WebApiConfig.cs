using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace GadgetHub.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // CORS temporarily disabled due to missing assembly
            // var cors = new EnableCorsAttribute("*", "*", "*");
            // config.EnableCors(cors);

            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Return JSON by default
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/html"));
        }
    }
}