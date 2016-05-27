using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;
using Sitecore.AdvancedSiteMap.Component;
using Sitecore.AdvancedSiteMap.Constants;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;

namespace Sitecore.AdvancedSiteMap
{
    public class SiteMapBuilder
    {
        private Database GetTargetDatabase()
        {
            SiteMapConfig siteMapConfig = new SiteMapConfig();
            return Factory.GetDatabase(siteMapConfig.targetDatabaseName);
        }

        public void BuildSiteMap(object sender, EventArgs args)
        {
            BuildSiteMap();
        }

        public void BuildSiteMap()
        {
            SiteMapConfig siteMapConfig = new SiteMapConfig();
            if (siteMapConfig.enableRefreshSiteMapOnPublish)
                GenerateSiteMap();

            if (siteMapConfig.enableSendXMLToSearchEngines)
                SendSiteMapTOSearchEngines();
        }

        private void GenerateSiteMap()
        {
            try
            {
                SiteMapConfig siteMapConfig = new SiteMapConfig();
                if (siteMapConfig.definedSites == null || !siteMapConfig.definedSites.Any())
                    return;

                if (siteMapConfig.targetDatabaseName == string.Empty)
                    return;

                foreach (var site in siteMapConfig.definedSites)
                {
                    if (site.Fields[SiteItemFields.SiteName] == null || string.IsNullOrEmpty(site.Fields[SiteItemFields.SiteName].Value))
                        continue;

                    Sitecore.Sites.SiteContext _site = Factory.GetSite(site.Fields[SiteItemFields.SiteName].Value);
                    if (_site == null)
                        continue;

                    Item _root = GetTargetDatabase().GetItem(_site.StartPath);
                    if (_root == null)
                        continue;

                    string siteHostName;
                    if (string.IsNullOrEmpty(_site.TargetHostName))
                    {
                        var hostArray = _site.HostName.Split('|');
                        siteHostName = hostArray.FirstOrDefault();
                    }
                    else
                    {
                        siteHostName = _site.TargetHostName;
                    }

                    bool useServerUrlOverride = site.Fields[SiteItemFields.ServerURL] != null && !string.IsNullOrEmpty(site.Fields[SiteItemFields.ServerURL].Value);
                    string serverUrlOverrideUrl = site.Fields[SiteItemFields.ServerURL].Value;

                    StringBuilder sbSiteMap = new StringBuilder();
                    sbSiteMap.Append("<urlset xmlns='http://www.sitemaps.org/schemas/sitemap/0.9' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd'>");

                    List<Item> siteMapItems = _root.Axes.GetDescendants().Where(P => P.Fields["Show In XML SiteMap"] != null && P.Fields["Show In XML SiteMap"].Value == "1").ToList();
                    if (siteMapItems != null && siteMapItems.Any())
                    {
                        if (_root.Fields["Show In XML SiteMap"].Value == "1")
                            siteMapItems.Add(_root);
                        
                        var options = global::Sitecore.Links.LinkManager.GetDefaultUrlOptions();
                        options.LanguageEmbedding = LanguageEmbedding.Always;
                        options.SiteResolving = true;
                        
                        
                        // get langauges 
                        List<Language> _availbleLangauges = null;
                        if (siteMapConfig.multilingualSiteMapXML)
                        {
                            Item _languagesRoot = GetTargetDatabase().GetItem(Guids.Items.LangaugesRootItem);
                            _availbleLangauges = _languagesRoot.Axes.GetDescendants().Where(P => P.Fields["Iso"].Value != string.Empty).Select(P => Language.Parse(P.Fields["Iso"].Value)).ToList();
                        }
                        else
                        {
                            _availbleLangauges = new List<Language>() { Language.Parse("en") };
                        }

                        XmlDocument doc = new XmlDocument();
                        foreach (var langauge in _availbleLangauges)
                        {
                            options.Language = langauge;
                            options.EmbedLanguage(langauge);
                            options.Site = _site;

                            foreach (var item in siteMapItems)
                            {
                                options.AlwaysIncludeServerUrl = !useServerUrlOverride;

                                //to resolve issues with multisite link resolution here, set the rootPath="#" on the publisher site in web.config
                                //and also add scheme="http" to the Site Definition for your site
                                string url = LinkManager.GetItemUrl(item, options);

                                // Add URL override to url
                                if (useServerUrlOverride)
                                {
                                    url = string.Format("{0}{1}", serverUrlOverrideUrl, url);
                                }

                                //handle where scheme="http" has not been added to Site Definitions
                                if (url.StartsWith("://"))
                                {
                                    url = url.Replace("://", "");                                    
                                }

                                if (!url.StartsWith("http://") || !url.StartsWith("https://"))
                                {
                                    url = "http://" + url;
                                }

                                string lastUpdated = DateUtil.IsoDateToDateTime(item.Fields[Sitecore.FieldIDs.Updated].Value).ToString("yyyy-MM-ddTHH:mm:sszzz");
                                string FrequencyChange = "yearly";
                                if (item.Fields[SiteMapFields.FrequencyChange] != null)
                                {
                                    if (!string.IsNullOrEmpty(item.Fields[SiteMapFields.FrequencyChange].Value))
                                    {
                                        Item _FrequencyChange = GetTargetDatabase().GetItem(item.Fields[SiteMapFields.FrequencyChange].Value);
                                        if (_FrequencyChange != null)
                                            FrequencyChange = _FrequencyChange.Name;

                                    }
                                }

                                string Priority = "0.0";
                                if (item.Fields[SiteMapFields.Priority] != null)
                                {
                                    if (!string.IsNullOrEmpty(item.Fields[SiteMapFields.Priority].Value))
                                    {
                                        Item _priority = GetTargetDatabase().GetItem(item.Fields[SiteMapFields.Priority].Value);
                                        if (_priority != null && _priority.Fields["Value"] != null && !string.IsNullOrEmpty(_priority.Fields["Value"].Value))
                                            Priority = _priority.Fields["Value"].Value;
                                    }
                                }

                                sbSiteMap.Append("<url>");
                                sbSiteMap.Append("<loc>");
                                sbSiteMap.Append(url);
                                sbSiteMap.Append("</loc>");
                                sbSiteMap.Append("<lastmod>");
                                sbSiteMap.Append(lastUpdated);
                                sbSiteMap.Append("</lastmod>");
                                sbSiteMap.Append("<changefreq>");
                                sbSiteMap.Append(FrequencyChange);
                                sbSiteMap.Append("</changefreq>");
                                sbSiteMap.Append("<priority>");
                                sbSiteMap.Append(Priority);
                                sbSiteMap.Append("</priority>");
                                sbSiteMap.Append("</url>");
                            }
                        }
                        sbSiteMap.Append("</urlset>");

                        string fileName = "SiteMap.xml";
                        if (site.Fields[SiteItemFields.SitemMapXMLFilename] != null &&
                            !string.IsNullOrEmpty(site.Fields[SiteItemFields.SitemMapXMLFilename].Value))
                            fileName = site.Fields[SiteItemFields.SitemMapXMLFilename].Value;

                        doc.LoadXml(sbSiteMap.ToString());
                        string xmlFilePath = MainUtil.MapPath("/" + fileName);
                        doc.Save(xmlFilePath);

                        if (site.Fields[SiteItemFields.AddToRobotFile] != null)
                        {
                            // Base URL
                            var serverUrl = serverUrlOverrideUrl;

                            if (!useServerUrlOverride)
                            {
                                // handles multi site, single site with hostname defined and single site with no hostname defined
                                var item = siteMapItems.FirstOrDefault();
                                Uri mySite = new Uri(LinkManager.GetItemUrl(item, new UrlOptions {LanguageEmbedding = LanguageEmbedding.Never, AlwaysIncludeServerUrl = true }));
                                serverUrl = mySite.Scheme + Uri.SchemeDelimiter + mySite.Host;
                            }

                            serverUrl = serverUrl.EndsWith("/") ? serverUrl : serverUrl + "/"; // Ensure URL is built correctly

                            Sitecore.Data.Fields.CheckboxField _AddToRobotFile = site.Fields[SiteItemFields.AddToRobotFile];
                            if (_AddToRobotFile != null)
                            {
                                if (_AddToRobotFile.Checked)
                                    AddSitemapToRobots(string.Format("{0}{1}", serverUrl, fileName));
                            }
                        }

                        Sitecore.Web.UI.Sheer.SheerResponse.Alert("SiteMap has been generated successfully");
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, "SiteMapBuilder - GenerateSiteMap method");
            }
        }

        private void SendSiteMapTOSearchEngines()
        {
            try
            {
                SiteMapConfig siteMapConfig = new SiteMapConfig();
                if (siteMapConfig.enabledSearchEngines == null)
                    return;

                if (!siteMapConfig.enabledSearchEngines.Any())
                    return;

                if (siteMapConfig.definedSites == null)
                    return;

                if (!siteMapConfig.definedSites.Any())
                    return;

                foreach (var engine in siteMapConfig.enabledSearchEngines)
                {
                    foreach (var site in siteMapConfig.definedSites)
                    {
                        string SearchEngineRequstUrl = engine.Fields[SiteMapSearchEngineFields.SearchEngineRequestUrl].Value;
                        string siteMapUrl = HttpUtility.HtmlEncode(site.Fields[SiteItemFields.ServerURL].Value + "/" + site.Fields[SiteItemFields.SitemMapXMLFilename].Value);


                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SearchEngineRequstUrl + siteMapUrl);
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                throw new Exception("Submit SsiteMap error. Engine name is : " + engine.Name);
                            }
                        }
                        catch
                        {
                            throw new Exception("404 error. Engine name is : " + engine.Name);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, "SiteMapBuilder - SendSiteMapTOSearchEngines");
            }
        }

        public void AddSitemapToRobots(string xmlFileName)
        {
            try
            {
                string _RobotsFilePath = MainUtil.MapPath("/" + "robots.txt");
                StringBuilder sbRobots = new StringBuilder(string.Empty);
                if (File.Exists(_RobotsFilePath))
                {
                    StreamReader reader = new StreamReader(_RobotsFilePath);
                    sbRobots.Append(reader.ReadToEnd());
                    reader.Close();
                }
                StreamWriter writer = new StreamWriter(_RobotsFilePath, false);
                string _stringToAppend = "Sitemap: " + xmlFileName;
                if (!sbRobots.ToString().Contains(_stringToAppend))
                {
                    sbRobots.AppendLine(_stringToAppend);
                }
                writer.Write(sbRobots.ToString());
                writer.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, "SiteMapBuilder - AddSitemapToRobots method");
            }
        }
    }
}
