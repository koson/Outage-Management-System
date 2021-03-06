﻿using OMS.Web.API.Filters;
using Outage.Common;
using System.Web.Http;

namespace OMS.Web.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.EnableCors();
            config.Filters.Add(new CustomExceptionFilterAttribute((ILogger)config.DependencyResolver.GetService(typeof(ILogger))));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
