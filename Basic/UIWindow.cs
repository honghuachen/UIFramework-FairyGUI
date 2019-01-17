using System;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using ZCore.InternalUtil;
using ZCore;

namespace Core.UIFramework
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class CGameUIViewportAttribute : System.Attribute
    {
        public CGameUIViewportAttribute(string name)
            : this(name, string.Empty)
        {
        }

        public CGameUIViewportAttribute(string name, string content)
        {
            this.name = name;
            this.content = content;
        }

        public string Content { get { return content; } }
        public string Name { get { return name; } }
        private string name;
        private string content;
    }

    //=========================================================================
    [Serializable]
    public enum UIWindowGroup {
        HUD = 0,    // lower group. depth 0 - 9
        Window,     // window group, only one show. depth 10 - 19
        Popup,      // window group. depth 20 - 29
        MsgBox,     // window group. depth 30 - 39
        Topmost     // notify group. depth 40 - 49
    }

    //=========================================================================
    [Serializable]
    public enum UIShutdownMode {
        Auto = 0,
        Hide,
        Custom
    }

    //=========================================================================
    public delegate void WindowAsynHandler(UIWindow win, object data);

    //=========================================================================
    [RequireComponent(typeof(FairyGUI.UIPanel))]
    public class UIWindow :MonoBehaviour{
        #region Public Property

        //---------------------------------------------------------------------
        //[DefaultValue(UIWindowGroup.Window)]
        [SerializeField]
        public UIWindowGroup windowGroup = UIWindowGroup.Window;

        //---------------------------------------------------------------------
        //[DefaultValue(UIShutdownMode.Auto)]
        [SerializeField]
        public UIShutdownMode shutdownMode = UIShutdownMode.Auto;

        [SerializeField]
        public int depth = 0;

        //---------------------------------------------------------------------
        protected string m_WaitGroup = null;
        public string defaultWaitGroup {
            get {
                if (m_WaitGroup == null) {
                    m_WaitGroup = transform.name + "_DefaultGroup";
                }

                return m_WaitGroup;
            }
        }

        //---------------------------------------------------------------------
        protected string m_LoadingWaitGroup = null;
        public string loadingWaitGroup {
            get {
                if (m_LoadingWaitGroup == null) {
                    m_LoadingWaitGroup = transform.name + "_LoadingGroup";
                }

                return m_LoadingWaitGroup;
            }
        }

        public UIManager UIManager { get; private set; } = null;
        public string WindowName { get; private set; }
        public UIPackageAsset package { get; private set; } = null;
        public FairyGUI.UIPanel uiPanel { get; private set; } = null;
        public FairyGUI.GComponent ui
        {
            get { return uiPanel.ui; }
        }
        #endregion

        #region Public Event
        // The first static asset loaded trigger this message.
        public event Action PreparedEvent;

        // The first all asset loaded to touch this event
        public event Action InitializedEvent;

        // After all asset have been ready, refresh requests processed.
        public event Action RefreshEvent;

        // After all asset have been ready, command requests processed.
        public event Action CommandEvent;

        // After all asset have been ready, show prepared event.
        public event Action PreShowEvent;

        // After all asset have been ready, show completed event.
        public event Action PostShowEvent;

        // After all animation have been completed, hide prepared event.
        public event Action PreHideEvent;

        // After all animation have been completed, hide completed event.
        public event Action PostHideEvent;

        // Each frame update event.
        public event Action UpdateEvent;
        public event Action LateUpdateEvent;

        // This event is fired before the destroy of asset.
        public event Action ShutdownEvent;
        #endregion

        #region Public Method

        //---------------------------------------------------------------------
        public void Shutdown() {
            Hide();
            UIManager.TouchWindowShutdownEvent(this);
        }

        //---------------------------------------------------------------------
        internal void DoShutdown() {
            if (ShutdownEvent != null) {
                ShutdownEvent();
            }
            OnShutdown();

            PreparedEvent = null;
            InitializedEvent = null;
            RefreshEvent = null;
            CommandEvent = null;
            PreShowEvent = null;
            PostShowEvent = null;
            PreHideEvent = null;
            PostHideEvent = null;
            UpdateEvent = null;
            LateUpdateEvent = null;
            ShutdownEvent = null;
        }

        //---------------------------------------------------------------------
        public bool IsInitialized() {
            return m_IsInitialized;
        }

        //---------------------------------------------------------------------
        public bool IsCompleted() {
            return (m_RequestBatchs.Count == 0);
        }

        //---------------------------------------------------------------------
        public bool IsShowing() {
            return ContainsRequestBatch(RequestType.ShowBegin) ||
                ContainsRequestBatch(RequestType.ShowEnd) ||
                ContainsRequestBatch(RequestType.ShowAnimation);
        }

        //---------------------------------------------------------------------
        public bool IsHiding() {
            return ContainsRequestBatch(RequestType.HideBegin) ||
                ContainsRequestBatch(RequestType.HideEnd) ||
                ContainsRequestBatch(RequestType.HideAnimation);
        }

        //---------------------------------------------------------------------
        public bool IsShow() {
            return gameObject.activeSelf;
        }

        //---------------------------------------------------------------------
        public bool CancelShow() {
            if (IsShow()) {
                return true;
            }

            return RemoveRequestBatch(RequestType.ShowBegin);
        }

        //---------------------------------------------------------------------
        public bool CancelHide() {
            if (!IsShow()) {
                return true;
            }

            return RemoveRequestBatch(RequestType.HideBegin);
        }

        //---------------------------------------------------------------------
        public void Show(params object[] datas) {
            InternalShow(datas);
        }

        //---------------------------------------------------------------------
        internal void InternalShow(object[] datas) {
            if (IsShow()) {
                // Cancel hide request
                RemoveRequestBatch(RequestType.HideBegin);
                RemoveRequestBatch(RequestType.HideEnd);
                UIManager.CancelShutdown(this);
                Refresh(datas);
                return;
            }
            UIManager.CancelShutdown(this);

            // Refresh first, Filling the data before displaying the content 
            // is not displayed together, avoid the problem.
            InternalRefresh(datas);

            PushRequest(RequestType.ShowBegin, datas);
        }

        //---------------------------------------------------------------------
        public void Hide(params object[] datas) {
            InternalHide(datas);
        }

        //---------------------------------------------------------------------
        internal void InternalHide(object[] datas) {
            if (!IsShow()) {
                // Cancel show and refresh request
                RemoveRequestBatch(RequestType.ShowBegin);
                RemoveRequestBatch(RequestType.ShowEnd);
                RemoveRequestBatch(RequestType.Refresh);
                return;
            }

            PushRequest(RequestType.HideBegin, datas);
        }

        //---------------------------------------------------------------------
        public void Refresh(params object[] datas) {
            PushRequest(RequestType.Refresh, datas);
        }

        //---------------------------------------------------------------------
        internal void InternalRefresh(object[] datas) {
            PushRequest(RequestType.Refresh, datas);
        }

        //---------------------------------------------------------------------
        public void Command(params object[] datas) {
            PushRequest(RequestType.Command, datas);
        }

        //---------------------------------------------------------------------
        internal void InternalCommand(object[] datas) {
            PushRequest(RequestType.Command, datas);
        }

        //---------------------------------------------------------------------
        public bool Search(string conditionType, object conditionValue,
            Action<Transform> handler, params object[] datas) {
            return InternalSearch(conditionType, conditionValue, handler, datas);
        }

        //---------------------------------------------------------------------
        internal bool InternalSearch(string conditionType, object conditionValue,
            Action<Transform> handler, object[] datas) {
            if (handler == null) {
                return false;
            }

            SearchInfo searchInfo = new SearchInfo(
                conditionType, conditionValue, handler);

            if (datas == null || datas.Length == 0) {
                PushRequest(RequestType.Search, searchInfo);
            } else {
                object[] newDatas = new object[datas.Length + 1];
                datas.CopyTo(newDatas, 0);
                newDatas[newDatas.Length - 1] = searchInfo;
                PushRequest(RequestType.Search, newDatas);
            }

            return true;
        }

        //---------------------------------------------------------------------
        internal object NotifyPushRollback() {
            return OnRequestHideByRollback();
        }

        //---------------------------------------------------------------------
        internal object NotifyPopRollback() {
            return OnRequestShowByRollback();
        }

        //---------------------------------------------------------------------
        public int CorrectDepth(int depth) {
            return CorrectDepth(depth, false);
        }

        //---------------------------------------------------------------------
        public int CorrectDepth(int depth, bool warning) {
            int minDepth = GetPanelDepth(windowGroup);
            int maxDepth = GetPanelDepth((UIWindowGroup)(windowGroup + 1)) - 1;
            return CorrectDepth(depth, minDepth, maxDepth, warning);
        }
        #endregion

        #region Internal Property
        //---------------------------------------------------------------------
        protected object[] requestDataArray {
            get {
                if (m_RequestData == null ||
                    m_RequestData.Length == 0) {
                    return null;
                }

                return m_RequestData;
            }
        }

        //---------------------------------------------------------------------
        protected object requestData {
            get {
                if (m_RequestData == null ||
                    m_RequestData.Length == 0) {
                    return null;
                }

                if (m_RequestData != null &&
                    m_RequestData.Length == 1) {
                    return m_RequestData[0];
                }

                return m_RequestData;
            }
        }
        #endregion

        #region Interface Method
        protected virtual void OnAwake() { }

        //---------------------------------------------------------------------
        protected virtual bool OnPrepared() {
            return true;
        }

        //---------------------------------------------------------------------
        protected virtual bool OnInitialized() {
            return true;
        }

        //---------------------------------------------------------------------
        protected virtual void OnLoaded() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnRefresh() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnCommand() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnRequestShow() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnPreShow() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnPostShow() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnRequestHide() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnPreHide() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnPostHide() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnUpdate() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnLateUpdate() {

        }

        //---------------------------------------------------------------------
        protected virtual void OnShutdown() {
            
        }

        //---------------------------------------------------------------------
        protected virtual object OnRequestHideByRollback() {
            return null;
        }

        //---------------------------------------------------------------------
        protected virtual object OnRequestShowByRollback() {
            return null;
        }

        //---------------------------------------------------------------------
        protected virtual Transform DoSearch(
            string conditionType, object conditionValue) {
            return null;
        }
        #endregion

        #region Internal Method
        //---------------------------------------------------------------------
        private void PushRequest(RequestType type, object data) {
            PushRequest(type, new object[] { data });
        }

        //---------------------------------------------------------------------
        private void PushRequest(RequestType type, object[] datas) {
            if (type == RequestType.None) {
                return;
            }

            RequestBatch batch = GetRequestBatch(type);
            if (batch == null) {
                batch = new RequestBatch(type);
                m_RequestBatchs.Add(batch);
                m_RequestBatchs.Sort(delegate(RequestBatch a, RequestBatch b) {
                    return -a.type.CompareTo(b.type);
                });
            }

            string group = defaultGroup;
            if (datas != null && datas.Length != 0) {
                List<IUIRequestOption> options = ExtractRequestOption(ref datas);
                string groupOption = GetRequestGroup(options);
                if (!string.IsNullOrEmpty(groupOption)) {
                    group = groupOption;
                }
            }

            if (ContainsRequestGroup(batch, group)) {
                ModifyRequestGroup(batch, group, datas);
                return;
            }

            // modify mutex request first
            if (type == RequestType.HideBegin) {
                if (ModifyMutexRequest(RequestType.HideEnd, group, datas) ||
                    ModifyMutexRequest(RequestType.HideAnimation, group, datas)) {
                    return;
                }

                OnRequestHide();
                UIManager.TouchWindowRequestHide(this);
            } else if (type == RequestType.ShowBegin) {
                if (ModifyMutexRequest(RequestType.ShowEnd, group, datas) ||
                    ModifyMutexRequest(RequestType.ShowAnimation, group, datas)) {
                    return;
                }

                OnRequestShow();
                UIManager.TouchWindowRequestShow(this);
            }

            batch.groups.Add(new RequestGroup(group, datas));
        }

        //---------------------------------------------------------------------
        private bool ModifyMutexRequest(RequestType type, string group, object[] datas) {
            RequestBatch batch = GetRequestBatch(type);
            if (batch == null) {
                return false;
            }

            RequestGroup requestGroup = GetRequestGroup(batch, group);
            if (requestGroup == null) {
                return false;
            }

            requestGroup.datas = datas;

            return true;
        }

        //---------------------------------------------------------------------
        private List<IUIRequestOption> ExtractRequestOption(ref object[] datas) {
            List<IUIRequestOption> options = new List<IUIRequestOption>();
            List<object> newDatas = new List<object>();
            for (int i = 0; i < datas.Length; ++i) {
                object data = datas[i];
                if (data is IUIRequestOption) {
                    options.Add(data as IUIRequestOption);
                } else {
                    newDatas.Add(data);
                }
            }

            datas = newDatas.ToArray();

            return options;
        }

        //---------------------------------------------------------------------
        private string GetRequestGroup(List<IUIRequestOption> options) {
            for (int i = 0; i < options.Count; ++i) {
                IUIRequestOption option = options[i];
                if (option is UIRequestGroup) {
                    UIRequestGroup group = (UIRequestGroup)option;
                    return group.name;
                }
            }

            return string.Empty;
        }

        //---------------------------------------------------------------------
        private RequestBatch LastRequestBatch() {
            RequestBatch batch = null;
            int index = m_RequestBatchs.Count;
            for (int i = index; i > 0; --i) {
                batch = m_RequestBatchs[i - 1];
                if (batch.groups.Count != 0) {
                    return batch;
                } else {
                    m_RequestBatchs.Remove(batch);
                }
            }

            return null;
        }

        //---------------------------------------------------------------------
        private RequestGroup LastRequestGroup(RequestBatch batch) {
            if (batch == null || batch.groups.Count == 0) {
                return null;
            }

            return batch.groups[batch.groups.Count - 1];
        }

        //---------------------------------------------------------------------
        private RequestGroup PopRequestGroup() {
            RequestBatch batch = LastRequestBatch();
            if (batch == null) {
                return null;
            }

            RequestGroup group = LastRequestGroup(batch);
            batch.groups.Remove(group);
            if (batch.groups.Count == 0) {
                m_RequestBatchs.Remove(batch);
            }

            return group;
        }

        //---------------------------------------------------------------------
        private RequestBatch GetRequestBatch(RequestType type) {
            for (int i = 0; i < m_RequestBatchs.Count; ++i) {
                RequestBatch batch = m_RequestBatchs[i];
                if (batch.type == type) {
                    return batch;
                }
            }

            return null;
        }

        //---------------------------------------------------------------------
        private RequestGroup GetRequestGroup(RequestType type, string group) {
            return GetRequestGroup(GetRequestBatch(type), group);
        }

        //---------------------------------------------------------------------
        private RequestGroup GetRequestGroup(RequestBatch batch, string group) {
            if (batch == null) {
                return null;
            }

            for (int i = 0; i < batch.groups.Count; ++i) {
                RequestGroup reqeustGroup = batch.groups[i];
                if (reqeustGroup.group == group) {
                    return reqeustGroup;
                }
            }

            return null;
        }

        //---------------------------------------------------------------------
        private bool ContainsRequestBatch(RequestType type) {
            return GetRequestBatch(type) != null;
        }

        //---------------------------------------------------------------------
        private bool ContainsRequestGroup(RequestType type, string group) {
            return GetRequestGroup(type, group) != null;
        }

        //---------------------------------------------------------------------
        private bool ContainsRequestGroup(RequestBatch batch, string group) {
            return GetRequestGroup(batch, group) != null;
        }

        //---------------------------------------------------------------------
        private bool ModifyRequestGroup(RequestType type, string group, object[] data) {
            return ModifyRequestGroup(GetRequestBatch(type), group, data);
        }

        //---------------------------------------------------------------------
        private bool ModifyRequestGroup(RequestBatch batch, string group, object[] data) {
            RequestGroup requestGroup = GetRequestGroup(batch, group);
            if (requestGroup == null) {
                return false;
            }

            requestGroup.datas = data;

            return true;
        }

        //---------------------------------------------------------------------
        private bool RemoveRequestBatch(RequestType type) {
            RequestBatch batch = GetRequestBatch(type);
            if (batch == null) {
                return false;
            }

            return m_RequestBatchs.Remove(batch);
        }

        //---------------------------------------------------------------------
        internal void ProcessRequests() {
            // There is not any request.
            RequestBatch batch = LastRequestBatch();
            if (batch == null) {
                return;
            }

            // Waiting for animation finished.
            if (batch.type == RequestType.ShowAnimation ||
                batch.type == RequestType.HideAnimation) {
                return;
            }

            // Waiting for asset loaded.
            if (batch.type == RequestType.Load) {
                // Pop last group
                PopRequestGroup();
                OnLoaded();
                return;
            }

            // Pop last group
            RequestGroup group = PopRequestGroup();
            if (group == null) {
                return;
            }

            // Apply new request data
            m_RequestData = group.datas;
            m_CustomExecute = true;
            switch (batch.type) {
            case RequestType.Initialize:
                ExecuteInitialize();
                break;
            case RequestType.Refresh:
                ExecuteRefresh();
                break;
            case RequestType.Command:
                ExecuteCommand();
                break;
            case RequestType.ShowBegin:
                ExecuteShowBegin();
                break;
            case RequestType.ShowEnd:
                ExecuteShowEnd();
                break;
            case RequestType.HideBegin:
                ExecuteHideBegin();
                break;
            case RequestType.HideEnd:
                ExecuteHideEnd();
                break;
            case RequestType.Search:
                ExecuteSearch();
                break;
            default:
                m_RequestData = null;
                break;
            }
            m_CustomExecute = false;
        }

        //---------------------------------------------------------------------
        private void ExecuteInitialize() {
            OnInitialized();
            if (InitializedEvent != null) {
                InitializedEvent();
            }
            m_IsInitialized = true;
            UIManager.TouchWindowInitializedEvent(this);
        }

        //---------------------------------------------------------------------
        private void ExecuteRefresh() {
            UIManager.TouchWindowRefreshEvent(this);
            OnRefresh();
            if (RefreshEvent != null) {
                RefreshEvent();
            }
        }

        //---------------------------------------------------------------------
        private void ExecuteCommand() {
            UIManager.TouchWindowCommandEvent(this);
            OnCommand();
            if (CommandEvent != null) {
                CommandEvent();
            }
        }

        //---------------------------------------------------------------------
        private void ExecuteShowBegin() {
            gameObject.SetActive(true);

            UIManager.TouchWindowPreShowEvent(this);
            OnPreShow();
            if (PreShowEvent != null) {
                PreShowEvent();
            }

            //if (m_CustomExecute) {
            //    if (!PlayWindowAnimation(RequestType.ShowEnd, true)) {
            //        PushRequest(RequestType.ShowEnd, requestDataArray);
            //    }
            //}

            if (UIManager.ContainsMutexGroup(windowGroup)) {
                UIManager.HideGroup(windowGroup, this.WindowName);
                //UIManager.HideGroup(windowGroup, GetType());
            }
        }

        //---------------------------------------------------------------------
        private void ExecuteShowEnd() {
            UIManager.TouchWindowPostShowEvent(this);
            OnPostShow();
            if (PostShowEvent != null) {
                PostShowEvent();
            }
        }

        //---------------------------------------------------------------------
        private void ExecuteHideBegin() {
            UIManager.TouchWindowPreHideEvent(this);
            OnPreHide();
            if (PreHideEvent != null) {
                PreHideEvent();
            }

            // iTween toggle message
            //if (m_CustomExecute) {
            //    if (!PlayWindowAnimation(RequestType.HideEnd, false)) {
            //        PushRequest(RequestType.HideEnd, requestDataArray);
            //    }
            //}
        }

        //---------------------------------------------------------------------
        private void ExecuteHideEnd() {
            gameObject.SetActive(false);
            UIManager.TouchWindowPostHideEvent(this);
            OnPostHide();
            if (PostHideEvent != null) {
                PostHideEvent();
            }

            if (shutdownMode == UIShutdownMode.Hide &&
                !UIManager.IsRollback(WindowName)) {
                Shutdown();
            }
        }

        //---------------------------------------------------------------------
        private void ExecuteSearch() {
            SearchInfo info = (SearchInfo)requestData;
            Transform result = DoSearch(info.conditionType, info.conditionValue);
            if (info.handler != null) {
                info.handler(result);
            }
        }

        //---------------------------------------------------------------------
        //private bool PlayWindowAnimation(RequestType nextRequest, bool isShow) {
        //    RequestBatch batch = LastRequestBatch();
        //    if (batch != null) {
        //        // Only one animation playing.
        //        if (batch.type == RequestType.ShowAnimation ||
        //            batch.type == RequestType.HideAnimation) {
        //            return false;
        //        }
        //    }

        //    RequestType requestType = isShow ?
        //        RequestType.ShowAnimation : RequestType.HideAnimation;

        //    int totalAnimationCount = 0;
        //    AnimationOrTween.Trigger conditionTrigger = isShow ?
        //        AnimationOrTween.Trigger.OnActivateTrue :
        //        AnimationOrTween.Trigger.OnActivateFalse;
        //    for (int index = 0; index < m_WindowAnimations.Length; ++index) {
        //        UIPlayTween playTween = m_WindowAnimations[index];
        //        if (playTween.trigger != conditionTrigger ||
        //            !playTween.enabled || !playTween.gameObject.activeSelf ||
        //            playTween.tweenTarget == null) {
        //            continue;
        //        }

        //        UITweener[] childTweens = null;
        //        if (playTween.includeChildren) {
        //            childTweens = playTween.tweenTarget.GetComponentsInChildren<UITweener>();
        //        } else {
        //            childTweens = playTween.tweenTarget.GetComponents<UITweener>();
        //        }

        //        int ownTweenerCount = 0;
        //        for (int i = 0; i < childTweens.Length; ++i) {
        //            if (childTweens[i].tweenGroup == playTween.tweenGroup) {
        //                ownTweenerCount++;
        //            }
        //        }

        //        if (ownTweenerCount == 0) {
        //            continue;
        //        }

        //        ++totalAnimationCount;
        //        EventDelegate finishedCallback = null;
        //        object[] lastDataArray = null;
        //        if (requestDataArray != null && requestDataArray.Length != 0) {
        //            lastDataArray = new object[requestDataArray.Length];
        //            Array.Copy(requestDataArray, lastDataArray, lastDataArray.Length);
        //        }

        //        finishedCallback = new EventDelegate(delegate() {
        //            playTween.onFinished.Remove(finishedCallback);
        //            --totalAnimationCount;
        //            if (totalAnimationCount <= 0) {
        //                RemoveRequestBatch(requestType);
        //                PushRequest(nextRequest, lastDataArray);
        //            }
        //        });
        //        playTween.onFinished.Add(finishedCallback);
        //    }

        //    if (totalAnimationCount == 0) {
        //        return false;
        //    }

        //    PushRequest(requestType, requestDataArray);

        //    // iTween toggle message
        //    gameObject.SendMessage("OnActivate", isShow,
        //        SendMessageOptions.DontRequireReceiver);

        //    return true;
        //}

        //---------------------------------------------------------------------
        public int GetPanelDepth(UIWindowGroup group) {
            return 10 * (int)group;
        }

        //---------------------------------------------------------------------
        private int CorrectDepth(int depth, int minDepth, int maxDepth, bool warning) {
            if (warning && (depth < minDepth || depth > maxDepth)) {
                if (!UIManager.ContainsMutexGroup(windowGroup)) {
                    LOG.Warning(GetType().Name +
                        " depth must be between " + minDepth.ToString() +
                        " ~ " + maxDepth.ToString() + ". " +
                        "The system will automatically corrected depth.");
                }
            }

            int offset = maxDepth - minDepth + 1;
            return minDepth + depth % offset;
        }

        //---------------------------------------------------------------------
        private void CorrectPanelsDepth(FairyGUI.UIPanel uiPanel, int minDepth, int maxDepth)
        {
            uiPanel.SetSortingOrder(depth, false);
            uiPanel.SetSortingOrder(CorrectDepth(
                uiPanel.sortingOrder, minDepth, maxDepth, true), true);
        }
        #endregion

        #region Unity Method
        // Don't allow sub class use follow unity method
        public void Setup(UIManager mgr, UIPackageAsset package, string name) {
            this.UIManager = mgr;
            this.WindowName = name;
            this.gameObject.name = name;
            this.package = package;
            InternalAwake();
        }

        private void InternalAwake()
        {
            // 处理名称
            if (string.IsNullOrEmpty(this.WindowName))
            {
                string name = this.gameObject.name;
                int idx = name.IndexOf("(Clone)");
                if (idx != -1) name = name.Substring(0, idx);
                this.gameObject.name = name;
            }
            uiPanel = gameObject.GetComponent<FairyGUI.UIPanel>();
            OnAwake();
            //m_WindowAnimations = GetComponents<UIPlayTween>();
            UIManager.TouchWindowLoaded(this, package);
        }

        //---------------------------------------------------------------------
        protected void Start() {
            gameObject.SetActive(false);

            // Make sure all widget initialized to panel.
            OnPrepared();
            if (PreparedEvent != null) {
                PreparedEvent();
            }
            UIManager.TouchWindowPreparedEvent(this);

            int minDepth = GetPanelDepth(windowGroup);
            int maxDepth = GetPanelDepth((UIWindowGroup)(windowGroup + 1)) - 1;
            CorrectPanelsDepth(uiPanel, minDepth, maxDepth);

            PushRequest(RequestType.Initialize, requestDataArray);
        }

        //---------------------------------------------------------------------luaWindow
        protected void OnEnable() {
            if (!m_CustomExecute) {
                ExecuteShowBegin();
                ExecuteShowEnd();
            }
        }

        //---------------------------------------------------------------------
        protected void OnDisable() {
            if (!m_CustomExecute) {
                ExecuteHideBegin();
                ExecuteHideEnd();
            }
        }

        //---------------------------------------------------------------------
        protected void Update() {
            OnUpdate();
            if (UpdateEvent != null) {
                UpdateEvent();
            }
        }

        //---------------------------------------------------------------------
        protected void LateUpdate() {
            OnLateUpdate();
            if (LateUpdateEvent != null) {
                LateUpdateEvent();
            }
        }

        // do nothing. this make forbid subclass to override.
        protected void OnDestroy() {
            LOG.Debug("***** {0} OnDestroy", this.GetType().Name);
            if (disposables.Count > 0) {
                disposables.Dispose();
            }
        }

        #endregion

        #region Internal Declare
        //---------------------------------------------------------------------
        private enum RequestType {
            None = 0,
            Initialize,
            ShowAnimation,
            HideAnimation,
            Load,
            Refresh,
            ShowEnd,
            HideEnd,
            ShowBegin,
            HideBegin,
            Command,
            Search,
        }

        //---------------------------------------------------------------------
        [System.Serializable]
        private class RequestGroup {
            public string group;
            public object[] datas;

            //---------------------------------------------------------------------
            public RequestGroup(string group, object[] datas) {
                this.group = group;
                this.datas = datas;
            }
        }

        //---------------------------------------------------------------------
        [System.Serializable]
        private class RequestBatch {
            public RequestType type = RequestType.None;
            public List<RequestGroup> groups = new List<RequestGroup>();

            //---------------------------------------------------------------------
            public RequestBatch(RequestType type) {
                this.type = type;
            }
        }

        //---------------------------------------------------------------------
        [System.Serializable]
        private struct SearchInfo {
            public string conditionType;
            public object conditionValue;
            public Action<Transform> handler;

            public SearchInfo(string type, object value,
                Action<Transform> handler) {
                this.conditionType = type;
                this.conditionValue = value;
                this.handler = handler;
            }
        }
        #endregion

        #region Internal Member
        //---------------------------------------------------------------------
        private bool m_IsInitialized = false;

        //---------------------------------------------------------------------
        private object[] m_RequestData = null;

        //---------------------------------------------------------------------
        private bool m_CustomExecute = true;

        //---------------------------------------------------------------------
        private List<RequestBatch> m_RequestBatchs = new List<RequestBatch>(8);

        //---------------------------------------------------------------------
        public const string defaultGroup = "DefaultGroup";
        private const string searchGroup = "__Search_Group_";
        #endregion

        #region 事件接口
        private CompositeDisposable disposables = new CompositeDisposable(10);

        public void RegEvent<TEvent>(Action<CObject, TEvent> action) {
            if (UIManager == null)
                return;
            IDisposable disp = UIManager.RegEvent<TEvent>(action);
            disposables.Add(disp);
        }

        public void FireEvent<TEvent>(TEvent e) {
            if (UIManager == null)
                return;
            UIManager.FireEvent(e);
        }
        #endregion
    }
}
