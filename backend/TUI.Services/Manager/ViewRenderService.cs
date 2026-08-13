using Microsoft.AspNetCore.Http;
using RazorLight;
using RazorLight.Razor;
using TUI.Services.DBModel;
using TUI.Services.Model;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;

namespace TUI.Services.Manager
{
    public interface IViewRenderService:IScopeDependency
    {
        string RenderView(string template, Object objModel, ExpandoObject viewbag = null);
        string RenderViewFromFile(string file, Object objModel, ExpandoObject viewbag = null);
    }
    public class ViewRenderService : IViewRenderService
    {

        public string RenderView(string template, Object objModel,  ExpandoObject viewbag = null)
        {
            
            var engine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(PriceBrand))
                //.SetOperatingAssembly(typeof(PriceBrand).Assembly)
                //.SetOperatingAssembly(typeof(List<>).Assembly)
                .UseMemoryCachingProvider().Build();
            if (viewbag == null)
            {
                viewbag = new ExpandoObject();
                IDictionary<string, object> dictionary = (IDictionary<string, object>)viewbag;
                dictionary["SystemSetting"] = App.SystemSetting;

            }
            //template="<div class=\"row title\">@Model.FooterDate</div>";
            string result = engine.CompileRenderStringAsync("templateKey", template, objModel, viewbag).GetAwaiter().GetResult();
            return result;
        }
        public string RenderViewFromFile(string file, Object objModel, ExpandoObject viewbag = null)
        {
            if (File.Exists(file)==false)
            {
                throw new FileNotFoundException(file);
            }
           

            using (System.IO.StreamReader sr = new System.IO.StreamReader(file))
            {
                var template = sr.ReadToEnd();
                return RenderView(template, objModel, viewbag);
            }
        }
    
    }
}
