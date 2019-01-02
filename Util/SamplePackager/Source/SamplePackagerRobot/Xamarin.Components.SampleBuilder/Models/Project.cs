﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Xamarin.Components.SampleBuilder.Enums;

namespace Xamarin.Components.SampleBuilder.Models
{
    public class Project
    {
        private string _path;

        public string Name { get; set; }

        public ProjectType Type { get; set; }

        public List<ProjectReference> ProjectReferences { get; set; }

        public string PackageId { get; set; }

        public string PackageVersion { get; set; }

        public Project(string path)
        {
            _path = path;

            ProjectReferences = new List<ProjectReference>();

            Name = Path.GetFileNameWithoutExtension(_path);
        }

        internal void Build()
        {
            var xml = new XmlDocument();
            xml.Load(_path);

            var loop = 0;

            XmlNode projectNode = null;

            while (projectNode == null && xml.ChildNodes.Count > loop)
            {
                var node = xml.ChildNodes[loop];

                if (node.Name.Equals("Project", StringComparison.OrdinalIgnoreCase))
                {
                    projectNode = node;

                    break;

                }
                else if (node.InnerXml.ToLower().Contains("project"))
                {
                    projectNode = xml.ChildNodes[loop];

                }

                loop++;
            }
            
            var toolsVersion = projectNode.Attributes["ToolsVersion"];
            var sdkVersion = projectNode.Attributes["Sdk"];

            if (toolsVersion != null && sdkVersion == null)
                Type = ProjectType.Classic;
            else if (toolsVersion == null & sdkVersion != null)
                Type = ProjectType.SDK;

            switch (Type)
            {
                case ProjectType.SDK:
                    {
                        FindProjectsSdk(projectNode);


                        FindSdkNugetDetails(xml);
                    }
                    break;
                case ProjectType.Classic:
                    {
                        FindProjectsClassic(projectNode);
                    }
                    break;
                default:
                    {
                        throw new Exception("Unable to determine the type of csproj");
                    }
            }
        }

        private void FindSdkNugetDetails(XmlDocument node)
        {
            XmlNode propsProjectNode = null;

            foreach (XmlNode aNode in node.ChildNodes)
            {
                propsProjectNode = FindParentNode(aNode, "GeneratePackageOnBuild");

                if (propsProjectNode != null)
                    break;
            }

            if (propsProjectNode != null)
            {
                var packageName = string.Empty;
                var versionNo = string.Empty;

                var pkgId = FindChildNode(propsProjectNode, "PackageId");

                if (pkgId == null)
                {
                    pkgId = FindChildNode(propsProjectNode, "AssemblyName");

                    if (pkgId == null)
                    {
                        packageName = Name;
                    }
                    else
                    {
                        packageName = pkgId.InnerText;
                    }
                }
                else
                {
                    packageName = pkgId.InnerText;
                }

                var versonNo = string.Empty;

                //
                var verNode = FindChildNode(propsProjectNode, "Version");

                if (verNode == null)
                {
                    verNode = FindChildNode(propsProjectNode, "AssemblyVersion");

                    if (verNode == null)
                    {
                        versionNo = "1.0.0";
                    }
                    else
                    {
                        versionNo = verNode.InnerText;
                    }
                }
                else
                {
                    versionNo = verNode.InnerText;
                }

                PackageId = packageName;
                PackageVersion = versionNo;
            }
        }

        private XmlNode FindChildNode(XmlNode node, string nodeName)
        {
            foreach (XmlNode aChild in node)
            {
                if (aChild.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                    return aChild;
            }

            return null;
        }

        private void FindProjectsSdk(XmlNode node)
        {
            XmlNode lstProjectNode = null;

            foreach (XmlNode aNode in node.ChildNodes)
            {
                lstProjectNode = FindParentNode(aNode, "ProjectReference");

                if (lstProjectNode != null)
                    break;
            }

            if (lstProjectNode != null)
            {
                foreach (XmlNode aProject in lstProjectNode.ChildNodes)
                {
                    var link = aProject.Attributes["Include"];

                    if (link != null)
                    {
                        var projLink = link.InnerText;

                        var basePath = System.IO.Path.GetDirectoryName(_path);

                        var projPath = Path.Combine(basePath, projLink);

                        var fileInfo = new FileInfo(projPath);

                        var path = fileInfo.FullName;

                        ProjectReferences.Add(new ProjectReference()
                        {
                            FullPath = path,
                        });
                    }
                }
            }

        }

        private void FindProjectsClassic(XmlNode node)
        {
            XmlNode lstProjectNode = null;

            foreach (XmlNode aNode in node.ChildNodes)
            {
                lstProjectNode = FindParentNode(aNode, "ProjectReference");

                if (lstProjectNode != null)
                    break;
            }

            if (lstProjectNode != null)
            {

                foreach (XmlNode aProject in lstProjectNode.ChildNodes)
                {
                    var projectId = aProject.ChildNodes[0].InnerText;
                    var projectName = aProject.ChildNodes[1].InnerText;

                    ProjectReferences.Add(new ProjectReference()
                    {
                        Id = projectId,
                        Name = projectName,
                    });
                }
            }
        }

        private XmlNode FindParentNode(XmlNode node, string nodeName)
        {
            if (ContainsNode(node, nodeName))
                return node;

            XmlNode exNode = null;

            foreach (XmlNode aNode in node.ChildNodes)
            {
                exNode = FindParentNode(aNode, nodeName);

                if (exNode != null)
                    break;
            }

            return exNode;
        }

        private bool ContainsNode(XmlNode node, string nodeName)
        {
            if (!node.HasChildNodes)
                return false;

            foreach (XmlNode aNode in node.ChildNodes)
            {
                if (aNode.Name.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
