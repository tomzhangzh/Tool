using Furion;
using System.Reflection;

namespace TUI.Web.Entry
{
    public class SingleFilePublish : ISingleFilePublish
    {
        public Assembly[] IncludeAssemblies()
        {
            return Array.Empty<Assembly>();
        }

        public string[] IncludeAssemblyNames()
        {
            return new[]
            {
            "TUI.Application",
            "TUI.Core",
            "TUI.Web.Core"
        };
        }
    }
}