using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.AdvancedSiteMap.Constants;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Sitecore.AdvancedSiteMap.Component
{
    public class SiteMapConfig
    {
        private string TargetDatabaseName;
        private bool EnableRefreshSiteMapOnPublish;
        private bool MultilingualSiteMapXML;
        private bool EnableSendXMLToSearchEngines;
        private List<Item> EnabledSearchEngines;
        private List<Item> DefinedSites;

        public string targetDatabaseName
        {
            get { return TargetDatabaseName; }
            set { TargetDatabaseName = value; }
        }

        public bool enableRefreshSiteMapOnPublish
        {
            get { return EnableRefreshSiteMapOnPublish; }
            set { EnableRefreshSiteMapOnPublish = value; }
        }

        public bool multilingualSiteMapXML
        {
            get { return MultilingualSiteMapXML; }
            set { MultilingualSiteMapXML = value; }
        }


        public bool enableSendXMLToSearchEngines
        {
            get { return EnableSendXMLToSearchEngines; }
            set { EnableSendXMLToSearchEngines = value; }
        }

        public List<Item> enabledSearchEngines
        {
            get { return EnabledSearchEngines; }
            set { EnabledSearchEngines = value; }
        }

        public List<Item> definedSites
        {
            get { return DefinedSites; }
            set { DefinedSites = value; }
        }

        public SiteMapConfig()
        {
            InitilizeConfig();
        }

        private void InitilizeConfig()
        {
            try
            {
                Database tempdb = Sitecore.Configuration.Factory.GetDatabase("web"); //don't use Master as this does not exist in CD Server environments
                Item configItem = tempdb.GetItem(Constants.Guids.Items.AdvancedSiteMapConfigItemId);
                if (configItem == null)
                    throw new Exception("Advanced SiteMap config item was not found.");

                this.targetDatabaseName = configItem.Fields[ConfigFieldsNames.TargetDatabaseName].Value;
                
                CheckboxField _EnableRefreshXMLONPublish = configItem.Fields[ConfigFieldsNames.EnableRefreshXMLONPublish];
                if (_EnableRefreshXMLONPublish != null)
                    this.enableRefreshSiteMapOnPublish = _EnableRefreshXMLONPublish.Checked;

                CheckboxField _MultilingualSiteMapXML = configItem.Fields[ConfigFieldsNames.MultilingualSiteMapXML];
                if (_MultilingualSiteMapXML != null)
                    this.multilingualSiteMapXML = _MultilingualSiteMapXML.Checked;

                CheckboxField _EnableSendXMLToSearchEngines = configItem.Fields[ConfigFieldsNames.EnableSendXMLToSearchEngines];
                if (_EnableSendXMLToSearchEngines != null)
                    this.enableSendXMLToSearchEngines = _EnableSendXMLToSearchEngines.Checked;

                List<Item> _EnabledSearchEngines = configItem.Axes.GetDescendants().Where(p => p.TemplateID == Sitecore.AdvancedSiteMap.Constants.Guids.Templates.SiteMapSearchEngineConfigTemplateId &&
                    p.Fields[SiteMapSearchEngineFields.Enabled].Value == "1").ToList();

                if (_EnabledSearchEngines != null && _EnabledSearchEngines.Any())
                    this.enabledSearchEngines = _EnabledSearchEngines;

                List<Item> _DefinedSites = configItem.Axes.GetDescendants().Where(p => p.TemplateID == Sitecore.AdvancedSiteMap.Constants.Guids.Templates.SiteMapSiteTemplateId).ToList();

                if (_DefinedSites != null && _DefinedSites.Any())
                    this.definedSites = _DefinedSites;

            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(ex.Message, "Class : SiteMapConfig");
            }
        }

    }
}
