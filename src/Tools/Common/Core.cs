﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Common
{
    // TODO: this class does a lot of routing maybe rename it accordingly
    public class Core
    {
        public Core(string modName, string workshopID, string configDirectoryPath, bool logDebugMessages)
        {
            this.Logger = new Logger(modName, logDebugMessages);

            this.ModName = modName;

            if (Pathfinder.FindModRootPath(modName, workshopID, out var path))
            {
                this.RootPath = this.ConfigPath = path;
            }

            if (configDirectoryPath != null)
            {
                this.ConfigPath = Pathfinder.MergePath(this.ConfigPath, configDirectoryPath);
            }

            this.Logger.Log($"Initialized successfully. Version: {Assembly.GetCallingAssembly().GetName().Version}, Path: {this.RootPath}, Config root: {this.ConfigPath}");
        }

        public string ModName { get; private set; }
        public string RootPath { get; private set; }
        public string ConfigPath { get; private set; }

        public Logger Logger;

        public ConfigWatcher<T> WatchConfig<T>(string path, Action<T> callback)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            return new ConfigWatcher<T>(this.ConfigPath, path, callback);
        }

        public T LoadConfig<T>(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            return ConfigHelper.Load<T>(Pathfinder.MergePath(this.ConfigPath, path));
        }

        public void SaveConfig(string path, object data)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            File.WriteAllText(Pathfinder.MergePath(this.ConfigPath, path), JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public bool ConfigExists(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            return File.Exists(Pathfinder.MergePath(this.ConfigPath, path));
        }
    }
}
