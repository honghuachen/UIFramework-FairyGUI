using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Core.ResourceRender;

#if true
namespace Core.UIFramework
{
    /********************************************************************
        created:	2017/07/10
        created:	10:7:2017   15:00
        filename: 	E:\ssss\trunk\client\Assets\Script\UIFramework\Basic\UIManager.cs
        file path:	E:\ssss\trunk\client\Assets\Script\UIFramework\Basic
        file base:	UIManager
        file ext:	cs
        author:		陈德怀
	
        purpose:	此类是UI框架的管理类，管理所有窗口的创建、关闭、刷新、周期等。
     * 提供窗口周期性触发事件，外部逻辑可根据逻辑需要监听相应窗口的周期性事件。
     * 提供UI的打开、关闭、刷新、发送命令、获取窗口等UI操作的基础接口。
     * 提供Dialog/Wait等常用接口
    *********************************************************************/

    public class UIManager : CLoopObject {
        private Action OnCompleted = null;
        public UIManager() { }
        public UIManager(Action OnCompleted)
        {
            this.OnCompleted = OnCompleted;
        }

        public override void Initialize()
        {
            GameObject uiRoot = GameObject.Find("FairyGUI");
            if (uiRoot == null)
                throw new Exception("UIRoot is null.please create it.");
            this.windowRoot = uiRoot.transform.Find("Windows");
            this.windowFolder = "res/ui/fairygui";
            this.windowCamera = uiRoot.transform.GetComponentInChildren<Camera>();

            m_Instance = this;
            ms_MutexGroups.Add(UIWindowGroup.Window);
            AddRollbackGroup(UIWindowGroup.Window);

            m_PackageDependenceAsset = RenderFactory.CreateInstance<UIPackageDependenceAsset>(string.Format("{0}/FairyGUIPackageDependences.fui", this.windowFolder),null);
        }

        #region Public Variable
        //---------------------------------------------------------------------
        public Transform windowRoot = null;

        //---------------------------------------------------------------------
        public Transform modelViewRoot = null;

        //---------------------------------------------------------------------
        public string windowFolder = string.Empty;

        public Camera windowCamera = null;
        #endregion

        #region Public Property
        //---------------------------------------------------------------------
        public static UIManager Instance {
            get {
                return m_Instance;
            }
        }
        #endregion

        #region Public Event
        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowLoadedEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowPreparedEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowInitializedEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowRefreshEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowCommandEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowPreShowEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowPostShowEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowPreHideEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowPostHideEvent;

        //---------------------------------------------------------------------
        public event Action<UIWindow> WindowShutdownEvent;
        #endregion

        #region Event Method
        //---------------------------------------------------------------------
        internal void TouchWindowLoaded(UIWindow win, UIPackageAsset package) {
            if (Instance != null) {
                Instance.AddWindow(win, package);
            }

            if (WindowLoadedEvent != null) {
                WindowLoadedEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowPreparedEvent(UIWindow win) {
            if (WindowPreparedEvent != null) {
                WindowPreparedEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowInitializedEvent(UIWindow win) {
            if (WindowInitializedEvent != null) {
                WindowInitializedEvent(win);
            }

            List<PendingInfo> pendingInfoList = null;
            if (!Instance.m_PendingWindows.TryGetValue(
                win.WindowName, out pendingInfoList)) {
                return;
            }

            for (int index = 0; index < pendingInfoList.Count; ++index) {
                PendingInfo pendingInfo = pendingInfoList[index];
                if (pendingInfo.handler != null) {
                    pendingInfo.handler(win, pendingInfo.data);
                }
            }

            Instance.m_PendingWindows.Remove(win.WindowName);
        }

        //---------------------------------------------------------------------
        internal void TouchWindowRefreshEvent(UIWindow win) {
            if (WindowRefreshEvent != null) {
                WindowRefreshEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowCommandEvent(UIWindow win) {
            if (WindowCommandEvent != null) {
                WindowCommandEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowPreShowEvent(UIWindow win) {
            if (WindowPreShowEvent != null) {
                WindowPreShowEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowPostShowEvent(UIWindow win) {
            if (WindowPostShowEvent != null) {
                WindowPostShowEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowPreHideEvent(UIWindow win) {
            if (WindowPreHideEvent != null) {
                WindowPreHideEvent(win);
            }

            if (Instance != null) {
                Instance.ExecutePopRollback(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowPostHideEvent(UIWindow win) {
            if (WindowPostHideEvent != null) {
                WindowPostHideEvent(win);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowShutdownEvent(UIWindow win) {
            if (!Instance.m_ShutdownWindows.Contains(win.WindowName)) {
                Instance.m_ShutdownWindows.Add(win.WindowName);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowRequestShow(UIWindow win) {
            if (Instance == null) {
                return;
            }

            Instance.RequestPushRollback(win);
            if (Instance.m_ShutdownWindows.Contains(win.WindowName)) {
                Instance.m_ShutdownWindows.Remove(win.WindowName);
            }
        }

        //---------------------------------------------------------------------
        internal void TouchWindowRequestHide(UIWindow win) {
            if (Instance == null) {
                return;
            }

            Instance.RequestPopRollback(win);
        }
        #endregion

        #region Public Method
        //---------------------------------------------------------------------
        public void AddMutexGroup(UIWindowGroup group) {
            if (!ms_MutexGroups.Contains(group)) {
                ms_MutexGroups.Add(group);
            }
        }

        //---------------------------------------------------------------------
        public bool ContainsMutexGroup(UIWindowGroup group) {
            return ms_MutexGroups.Contains(group);
        }

        //---------------------------------------------------------------------
        public void ClearMutexGroup(UIWindowGroup group) {
            ms_MutexGroups.Clear();
        }

        //---------------------------------------------------------------------
        public bool CancelShutdown(UIWindow win) {
            if (win == null) {
                return false;
            }
            return CancelShutdown(win.WindowName);
        }

        //---------------------------------------------------------------------
        public bool CancelShutdown(string name) {
            if (Instance == null) {
                return false;
            }
            return Instance.m_ShutdownWindows.Remove(name);
        }

        //---------------------------------------------------------------------
        public bool Shutdown(string name) {
            if (Instance != null) {
                return Instance.DoShutdown(name);
            }
            return false;
        }

        //---------------------------------------------------------------------
        public void ShutdownGroup(UIWindowGroup group) {
            ShutdownGroup(group, null);
        }

        public void ShutdownGroup(UIWindowGroup group, List<string> excludeList) {
            if (excludeList != null) {
                for (int i = 0; i < excludeList.Count; ++i) excludeList[i] = excludeList[i];
            }

            UIWindow[] windows = Windows(group);
            for (int index = 0; index < windows.Length; ++index) {
                string winName = windows[index].WindowName;
                if (excludeList != null && excludeList.Contains(winName)) {
                    continue;
                }

                if (!Shutdown(winName)) {
                    LOG.Warning("Shutdown '" + winName + "' failed.");
                }
            }
        }

        //---------------------------------------------------------------------
        public void ShutdownAll() {
            List<string> list = null;
            ShutdownAll(list);
        }

        public void ShutdownAll(List<string> excludeList) {
            List<string> list = new List<string>(m_PackageLoading.Keys);
            for (int i = 0; i < list.Count; ++i) {
                string name = list[i];
                if (excludeList != null && excludeList.Contains(name))
                    continue;
                if (m_PackageLoading[name] != null)
                    m_PackageLoading[name].Destroy();
                m_PackageLoading.Remove(name);
                m_PendingWindows.Remove(name);
                m_RequestShows.Remove(name);
            }
            m_RollbackStack.Clear();

            for (int index = 0; index < Instance.m_WindowList.Count; ++index)
            {
                string name = Instance.m_WindowList[index].WindowName;
                if (excludeList != null && excludeList.Contains(name))
                {
                    continue;
                }

                if (!Shutdown(name))
                {
                    LOG.Warning("Shutdown '" + name + "' failed.");
                }
            }
        }

        public UIWindow Window(string name) {
            if (Instance == null)
                return null;
            return Instance.GetWindow(name);
        }

        public void Window(string packageName, string name, WindowAsynHandler handler, params object[] data) {
            if (Instance == null)
                return;
            Instance.LoadWindow(packageName, name, handler, data);
        }

        //---------------------------------------------------------------------
        public UIWindow[] Windows(UIWindowGroup group) {
            if (Instance == null) {
                return null;
            }

            return Instance.GetWindows(group);
        }

        //---------------------------------------------------------------------
        public static void Show(string name)
        {
            Show(name, name);
        }

        //---------------------------------------------------------------------
        public static void Show(string packageName, string winName)
        {
            if (Instance != null)
            {
                Instance.DoShow(packageName, winName, null, null);
            }
        }

        //---------------------------------------------------------------------
        public static void Show(string packageName, string winName, params object[] data)
        {
            if (Instance != null)
            {
                Instance.DoShow(packageName, winName, null, data);
            }
        }

        //---------------------------------------------------------------------
        public static void Show(string packageName, string winName, WindowAsynHandler handler)
        {
            if (Instance != null)
            {
                Instance.DoShow(packageName, winName, handler, null);
            }
        }

        //---------------------------------------------------------------------
        public static void Show(string packageName, string winName, WindowAsynHandler handler, params object[] data)
        {
            if (Instance != null)
            {
                Instance.DoShow(packageName, winName, handler, data);
            }
        }

        //---------------------------------------------------------------------
        public static void Hide(string winName) {
            if (Instance != null) {
                Instance.DoHide(winName, null);
            }
        }

        //---------------------------------------------------------------------
        public static void Hide(string winName, params object[] data) {
            if (Instance != null) {
                Instance.DoHide(winName, data);
            }
        }

        //---------------------------------------------------------------------
        public static void ShowGroup(UIWindowGroup winGroup) {
            ShowGroup(winGroup, null, null);
        }

        //---------------------------------------------------------------------
        public static void ShowGroup(UIWindowGroup winGroup,
            Action<object> handler, params object[] data) {
            List<UIWindow> winList = Instance.m_WindowList;
            int totalWinCount = winList.Count;
            int alreadyWindowCount = 0;
            for (int index = 0; index < winList.Count; ++index) {
                UIWindow win = winList[index];
                if (win.windowGroup == winGroup) {
                    if (handler == null) {
                        win.Show();
                        continue;
                    }

                    Action postShowHandler = null;
                    postShowHandler = delegate () {
                        win.PostShowEvent -= postShowHandler;
                        alreadyWindowCount++;
                        if (alreadyWindowCount >= totalWinCount) {
                            handler(data);
                        }
                    };
                    win.PostShowEvent += postShowHandler;
                    win.Show();
                }
            }
        }

        //---------------------------------------------------------------------
        public static void ShowAll() {
            List<UIWindow> winList = Instance.m_WindowList;
            for (int index = 0; index < winList.Count; ++index) {
                winList[index].Show();
            }
        }

        //---------------------------------------------------------------------
        public static void ShowAll(Action<object> handler, params object[] data) {
            List<UIWindow> winList = Instance.m_WindowList;
            int totalWinCount = winList.Count;
            int alreadyWindowCount = 0;
            for (int index = 0; index < winList.Count; ++index) {
                UIWindow win = winList[index];
                Action postHandler = null;
                postHandler = delegate () {
                    win.PostHideEvent -= postHandler;
                    alreadyWindowCount++;
                    if (alreadyWindowCount >= totalWinCount) {
                        handler(data);
                    }
                };
                win.PostHideEvent += postHandler;
                win.Show();
            }
        }

        //---------------------------------------------------------------------
        public static void HideGroup(UIWindowGroup winGroup) {
            string s = null;
            HideGroup(winGroup, s, null, null);
        }

        public static void HideGroup(UIWindowGroup winGroup, string exclusiveWinName) {
            HideGroup(winGroup, exclusiveWinName, null, null);
        }

        //---------------------------------------------------------------------
        public static void HideGroup(UIWindowGroup winGroup,
            Action<object> handler, params object[] data) {
            string s = null;
            HideGroup(winGroup, s, handler, data);
        }

        //---------------------------------------------------------------------
        public static void HideGroup(UIWindowGroup winGroup, string exclusiveName,
            Action<object> handler, params object[] data) {
            List<UIWindow> winList = Instance.m_WindowList;
            int totalWinCount = winList.Count;
            int alreadyWindowCount = 0;
            for (int index = 0; index < winList.Count; ++index) {
                UIWindow win = winList[index];
                if (win.WindowName == exclusiveName) {
                    continue;
                }

                if (win.windowGroup == winGroup) {
                    if (handler == null) {
                        win.Hide();
                        continue;
                    }

                    Action postHideHandler = null;
                    postHideHandler = delegate () {
                        win.PostHideEvent -= postHideHandler;
                        alreadyWindowCount++;
                        if (alreadyWindowCount >= totalWinCount) {
                            handler(data);
                        }
                    };
                    win.PostHideEvent += postHideHandler;
                    win.Hide();
                }
            }
        }

        //---------------------------------------------------------------------
        public static void HideAll() {
            List<UIWindow> winList = Instance.m_WindowList;
            for (int index = 0; index < winList.Count; ++index) {
                winList[index].Hide();
            }
        }

        //---------------------------------------------------------------------
        public static void HideAll(Action<object> handler, params object[] data) {
            List<UIWindow> winList = Instance.m_WindowList;
            int totalWinCount = winList.Count;
            int alreadyWindowCount = 0;
            for (int index = 0; index < winList.Count; ++index) {
                UIWindow win = winList[index];
                Action postHandler = null;
                postHandler = delegate () {
                    win.PostHideEvent -= postHandler;
                    alreadyWindowCount++;
                    if (alreadyWindowCount >= totalWinCount) {
                        handler(data);
                    }
                };
                win.PostHideEvent += postHandler;
                win.Hide();
            }
        }

        public void Command(string packageName, string name, params object[] data) {
            if (Instance != null) {
                Instance.DoCommand(packageName, name, data);
            }
        }

        //---------------------------------------------------------------------
        public void Refresh(string name) {
            Refresh(name, null);
        }

        //---------------------------------------------------------------------
        public void Refresh(string packageName, string name, params object[] data) {
            if (Instance != null) {
                Instance.DoRefresh(packageName, name, data);
            }
        }


        //---------------------------------------------------------------------
        public bool Search(string packageName, string name, string conditionType,
            object conditionValue, Action<Transform> handler, params object[] datas) {
            if (Instance != null) {
                return Instance.DoSearch(packageName, name,
                    conditionType, conditionValue, handler, datas);
            }

            return false;
        }

        //---------------------------------------------------------------------
        public void AddRollbackGroup(UIWindowGroup group) {
            if (Instance == null) {
                return;
            }

            if (!Instance.m_RollbackGroupList.Contains(group)) {
                Instance.m_RollbackGroupList.Add(group);
            }
        }

        //---------------------------------------------------------------------
        public void RemoveRollbackGroup(UIWindowGroup group) {
            if (Instance == null) {
                return;
            }

            Instance.m_RollbackGroupList.Remove(group);
        }

        public void ExcludeRollback(string name) {
            if (Instance == null) {
                return;
            }
            if (Instance.m_ExcludeRoolbackList.Contains(name) ||
                Instance.m_ExcludeRoolbackForeverList.Contains(name)) {
                return;
            }

            Instance.m_ExcludeRoolbackList.Add(name);
        }

        //---------------------------------------------------------------------
        public void IncludeRollback(string name) {
            if (Instance == null) {
                return;
            }
            if (!Instance.m_ExcludeRoolbackList.Contains(name)) {
                return;
            }

            Instance.m_ExcludeRoolbackList.Remove(name);
        }

        //---------------------------------------------------------------------
        public void ExcludeRollbackForever(string name) {
            if (Instance == null) {
                return;
            }
            if (Instance.m_ExcludeRoolbackForeverList.Contains(name)) {
                return;
            }

            Instance.m_ExcludeRoolbackForeverList.Add(name);
        }

        //---------------------------------------------------------------------
        public bool IsRollback(string name) {
            Stack<RollbackInfo>.Enumerator iter =
                Instance.m_RollbackStack.GetEnumerator();
            while (iter.MoveNext()) {
                RollbackInfo info = iter.Current;
                for (int index = 0; index < info.winList.Count; ++index) {
                    if (info.winList[index].WindowName == name) {
                        return true;
                    }
                }
            }

            return false;
        }

        //---------------------------------------------------------------------
        public bool IsRollback(UIWindow win) {
            Stack<RollbackInfo>.Enumerator iter =
                Instance.m_RollbackStack.GetEnumerator();
            while (iter.MoveNext()) {
                if (iter.Current.winList.Contains(win)) {
                    return true;
                }
            }

            return false;
        }

        //---------------------------------------------------------------------
        public List<UIWindow> PeekRoolback() {
            if (Instance.m_RollbackStack.Count == 0) {
                return null;
            }

            return Instance.m_RollbackStack.Peek().winList;
        }

        //---------------------------------------------------------------------
        public void ClearRollback() {
            if (Instance == null) {
                return;
            }

            Instance.m_RollbackStack.Clear();
        }

        //---------------------------------------------------------------------
        public int GetShowCount(UIWindowGroup group, bool includeHiding) {
            if (Instance == null) {
                return 0;
            }

            List<UIWindow> wins = Instance.m_WindowList;
            int currentShowCount = 0;
            for (int index = 0; index < wins.Count; ++index) {
                UIWindow win = wins[index];
                if (win.windowGroup == group && win.IsShow()) {
                    if (!includeHiding && win.IsHiding()) {
                        continue;
                    }

                    currentShowCount++;
                }
            }

            return currentShowCount;
        }

        public UIWindow GetAnyShowWindow(UIWindowGroup group, UIWindow exludeWindow) {
            if (Instance == null) {
                return null;
            }

            List<UIWindow> wins = Instance.m_WindowList;
            for (int index = 0; index < wins.Count; ++index) {
                UIWindow win = wins[index];
                if (win.windowGroup == group && win.IsShow()) {
                    if (exludeWindow != null && exludeWindow == win)
                        continue;

                    return win;
                }
            }

            return null;
        }
        #endregion

        #region Internal Method
        //---------------------------------------------------------------------
        private void ProcessAsyncInvokeList() {
            for (ms_AsyncInvokeList.Begin(); ms_AsyncInvokeList.Next();) {
                string name = ms_AsyncInvokeList.Key;
                UIWindow win = GetWindow(name);
                if (win == null) {
                    continue;
                }

                List<AsyncInvokeInfo> infoList = ms_AsyncInvokeList.Value;
                for (int index = 0; index < infoList.Count; ++index) {
                    AsyncInvokeInfo info = infoList[index];
                    MemberInfo memberInfo = info.memberInfo;
                    object[] paramList = info.paramList;

                    try {
                        if (memberInfo.MemberType == MemberTypes.Property) {
                            PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                            propertyInfo.SetValue(win, paramList[0], null);
                        } else {
                            MethodInfo methodInfo = memberInfo as MethodInfo;
                            methodInfo.Invoke(win, paramList);
                        }
                    } catch (System.Exception ex) {
                        LOG.Erro("Async invoke failed: " + ex.Message);
                    }
                }

                infoList.Clear();
            }
        }

        private void LoadWindow(string packageName, string name, WindowAsynHandler handler, object[] data)
        {
            if (string.IsNullOrEmpty(name))
                return;
            UIWindow win = GetWindow(name);
            if (win != null && win.IsInitialized())
            {
                if (handler != null)
                {
                    handler(win, data);
                }

                return;
            }

            List<PendingInfo> pendingInfoList = null;
            if (!m_PendingWindows.TryGetValue(name, out pendingInfoList))
            {
                pendingInfoList = new List<PendingInfo>();
                m_PendingWindows.Add(name, pendingInfoList);

                if (handler != null)
                {
                    pendingInfoList.Add(new PendingInfo(handler, data));
                }
            }
            else
            {
                if (handler != null)
                {
                    pendingInfoList.Add(new PendingInfo(handler, data));
                }

                return;
            }

            LoadWindowInternal(packageName, name);
        }

        private void LoadWindowInternal(string packageName, string name)
        {
            UIPackageAsset package = LoadPackage(packageName);
            if (package != null)
                package.LoadWindow(name);
        }

        private UIPackageAsset LoadPackage(string packageName)
        {
            UIPackageAsset package = null;
            if (!m_PackageAssets.TryGetValue(packageName, out package))
            {
                string descPath = string.Format("{0}/{1}_fui.fui", windowFolder, packageName).ToLower();
                package = RenderFactory.CreateInstance<UIPackageAsset>(descPath, null, packageName, this, m_PackageDependenceAsset);
                m_PackageAssets.Add(packageName, package);
            }
            //加载依赖包
            List<string> dependences = m_PackageDependenceAsset.GetDependencePackages(packageName);
            for (int d = 0; d < dependences.Count; d++)
            {
                LoadPackage(dependences[d]);
            }
            package.ReferenceCount++;
            return package;
        }

        private UIPackageAsset LoadPackageAsync(string packageName)
        {
            UIPackageAsset package = null;
            if (!m_PackageLoading.TryGetValue(packageName, out package))
            {
                if (!m_PackageAssets.TryGetValue(packageName, out package))
                {
                    string descPath = string.Format("{0}/{1}_fui.fui", windowFolder, packageName).ToLower();
                    package = RenderFactory.CreateInstance<UIPackageAsset>(descPath, null, this);
                    m_PackageLoading.Add(packageName, package);
                }
            }
            return package;
        }

        private void UnLoadPackage(string packageName, string winName)
        {
            UIPackageAsset package = null;
            if (!m_PackageLoading.TryGetValue(packageName, out package))
            {
                if (m_PackageAssets.TryGetValue(packageName, out package))
                {
                    bool unload = package.UnLoadAssets(winName);
                    if (unload)
                    {
                        List<string> packageDependences = m_PackageDependenceAsset.GetDependencePackages(packageName);
                        for (int i = 0; i < packageDependences.Count; i++)
                        {
                            UnLoadPackage(packageDependences[i], winName);
                        }
                    }
                }
            }
            else
            {
                m_PackageLoading.Remove(packageName);
                bool unload = package.UnLoadAssets(winName);
                if (unload)
                {
                    List<string> packageDependences = m_PackageDependenceAsset.GetDependencePackages(packageName);
                    for (int i = 0; i < packageDependences.Count; i++)
                    {
                        UnLoadPackage(packageDependences[i], winName);
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        private void DoHide(string name, object[] datas) {
            m_RequestShows.Remove(name);
            UIWindow win = GetWindow(name);
            if (win != null) {
                win.InternalHide(datas);
            }
        }

        //---------------------------------------------------------------------
        private void DoShow(string packageName,string name, WindowAsynHandler handler, object[] data)
        {
            m_RequestShows.Add(name);
            LoadWindow(packageName,name, delegate (UIWindow win, object data1) {
                if (null == win)
                {
                    LOG.Erro("*** [UIManager] DoShow(win=null, type={0})", name);
                    return;
                }
                if (handler != null)
                {
                    handler(win, data);
                }

                m_RequestShows.Remove(name);
                win.InternalShow(data);
            }, data);
        }


        //---------------------------------------------------------------------
        private void DoRefresh(string packageName ,string name, object[] datas) {
            LoadWindow(packageName,name, delegate(UIWindow win, object data1) {
                win.InternalRefresh(datas);
            }, datas);
        }


        private void DoCommand(string packageName, string name, object[] datas) {
            LoadWindow(packageName,name, delegate(UIWindow win, object data1) {
                win.InternalCommand(datas);
            }, datas);
        }

        //---------------------------------------------------------------------
        private bool DoSearch(string packageName, string name, string conditionType,
            object conditionValue, Action<Transform> handler, object[] datas) {
            LoadWindow(packageName,name, delegate(UIWindow win, object data1) {
                win.InternalSearch(conditionType, conditionValue, handler, datas);
            }, new object[] { conditionType, conditionValue, handler, datas });

            return true;
        }

        //---------------------------------------------------------------------
        private bool DoShutdown(string name) {
            bool result = false;
            m_RequestShows.Remove(name);
            if (m_PendingWindows.ContainsKey(name)) {
                result = m_PendingWindows.Remove(name);
            } else {
                UIWindow win = Window(name);
                if (win != null) {
                    result = true;
                    win.Shutdown();
                }
            }
            return result;
        }

        //---------------------------------------------------------------------
        private void AddWindow(UIWindow win, UIPackageAsset package) {
            if (m_WindowList.Contains(win)) {
                LOG.Warning(win.GetType().Name +
                    " is the Instance, only one instance.");
                return;
            }

            m_WindowList.Add(win);
            m_WindowPackageMap.Add(win, package);
        }

        //---------------------------------------------------------------------
        private UIWindow GetWindow(string name) {
            for (int i = 0; i < m_WindowList.Count; ++i) {
                UIWindow win = m_WindowList[i];
                if (win.WindowName == name) {
                    return win;
                }
            }

            return null;
        }

        //---------------------------------------------------------------------
        private UIWindow[] GetWindows(UIWindowGroup group) {
            List<UIWindow> windows = new List<UIWindow>();
            for (int index = 0; index < Instance.m_WindowList.Count; ++index) {
                UIWindow win = Instance.m_WindowList[index];
                if (win.windowGroup == group) {
                    windows.Add(win);
                }
            }

            return windows.ToArray();
        }

        //---------------------------------------------------------------------
        private void RequestPushRollback(UIWindow currentWin) {
            if (m_InRequestRollback) {
                return;
            }

            m_InRequestRollback = true;
            if (!CanRollback(currentWin)) {
                m_InRequestRollback = false;
                return;
            }

            if (m_RollbackStack.Count != 0 &&
                m_RollbackStack.Peek().source == currentWin) {
                m_InRequestRollback = false;
                return;
            }

            List<UIWindow> winList = new List<UIWindow>();
            for (int i = 0; i < m_WindowList.Count; ++i) {
                UIWindow lastWin = m_WindowList[i];
                if (lastWin != currentWin && lastWin.IsShow() && !lastWin.IsHiding() &&
                    m_RollbackGroupList.Contains(lastWin.windowGroup)) {
                    winList.Add(lastWin);
                }
                /*
                if (lastWin != currentWin && lastWin.IsShow() &&
                    m_RollbackGroupList.Contains(lastWin.windowGroup)) {
                    winList.Add(lastWin);
                }
                */
            }

            if (winList.Count != 0) {
                RollbackInfo info = new RollbackInfo(currentWin, winList);
                m_RollbackStack.Push(info);

                Action preShowHandler = null;
                preShowHandler = delegate() {
                    bool lastState = m_InRequestRollback;
                    m_InRequestRollback = true;
                    currentWin.PreShowEvent -= preShowHandler;
                    for (int i = 0; i < info.winList.Count; ++i) {
                        UIWindow rollbackWin = info.winList[i];
                        rollbackWin.Hide((object[])rollbackWin.NotifyPushRollback());
                    }
                    m_InRequestRollback = lastState;
                };
                currentWin.PreShowEvent += preShowHandler;
            }

            m_InRequestRollback = false;
        }

        //---------------------------------------------------------------------
        private void RequestPopRollback(UIWindow currentWin) {
            if (m_InRequestRollback || m_RollbackStack.Count == 0) {
                return;
            }

            m_InRequestRollback = true;
            RollbackInfo info = m_RollbackStack.Peek();
            if (!CanRollback(currentWin) || info.source != currentWin) {
                m_InRequestRollback = false;
                return;
            }

            int remainShowCount = info.winList.Count;
            currentWin.CancelHide();
            for (int i = 0; i < info.winList.Count; ++i) {
                UIWindow rollbackWin = info.winList[i];
                Action preShowHandler = null;
                preShowHandler = delegate() {
                    currentWin.PreShowEvent -= preShowHandler;
                    --remainShowCount;
                    if (remainShowCount == 0) {
                        bool lastState = m_InRequestRollback;
                        m_InRequestRollback = true;
                        currentWin.Hide();
                        m_InRequestRollback = lastState;
                    }
                };
                rollbackWin.PreShowEvent += preShowHandler;

                if (rollbackWin.IsShow()) {
                    preShowHandler();
                } else {
                    rollbackWin.Show((object[])rollbackWin.NotifyPopRollback());
                }
            }

            m_InRequestRollback = false;
        }

        //---------------------------------------------------------------------
        private void ExecutePopRollback(UIWindow currentWin) {
            if (m_RollbackStack.Count == 0 ||
                m_RollbackStack.Peek().source != currentWin) {
                return;
            }

            m_RollbackStack.Pop();
        }

        //---------------------------------------------------------------------
        private bool CanRollback(UIWindow win) {
            if (win == null) {
                return false;
            }

            if (!m_RollbackGroupList.Contains(win.windowGroup)) {
                return false;
            }

            if (m_ExcludeRoolbackList.Contains(win.WindowName) ||
                m_ExcludeRoolbackForeverList.Contains(win.WindowName)) {
                return false;
            }

            return true;
        }

        //---------------------------------------------------------------------
        private void ProcessShutdownWindows() {
            for (int index = 0; index < m_ShutdownWindows.Count; ++index) {
                string name= m_ShutdownWindows[index];
                UIWindow win = GetWindow(name);
                if (win == null || win.IsShow()) {
                    continue;
                }

                if (WindowShutdownEvent != null) {
                    WindowShutdownEvent(win);
                }

                win.DoShutdown();

                m_WindowList.Remove(win);
                m_ShutdownWindows.Remove(name);
                --index;

                //package dont destroy when window shutdown 
                UIPackageAsset package = null;
                if (m_WindowPackageMap.TryGetValue(win, out package))
                    UnLoadPackage(package.name, name);
                UnityEngine.Object.Destroy(win.gameObject);
                m_PackageAssets.Remove(name);
                m_WindowPackageMap.Remove(win);
            }
        }
        #endregion

        #region Unity Method
        //---------------------------------------------------------------------
        List<string> removeLoading = new List<string>();
        public override void Update() {
            try{
                if (OnCompleted != null && m_PackageDependenceAsset.complete)
                {
                    OnCompleted();
                    OnCompleted = null;
                }

                for (int index = 0; index < m_WindowList.Count; ++index) {
                    m_WindowList[index].ProcessRequests();
                }

                for (m_PackageLoading.Begin(); m_PackageLoading.Next();)
                {
                    if (m_PackageLoading.Value.complete)
                        removeLoading.Add(m_PackageLoading.Key);
                }

                if (removeLoading.Count > 0)
                {
                    for (int i = 0; i < removeLoading.Count; i++)
                    {
                        //加载依赖包
                        UIPackageAsset package = m_PackageLoading[removeLoading[i]];
                        List<string> dependences = m_PackageDependenceAsset.GetDependencePackages(removeLoading[i]);
                        for (int d = 0; d < dependences.Count; d++)
                        {
                            LoadPackageAsync(dependences[d]);
                        }
                        package.ReferenceCount++;
                        m_PackageAssets.Add(removeLoading[i], package);
                        m_PackageLoading.Remove(removeLoading[i]);
                    }
                    removeLoading.Clear();
                }

                ProcessAsyncInvokeList();

                if (m_ShutdownWindows.Count != 0) {
                    ProcessShutdownWindows();
                }
            }
            catch(Exception ex){
                Debug.LogError("UIWindow error.msg="+ex.ToString());
            }
        }
        #endregion

        #region Internal Declare
        //---------------------------------------------------------------------
        private struct PendingInfo {
            public object[] data;
            public WindowAsynHandler handler;

            public PendingInfo(WindowAsynHandler handler, object[] data) {
                this.data = data;
                this.handler = handler;
            }
        }

        //---------------------------------------------------------------------
        private struct ShowInfo {
            public Type type;
            public object[] data;
            public UIWindow win;
            public WindowAsynHandler handler;

            public ShowInfo(Type type, WindowAsynHandler handler, object[] data) {
                this.type = type;
                this.data = data;
                this.handler = handler;
                this.win = null;
            }
        }

        //---------------------------------------------------------------------
        private struct AsyncInvokeInfo {
            public MemberInfo memberInfo;
            public object[] paramList;

            public AsyncInvokeInfo(MemberInfo mi, object[] pl) {
                memberInfo = mi;
                paramList = pl;
            }
        }

        //---------------------------------------------------------------------
        private struct RollbackInfo {
            public List<UIWindow> winList;
            public UIWindow source;

            public RollbackInfo(UIWindow source) {
                this.source = source;
                this.winList = new List<UIWindow>();
            }

            public RollbackInfo(UIWindow source, List<UIWindow> winList) {
                this.source = source;
                this.winList = winList;
            }
        }
        #endregion

        #region Internal Member
        //---------------------------------------------------------------------
        //private GameObject ms_SceneObject = null;
        private static UIManager m_Instance = null;
        private List<UIWindowGroup> ms_MutexGroups = new List<UIWindowGroup>();
        private Map<string, List<AsyncInvokeInfo>> ms_AsyncInvokeList = new Map<string, List<AsyncInvokeInfo>>();

        //---------------------------------------------------------------------
        private bool m_InRequestRollback = false;
        private List<UIWindowGroup> m_RollbackGroupList = new List<UIWindowGroup>();
        private List<string> m_ExcludeRoolbackList = new List<string>();
        private List<string> m_ExcludeRoolbackForeverList = new List<string>();

        //---------------------------------------------------------------------
        private List<UIWindow> m_WindowList = new List<UIWindow>();
        private Map<UIWindow, UIPackageAsset> m_WindowPackageMap= new Map<UIWindow, UIPackageAsset>();
        private List<string> m_ShutdownWindows = new List<string>();
        private Stack<RollbackInfo> m_RollbackStack = new Stack<RollbackInfo>();
        private List<string> m_RequestShows = new List<string>();
        private Map<string, List<PendingInfo>> m_PendingWindows = new Map<string, List<PendingInfo>>();
        private Map<string, UIPackageAsset> m_PackageAssets = new Map<string, UIPackageAsset>();
        private Map<string, UIPackageAsset> m_PackageLoading = new Map<string, UIPackageAsset>();
        private Map<string, Type> m_winTypes = new Map<string, Type>();
        private UIPackageDependenceAsset m_PackageDependenceAsset = null;
        #endregion
    }
}
#endif
