using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Diagnostics;
using NLog.Extensions.Logging;

namespace Library.API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }		

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true; //Will now return 406 errors for non-supported content-types.
                //Default formatter is the first format in this list, that how asp.net core chooses the default formatter
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter()); //Add the xml formatter type, so our api can return xml files.
                setupAction.InputFormatters.Add(new XmlSerializerInputFormatter(setupAction));
                //setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter()); //This allows us to, as the name says, accept xml inputs to in the content-type field of the header.
            }); //Adds MVC services to the container so that they can be used for dependency injection.

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, 
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            loggerFactory.AddConsole();

            loggerFactory.AddDebug(LogLevel.Information);
            //loggerFactory.AddProvider(new NLog.Extensions.Logging.NLogLoggerProvider()); //Expects an instance of a class that implements ILogger provider.
            loggerFactory.AddNLog(); //Add Nlogger

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage(); //Leave as is during development
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        //We get a collection of http features provided by the server and middleware, available in the request.
                       IExceptionHandlerFeature exception =  context.Features.Get<IExceptionHandlerFeature>(); //This will let us look at the error property to look at the actual exception
                        if(exception != null)
                        {
                           ILogger log=  loggerFactory.CreateLogger("Global exception logger"); //Creating a logger
                            log.LogError(500, exception.Error, exception.Error.Message); //Logging the exception
                        }
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpecte fault happened. Try again later.");
                    });



                }); //Used in a production environment.
            }
            AutoMapper.Mapper.Initialize(cfg =>
            {
                //Author is the source and AuthorDto is the destination
                //Convention based it will map property names on source to same named property on destination. If property doesnt exist in destination then it is ignored
                cfg.CreateMap<Entities.Author, Models.AuthorDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}")) 
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.DateOfBirth.GetCurrentAge())); //Projecting since we have name and age in AuthorDto but not in Author
                cfg.CreateMap<Entities.Book, Models.BookDto>();
                cfg.CreateMap<Models.AuthorForCreationDto, Entities.Author>();
                cfg.CreateMap<Models.BooksForCreationDto, Entities.Author>();
                cfg.CreateMap<Models.BookForUpdateDto, Entities.Book>();
                cfg.CreateMap<Entities.Author, Models.BookForUpdateDto>(); //<source, dest>, source to dest
          
            });

            libraryContext.EnsureSeedDataForContext();

            app.UseMvc(); 
        }
    }
}
