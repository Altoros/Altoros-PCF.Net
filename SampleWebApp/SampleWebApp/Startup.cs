using System.Configuration;
using System.Reflection;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;

[assembly: OwinStartupAttribute(typeof(SampleWebApp.Startup))]

namespace SampleWebApp
{
    public partial class Startup
    {
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
            string dbServiceName = ConfigurationManager.AppSettings["DBServiceName"];
            string vcapServices = System.Environment.GetEnvironmentVariable("VCAP_SERVICES");

            // if we are in the cloud and DB service was bound successfully...
            if (vcapServices != null)
            {
                dynamic json = JsonConvert.DeserializeObject(vcapServices);
                foreach (dynamic obj in json.Children())
                {
                    if (((string)obj.Name).ToLowerInvariant().Contains(dbServiceName))
                    {
                        dynamic credentials = (((JProperty)obj).Value[0] as dynamic).credentials;

                        // replace connection string 
                        var settings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
                        var fi = typeof(ConfigurationElement).GetField(
                                      "_bReadOnly",
                                      BindingFlags.Instance | BindingFlags.NonPublic);
                        fi?.SetValue(settings, false);
                        settings.ConnectionString = credentials?.connectionString;

                        break;
                    }
                }
            }
        }
    }
}
