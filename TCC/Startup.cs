﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using TCC.Data;
using TCC.Models;
using TCC.RegraNegocio;

namespace TCC
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<ApiConfiguration>(Configuration.GetSection("ApiConfiguration"));

            services.AddScoped<MongoDbContext>();
            services.AddTransient<JogadorRegraNegocio>();
            services.AddTransient<Util>();
            services.AddTransient<ItensRegraNegocio>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            MongoDbContext.ConnectionString = Configuration.GetSection("MongoConnection:ConnectionString").Value;
            MongoDbContext.DatabaseName = Configuration.GetSection("MongoConnection:Database").Value;
            MongoDbContext.IsSSL = Convert.ToBoolean(Configuration.GetSection("MongoConnection:IsSSL").Value);

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
