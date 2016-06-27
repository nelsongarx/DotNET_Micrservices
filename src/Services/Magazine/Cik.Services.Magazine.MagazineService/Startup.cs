﻿using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Cik.Domain;
using Cik.Services.Magazine.MagazineService.Extensions;
using Cik.Services.Magazine.MagazineService.Model;
using Cik.Services.Magazine.MagazineService.QueryModel;
using Cik.Services.Magazine.MagazineService.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cik.Services.Magazine.MagazineService
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Use a PostgreSQL database
            var sqlConnectionString = Configuration["DataAccessPostgreSqlProvider:ConnectionString"];
            services.AddDbContext<MagazineDbContext>(options =>
                options.UseNpgsql(
                    sqlConnectionString,
                    b => b.MigrationsAssembly("Cik.Services.Magazine.MagazineService")
                    ));

            // Add framework services.
            services.AddMvc();

            // Autofac container
            var builder = new ContainerBuilder();
            builder.RegisterType<MagazineDbContext>().AsSelf().SingleInstance();
            builder.RegisterType<CategoryRepository>()
                .As<IRepository<Category, Guid>>()
                .InstancePerLifetimeScope();
            builder.RegisterInstance(new InMemoryBus()).SingleInstance();
            builder.Register(x => x.Resolve<InMemoryBus>()).As<ICommandHandler>();
            builder.Register(x => x.Resolve<InMemoryBus>()).As<IDomainEventPublisher>();
            builder.Register(x => x.Resolve<InMemoryBus>()).As<IHandlerRegistrar>();
            builder.RegisterType<CategoryQueryModelFinder>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterCommandHandlers();
            builder.Populate(services);

            // build up the container
            var container = builder.Build();
            container.RegisterHandlers(typeof (Startup));
            return container.Resolve<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                SeedData.InitializeMagazineDatabaseAsync(app.ApplicationServices).Wait();
            }

            app.UseMvc();
        }
    }
}