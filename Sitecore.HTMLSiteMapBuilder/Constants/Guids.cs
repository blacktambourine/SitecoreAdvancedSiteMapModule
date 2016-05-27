using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data;

namespace Sitecore.AdvancedSiteMap.Constants
{
    public class Guids
    {
        public static class Items
        {
            public static ID AdvancedSiteMapConfigItemId = new ID("{38ACC950-E87F-4A5C-9271-C55C4336AAAB}");
            public static ID LangaugesRootItem = new ID("{64C4F646-A3FA-4205-B98E-4DE2C609B60F}");
        }

        public static class Templates
        {
            public static ID SiteMapSearchEngineConfigTemplateId = new ID("{55C6049A-09B7-488A-A363-46FB4A40819A}");
            public static ID SiteMapSiteTemplateId = new ID("{9F454752-3C73-45F0-A03A-1B8EFD9F7F9E}");
        }
    }
}
