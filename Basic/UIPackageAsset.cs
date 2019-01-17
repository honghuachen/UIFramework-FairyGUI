using System;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;
using Core.ResourceRender;

/*! CGameUIAsset
\brief
UI prefab对象
*/
namespace Core.UIFramework
{
    /// <summary>
    /// FairyGUI 描述文件
    /// </summary>
    public class UIPackageAsset : IRenderObject
    {
        private UIPackageDependenceAsset dependenceAsset = null;
        private UIManager uiMrg = null;
        public UIPackage package = null;
        public int ReferenceCount = 0;
        Map<string, UIPackageDependenceObject> dependences = new Map<string, UIPackageDependenceObject>();
        Map<string, UIPackageDependenceObject> loading = new Map<string, UIPackageDependenceObject>();
        List<string> loadingWins = new List<string>();

        public UIPackageAsset(string packageName,UIManager uiMrg, UIPackageDependenceAsset dependenceAsset)
        {
            this.uiMrg = uiMrg;
            this.dependenceAsset = dependenceAsset;

            List<string> dpFiles = dependenceAsset.GetDependenceFiles(packageName);
            for(int i = 0;i < dpFiles.Count; i++)
            {
                string[] fileNames = dpFiles[i].Split('.');
                string assetPath = string.Format("{0}/{1}.fui", uiMrg.windowFolder, fileNames[0]).ToLower();
                UIPackageDependenceObject depence = RenderFactory.CreateInstance<UIPackageDependenceObject>(assetPath, this);
                loading.Add(assetPath, depence);
            }
        }

        protected override void OnCreate(UnityEngine.Object asset)
        {
            if (asset == null)
                return;
            TextAsset desc = asset as TextAsset;
            package = UIPackage.AddPackage(desc.bytes, null, (string name, string extension, Type type, out DestroyMethod destroyMethod) =>
            {
                destroyMethod = DestroyMethod.None;
                string assetPath = string.Format("{0}/{1}_{2}.fui", uiMrg.windowFolder, package.name, name).ToLower();
                UIPackageDependenceObject depence = null;
                if (!loading.TryGetValue(assetPath, out depence))
                {
                    if (!dependences.TryGetValue(assetPath, out depence))
                    {
                        depence = RenderFactory.CreateInstance<UIPackageDependenceObject>(assetPath, this);
                        loading.Add(assetPath, depence);
                    }
                }

                return depence.asset;
            });
        }

        public void LoadWindow(string winName)
        {
            loadingWins.Add(winName);
        }

        /// <summary>
        /// 依赖包
        /// </summary>
        /// <returns></returns>
        public List<string> GetDependencePackages()
        {
            if (package == null)
                return new List<string>();
            List<string> dependencePackageNames = new List<string>();
            Dictionary<string,string>[] dps = package.dependencies;
            for(int i = 0;i < dps.Length; i++)
            {
                dependencePackageNames.Add(dps[i]["name"]);
            }

            return dependencePackageNames;
        }

        List<string> loadComplets = new List<string>();
        protected override void OnUpdate()
        {
            if (!complete || package == null)
                return;

            if (loadingWins.Count > 0)
            {
                for (int i = 0; i < loadingWins.Count; i++)
                {
                    ReferenceCount++;
                    CreateWindowAsync(package.name, loadingWins[i]);
                }
                loadingWins.Clear();
            }

            if (loading.Count > 0)
            {
                for (loading.Begin(); loading.Next();)
                {
                    var dependence = loading.Value;
                    if (dependence.complete)
                    {
                        dependences.Add(dependence.name, dependence);
                        loadComplets.Add(loading.Key);
                    }
                }
                for (int i = 0; i < loadComplets.Count; i++)
                    loading.Remove(loadComplets[i]);
                loadComplets.Clear();
                if (loading.Count == 0)
                    package.ReloadAssets();
            }
        }

        private void CreateWindowAsync(string packageName, string name)
        {
            GameObject win = new GameObject(name);
            Utils.NormalizeTransform(win.transform);
            if (!win.activeSelf)
                win.SetActive(true);

            FairyGUI.UIPanel panel = win.AddComponent<FairyGUI.UIPanel>();
            panel.container.renderMode = RenderMode.ScreenSpaceOverlay;
            panel.container.fairyBatching = true;
            panel.container.SetChildrenLayer(uiMrg.windowRoot.gameObject.layer);
            panel.packageName = packageName;
            panel.componentName = name;

            LuaWindow window = win.AddComponent<LuaWindow>();
            window.Setup(uiMrg,this, name);
            panel.CreateUI();
            Utils.AttachChild(uiMrg.windowRoot, win.transform, true);

            //UIPackage.CreateObjectAsync(packageName, name, (GObject result) =>
            //{
            //    GameObject winObj = result.displayObject.gameObject;
            //    if (!winObj)
            //    {
            //        LOG.Debug(string.Format("load window '{0}' fail.", name));
            //        return;
            //    }

            //    CClientCommon.AttachChild(uiMrg.windowRoot, winObj.transform);
            //    CClientCommon.NormalizeTransform(winObj.transform);
            //    if (!winObj.activeSelf)
            //        winObj.SetActive(true);

            //    Type winType = uiMrg.GetWindowType(name);
            //    if (winType == null)
            //    {
            //        return;
            //    }

            //    FairyGUI.UIPanel panel = winObj.AddComponent<FairyGUI.UIPanel>();
            //    panel.container.renderMode = RenderMode.ScreenSpaceOverlay;
            //    panel.container.fairyBatching = true;

            //    UIWindow window = winObj.AddComponent(winType) as UIWindow;
            //    window.Setup(uiMrg, name);
            //});
        }

        protected override void OnDestroy()
        {
            UnLoadAssets(null);
        }

        public bool UnLoadAssets(string winName)
        {
            ReferenceCount--;
            if (ReferenceCount <= 0 || string.IsNullOrEmpty(winName))
            {
                for (loading.Begin(); loading.Next();)
                {
                    loading.Value.Destroy();
                }
                loading.Clear();
                for (dependences.Begin(); dependences.Next();)
                {
                    dependences.Value.Destroy();
                }
                dependences.Clear();
                loadingWins.Clear();
                package.UnloadAssets();
                return true;
            }
            else if (loadingWins.Contains(winName))
            {
                loadingWins.Remove(winName);
            }
            return false;
        }
    }

    /// <summary>
    /// FairyGUI package 依赖资源
    /// </summary>
    public class UIPackageDependenceObject : IRenderObject
    {
        protected override void OnCreate(UnityEngine.Object asset) { }
    }

    /// <summary>
    /// fairygui package dependences config
    /// </summary>
    public class UIPackageDependenceAsset : IRenderObject
    {
        private Map<string, UIPackageDependence> packageDependences = null;
        protected override void OnCreate(UnityEngine.Object asset)
        {
            TextAsset config = asset as TextAsset;
            packageDependences = LitJson.JsonMapper.ToObject<Map<string, UIPackageDependence>>(config.text);
        }

        public List<string> GetDependencePackages(string packageName)
        {
            if (packageDependences == null)
            {
                LOG.LogError("'UIPackageDependenceAsset' is null.Get package dependences failed.");
                return new List<string>();
            }

            UIPackageDependence dependence = null;
            if (packageDependences.TryGetValue(packageName, out dependence))
                return dependence.dependencePackages;
            return new List<string>();
        }


        public List<string> GetDependenceFiles(string packageName)
        {
            if (packageDependences == null)
            {
                LOG.LogError("'UIPackageDependenceAsset' is null.Get package dependences failed.");
                return new List<string>();
            }

            UIPackageDependence dependence = null;
            if (packageDependences.TryGetValue(packageName, out dependence))
                return dependence.dependenceFiles;
            return new List<string>();
        }
    }

    public class UIPackageDependence
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<string> dependencePackages = new List<string>();
        public List<string> dependenceFiles= new List<string>();

        public UIPackageDependence() { }
        public UIPackageDependence(string packageID, string packageName, List<string> dependencePackages, List<string> dependenceFiles)
        {
            id = packageID;
            name = packageName;
            this.dependencePackages = dependencePackages;
            this.dependenceFiles = dependenceFiles;
        }
    }
}