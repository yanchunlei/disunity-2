﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Disunity.Core;

using UnityEditor;

using UnityEngine;


namespace Disunity.Editor {

    public class Export {

        private readonly string _modDirectory;
        private readonly ExportSettings _settings;
        private readonly string _tempModDirectory;

        public Export(ExportSettings settings) {
            _settings = settings;
            _tempModDirectory = Path.Combine("Temp", settings.Name);
            _modDirectory = Path.Combine(settings.OutputDirectory, settings.Name);
        }

        public void SetAssetBundle(string assetPath, string variant = "assets") {
            var importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = _settings.Name;
            importer.assetBundleVariant = variant;
        }

        private void CopyAll(string sourceDirectory, string targetDirectory) {
            Directory.CreateDirectory(targetDirectory);

            foreach (var file in Directory.GetFiles(sourceDirectory)) {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(targetDirectory, fileName), true);
            }

            foreach (var subDirectory in Directory.GetDirectories(sourceDirectory)) {
                var targetSubDirectory = Path.Combine(targetDirectory, Path.GetFileName(subDirectory));
                CopyAll(subDirectory, targetSubDirectory);
            }
        }

        private void CreateTempDirectory() {
            if (Directory.Exists(_tempModDirectory)) {
                Directory.Delete(_tempModDirectory, true);
            }

            Debug.Log($"Creating build directory: {_tempModDirectory}");
            Directory.CreateDirectory(_tempModDirectory);
        }

        private List<string> ExportAssemblies(string[] assemblies, string folder) {
            var destinations = new List<string>();

            if (assemblies.Length == 0) {
                return destinations;
            }

            var destinationPath = Path.Combine(_tempModDirectory, folder);
            Directory.CreateDirectory(destinationPath);

            foreach (var asset in assemblies) {
                var asmDef = AsmDef.FromAssetPath(asset);
                var assemblyName = $"{asmDef.name}.dll";
                var modAsmPath = Path.Combine("Library", "ScriptAssemblies", assemblyName);


                if (!File.Exists(modAsmPath)) {
                    Debug.LogError($"{asmDef.name} not found: {modAsmPath}");
                    continue;
                }

                var destination = Path.Combine(destinationPath, assemblyName);
                File.Copy(modAsmPath, destination);
                destinations.Add(Path.Combine(folder, assemblyName));
            }

            return destinations;
        }

        private string[] ExportArtifacts() {
            var artifactFiles = new List<string>();
            var artifactRoot = Path.Combine(_tempModDirectory, "artifacts");

            if (!Directory.Exists(artifactRoot)) {
                Directory.CreateDirectory(artifactRoot);
            }

            foreach (var path in _settings.Artifacts) {
                var relativePath = path.Substring(7);
                var filePath = Path.Combine("artifacts", relativePath);
                var parentPath = Path.GetDirectoryName(filePath);
                artifactFiles.Add(relativePath);
                var fullPath = Path.Combine(_tempModDirectory, filePath);
                Directory.CreateDirectory(Path.Combine(_tempModDirectory, parentPath));
                File.Copy(path, fullPath);
            }

            return artifactFiles.ToArray();
        }

        private void SetContentTypes() {
            _settings.ContentTypes = 0;

            if (_settings.PreloadAssemblies.Length > 0) {
                _settings.ContentTypes |= ContentType.PreloadAssemblies;
            }

            if (_settings.RuntimeAssemblies.Length > 0) {
                _settings.ContentTypes |= ContentType.RuntimeAssemblies;
            }

            if (_settings.Prefabs.Length > 0) {
                _settings.ContentTypes |= ContentType.Prefabs;
            }

            if (_settings.Scenes.Length > 0) {
                _settings.ContentTypes |= ContentType.Scenes;
            }

            if (_settings.Artifacts.Length > 0) {
                _settings.ContentTypes |= ContentType.Artifacts;
            }
        }

        private List<string> ExportRuntimeAssemblies() {
            return ExportAssemblies(_settings.RuntimeAssemblies, "runtime");
        }

        private List<string> ExportPreloadAssemblies() {
            return ExportAssemblies(_settings.PreloadAssemblies, "preload");
        }

        private void ExportModAssets() {
            var destination = Path.Combine(_tempModDirectory, "assetbundles");
            Directory.CreateDirectory(destination);
            _settings.Prefabs.ToList().ForEach(s => SetAssetBundle(s));
            _settings.Scenes.ToList().ForEach(s => SetAssetBundle(s, "scenes"));
            BuildPipeline.BuildAssetBundles(destination, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
        }

        private void SaveMetadata(List<string> preloadAssemblies, List<string> runtimeAssemblies, string[] artifactFiles) {
            var modInfo = new ModInfo(
                _settings.Name,
                _settings.Author,
                _settings.Description,
                _settings.Version,
                Application.unityVersion,
                _settings.ContentTypes,
                preloadAssemblies.ToArray(),
                runtimeAssemblies.ToArray(),
                _settings.PreloadClass,
                _settings.PreloadAssembly,
                _settings.RuntimeClass,
                _settings.RuntimeAssembly,
                _settings.Dependencies,
                artifactFiles);

            ModInfo.Save(Path.Combine(_tempModDirectory, _settings.Name + ".info"), modInfo);
        }

        private void CopyToOutput() {
            try {
                if (Directory.Exists(_modDirectory)) {
                    Directory.Delete(_modDirectory, true);
                }

                Debug.Log($"Copying {_tempModDirectory} => {_modDirectory}");
                CopyAll(_tempModDirectory, _modDirectory);
                Debug.Log($"Export completed: {_modDirectory}");
            }
            catch (Exception e) {
                Debug.LogWarning("There was an issue while copying the mod to the output folder. ");
                Debug.LogWarning(e.Message);
            }
        }

        public void Run() {
            Debug.Log($"Starting export of {_settings.Name}");
            CreateTempDirectory();
            var preloadAssemblies = ExportPreloadAssemblies();
            var runtimeAssemblies = ExportRuntimeAssemblies();
            var artifactFiles = ExportArtifacts();
            ExportModAssets();
            SetContentTypes();
            SaveMetadata(preloadAssemblies, runtimeAssemblies, artifactFiles);
            CopyToOutput();
        }

        public static void ExportMod(ExportSettings settings) {
            var exporter = new Export(settings);
            exporter.Run();
        }

    }

}