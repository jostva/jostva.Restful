#region usings

using AutoMapper;
using jostva.Restful.API.Entities;
using jostva.Restful.API.Mapping;
using jostva.Restful.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System.Linq;

#endregion

namespace jostva.Restful.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(item => item.UseSqlServer(connectionString));

            //  register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddScoped<IUrlHelper>(implementationFactory =>
            {
                ActionContext actionContext = implementationFactory.GetService<IActionContextAccessor>().ActionContext;
                IUrlHelper urlHelper = implementationFactory.GetService<IUrlHelperFactory>().GetUrlHelper(actionContext);

                return urlHelper;
            });

            services.AddTransient<IPropertyMappingService, PropertyMappingService>();

            services.AddTransient<ITypeHelperService, TypeHelperService>();

            //  Adding Cache Headers nuget: Marvin.Cache.Headers.
            services.AddHttpCacheHeaders((expirationModelOptions) =>
            {
                expirationModelOptions.MaxAge = 600;
            },
            (validationModelOptions) =>
            {
                validationModelOptions.MustRevalidate = true;
            });

            //  Adding Cache Store (Microsoft).
            services.AddResponseCaching();

            MapperConfiguration mappingConfig = new MapperConfiguration(config =>
            {
                config.AddProfile(new MappingProfile());
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());

                //  TODO:   INVESTIGAR!! - No está recibiendo XML....
                //setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter(setupAction));

                //  Xml Custom
                //var xmlDataContractSerializerInputFormatter =
                //        new XmlDataContractSerializerInputFormatter();
                //xmlDataContractSerializerInputFormatter.SupportedMediaTypes
                //    .Add("application/vnd.marvin.authorwithdateofdeath.full+xml");
                //setupAction.InputFormatters.Add(xmlDataContractSerializerInputFormatter);
                //  INVESTIGAR!! - No está recibiendo XML....

                var jsonInputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();
                if (jsonInputFormatter != null)
                {
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.author.full+json");
                    jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.authorwithdateofdeath.full+json");
                }

                var jsonOuputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
                if (jsonOuputFormatter != null)
                {
                    jsonOuputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
                }
            })
            //.AddXmlSerializerFormatters()
            //.AddXmlDataContractSerializerFormatters()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
                                ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            //loggerFactory.AddConsole();

            //loggerFactory.AddDebug(LogLevel.Information);

            // loggerFactory.AddProvider(new NLog.Extensions.Logging.NLogLoggerProvider());
            // loggerFactory.AddNLog();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        if (exceptionHandlerFeature != null)
                        {
                            ILogger logger = loggerFactory.CreateLogger("Global exception logger");
                            logger.LogError(500,
                                            exceptionHandlerFeature.Error,
                                            exceptionHandlerFeature.Error.Message);
                        }

                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happend. Try again later.");
                    });
                });

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            libraryContext.EnsureSeedDataForContext();

            app.UseResponseCaching();
            app.UseHttpCacheHeaders();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}