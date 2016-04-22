using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;
using StackExchange.Redis;

[assembly: OwinStartupAttribute(typeof(SampleWebApp.Startup))]

namespace SampleWebApp
{
    public partial class Startup
    {

        public static string DBConnectionString => ParseVCAP().DBConnectionString;

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            ConfigureDB(app);
        }

        /// <summary>
        /// Get connection string from environment variables and replace it in web.config
        /// </summary>
        /// <param name="app"></param>
        private void ConfigureDB(IAppBuilder app)
        {
            // replace connection string 
            var settings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            var fi = typeof(ConfigurationElement).GetField("_bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            fi?.SetValue(settings, false);
            settings.ConnectionString = DBConnectionString;

        }


        public static string GetRedisConnectionString()
        {
            var settings = ParseVCAP();
            ConfigurationOptions config = new ConfigurationOptions
            {
                EndPoints =
                    {
                        { settings.RedisHost, int.Parse(settings.RedisPort)}
                    },
                CommandMap = CommandMap.Create(new HashSet<string>
                { // EXCLUDE a few commands
                    /*"INFO", "CONFIG", "CLUSTER",
                    "PING", "ECHO", "CLIENT"*/
                }, available: false),
                KeepAlive = 180,
                DefaultVersion = new Version(2, 8, 8),
                Password = settings.RedisPassword
            };

            return config.ToString();
        }

        public static Credentials ParseVCAP()
        {
            Credentials result = new Credentials();
            const string dbServiceName = "mssql-dev";
            const string redisServiceName = "p-redis";
            string vcapServices = Environment.GetEnvironmentVariable("VCAP_SERVICES");

            // if we are in the cloud and DB service was bound successfully...
            if (vcapServices != null)
            {
                dynamic json = JsonConvert.DeserializeObject(vcapServices);
                foreach (dynamic obj in json.Children())
                {

                    switch (((string)obj.Name).ToLowerInvariant())
                    {
                        case dbServiceName:
                            {
                                dynamic credentials = (((JProperty)obj).Value[0] as dynamic).credentials;
                                result.DBHost = credentials?.host;
                                result.DBPort = credentials?.port;
                                result.DBUID = credentials?.username;
                                result.DBPassword = credentials?.password;
                                result.DBConnectionString = credentials?.connectionString;
                            }
                            break;

                        case redisServiceName:
                            {
                                dynamic credentials = (((JProperty)obj).Value[0] as dynamic).credentials;
                                result.RedisHost = credentials?.host;
                                result.RedisPort = credentials?.port;
                                result.RedisPassword = credentials?.password;
                            }
                            break;

                        default:
                            break;
                    }

                }
            }
            return result;
        }

    }

    public struct Credentials
    {
        public string DBUID;
        public string DBHost;
        public string DBPort;
        public string DBPassword;
        public string DBConnectionString;
        public string RedisHost;
        public string RedisPort;
        public string RedisPassword;
    }

}
