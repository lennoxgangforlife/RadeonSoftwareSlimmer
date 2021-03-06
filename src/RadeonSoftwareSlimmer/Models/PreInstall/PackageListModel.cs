﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RadeonSoftwareSlimmer.ViewModels;

namespace RadeonSoftwareSlimmer.Models.PreInstall
{
    public class PackageListModel : INotifyPropertyChanged
    {
        private IEnumerable<PackageModel> _packages;


        public PackageListModel() { }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public IEnumerable<PackageModel> InstallerPackages
        {
            get { return _packages; }
            set
            {
                _packages = value;
                OnPropertyChanged(nameof(InstallerPackages));
            }
        }


        public void LoadOrRefresh(DirectoryInfo installDirectory)
        {
            if (installDirectory != null)
                InstallerPackages = new List<PackageModel>(GetAllInstallerPackages(installDirectory));
        }

        public static void RemovePackage(PackageModel packageToRemove)
        {
            if (packageToRemove == null)
                throw new ArgumentNullException(nameof(packageToRemove));

            JObject fullJson;

            StaticViewModel.AddDebugMessage($"Removing package {packageToRemove.ProductName} from {packageToRemove.File.FullName}");

            using (StreamReader streamReader = new StreamReader(packageToRemove.File.FullName))
            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
            {
                fullJson = (JObject)JToken.ReadFrom(jsonTextReader);
                JToken jToken = fullJson.SelectToken("Packages.Package");
                foreach (JToken token in jToken.Children())
                {
                    PackageModel currentPackage = new PackageModel();
                    currentPackage.Description = token.SelectToken("Info.Description").ToString();
                    currentPackage.ProductName = token.SelectToken("Info.productName").ToString();
                    currentPackage.Url = token.SelectToken("Info.url").ToString();
                    currentPackage.Type = token.SelectToken("Info.ptype").ToString();
                    currentPackage.File = packageToRemove.File;

                    if (currentPackage.Equals(packageToRemove))
                    {
                        token.Remove();
                        break;
                    }
                }
            }

            using (StreamWriter streamWriter = new StreamWriter(packageToRemove.File.FullName))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonTextWriter.Formatting = Formatting.Indented;
                fullJson.WriteTo(jsonTextWriter);
            }
        }


        private static IEnumerable<PackageModel> GetAllInstallerPackages(DirectoryInfo installDirectory)
        {
            FileInfo[] packageFiles =
            {
                new FileInfo($@"{installDirectory.FullName}\Bin64\cccmanifest_64.json"),
                new FileInfo($@"{installDirectory.FullName}\Config\InstallManifest.json"),
            };

            foreach (FileInfo file in packageFiles)
            {
                if (file.Exists)
                {
                    using (StreamReader streamReader = new StreamReader(file.OpenRead()))
                    using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                    {
                        JObject jObject = (JObject)JToken.ReadFrom(jsonTextReader);
                        JToken jToken = jObject.SelectToken("Packages.Package");
                        foreach (JToken token in jToken.Children())
                        {
                            PackageModel package = new PackageModel();
                            package.Description = token.SelectToken("Info.Description").ToString();
                            package.ProductName = token.SelectToken("Info.productName").ToString();
                            package.Url = token.SelectToken("Info.url").ToString();
                            package.Type = token.SelectToken("Info.ptype").ToString();
                            package.File = file;

                            StaticViewModel.AddDebugMessage($"Found package {package.ProductName} in {package.File.FullName}");
                            yield return package;
                        }
                    }
                }
            }
        }
    }
}
