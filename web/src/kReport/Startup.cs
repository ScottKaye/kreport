﻿using kReport.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace kReport
{
	public class Startup
	{
		public static string AppBasePath { get; private set; }
		public static IConfiguration Configuration { get; private set; }

		public static void RebuildConfiguration()
		{
			var builder = new ConfigurationBuilder(AppBasePath)
				.AddJsonFile("config.json")
				.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
		{
			Mongo.Client = new MongoClient();
			Mongo.Db = Mongo.Client.GetServer().GetDatabase("kReport");
			BsonClassMap.RegisterClassMap<kUser>();
			BsonClassMap.RegisterClassMap<Report>();
			BsonClassMap.RegisterClassMap<Middleman>();

			AppBasePath = appEnv.ApplicationBasePath;
			RebuildConfiguration();
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();
			services.AddSession();
			services.AddCaching();
			services.AddSignalR(options=>
			{
				options.EnableJSONP = true;
			});

			services.Configure<AuthorizationOptions>(options =>
			{
				options.AddPolicy("Admin", new AuthorizationPolicyBuilder().RequireClaim("Admin", "Allowed").Build());
				options.AddPolicy("User", new AuthorizationPolicyBuilder().RequireClaim("User", "Allowed").Build());
			});
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);
				app.UseErrorPage();
			}
			else
			{
				//TODO: custom error page
				//app.UseErrorHandler("/Home/Error");
			}

			app.UseStaticFiles();
			app.UseSession();
			app.Use((context, next) =>
			{
				context.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });
				context.Response.Headers.Add("Access-Control-Allow-Headers", new[] { "*" });
				context.Response.Headers.Add("Access-Control-Allow-Methods", new[] { "*" });
				return next();
			});
			app.UseCookieAuthentication(options =>
			{
				options.AutomaticAuthentication = true;
			});
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller}/{action}/{id?}",
					defaults: new { controller = "Home", action = "Index" });
			});
			app.UseSignalR();
		}
	}
}