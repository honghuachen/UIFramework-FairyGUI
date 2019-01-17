using XLua;

namespace Core.UIFramework
{

    [LuaCallCSharp]
    public class LuaWindow : UIWindow
    {
        public LuaTable _peerTable;
        LuaFunction _OnLoaded;
        LuaFunction _OnPrepared;
        LuaFunction _OnInitialized;
        LuaFunction _OnCommand;
        LuaFunction _OnRefresh;
        LuaFunction _OnPreShow;
        LuaFunction _OnPostShow;
        LuaFunction _OnUpdate;
        LuaFunction _OnLateUpdate;
        LuaFunction _OnPreHide;
        LuaFunction _OnPostHide;
        LuaFunction _OnShutdown;
        LuaFunction _OnRequestHide;
        LuaFunction _OnRequestShow;
        LuaFunction _OnRequestHideByRollback;
        LuaFunction _OnRequestShowByRollback;

        protected override void OnAwake()
        {
            _peerTable = CGame.luamgr.Global.CreateWindow(WindowName, this);
            if(_peerTable == null)
            {
                LOG.LogError("lua is no registry window.name="+WindowName);
                return;
            }
            _OnLoaded = _peerTable.Get<string,LuaFunction>("_OnLoaded");
            _OnPrepared = _peerTable.Get<string,LuaFunction>("_OnPrepared");
            _OnInitialized = _peerTable.Get<string,LuaFunction>("_OnInitialized");
            _OnCommand = _peerTable.Get<string,LuaFunction>("_OnCommand");
            _OnRefresh = _peerTable.Get<string,LuaFunction>("_OnRefresh");
            _OnPreShow = _peerTable.Get<string,LuaFunction>("_OnPreShow");
            _OnPostShow = _peerTable.Get<string,LuaFunction>("_OnPostShow");
            _OnUpdate = _peerTable.Get<string,LuaFunction>("_OnUpdate");
            _OnLateUpdate = _peerTable.Get<string,LuaFunction>("_OnLateUpdate");
            _OnPreHide = _peerTable.Get<string,LuaFunction>("_OnPreHide");
            _OnPostHide = _peerTable.Get<string,LuaFunction>("_OnPostHide");
            _OnShutdown = _peerTable.Get<string,LuaFunction>("_OnShutdown");
            _OnRequestHide = _peerTable.Get<string,LuaFunction>("_OnRequestHide");
            _OnRequestShow = _peerTable.Get<string,LuaFunction>("_OnRequestShow");
            _OnRequestHideByRollback = _peerTable.Get<string,LuaFunction>("_OnRequestHideByRollback");
            _OnRequestShowByRollback = _peerTable.Get<string,LuaFunction>("_OnRequestShowByRollback");
        }

        protected override void OnLoaded()
        {
            if (_OnLoaded != null)
                _OnLoaded.Action(_peerTable);
        }

        protected override bool OnPrepared()
        {
            if (_OnPrepared != null)
                return _OnPrepared.Func<LuaTable, bool>(_peerTable);
            return true;
        }

        protected override bool OnInitialized()
        {
            if (_OnInitialized != null)
                return _OnInitialized.Func<LuaTable, bool>(_peerTable);
            return true;
        }

        protected override void OnCommand()
        {
            if (_OnCommand != null)
                _OnCommand.Action(_peerTable);
        }

        protected override void OnRefresh()
        {
            if (_OnRefresh != null)
                _OnRefresh.Action(_peerTable);
        }

        protected override void OnPreShow()
        {
            if (_OnPreShow != null)
                _OnPreShow.Action(_peerTable);
        }

        protected override void OnPostShow()
        {
            if (_OnPostShow != null)
                _OnPostShow.Action(_peerTable);
        }

        protected override void OnUpdate()
        {
            if (_OnUpdate != null)
                _OnUpdate.Action(_peerTable);
        }

        protected override void OnLateUpdate()
        {
            if (_OnLateUpdate != null)
                _OnLateUpdate.Action(_peerTable);
        }

        protected override void OnPreHide()
        {
            if (_OnPreHide != null)
                _OnPreHide.Action(_peerTable);
        }

        protected override void OnPostHide()
        {
            if (_OnPostHide != null)
                _OnPostHide.Action(_peerTable);
        }

        protected override void OnShutdown()
        {
            if (_OnShutdown != null)
                _OnShutdown.Action(_peerTable);
            CGame.luamgr.Global.DestroyWindow(WindowName);

            if (_peerTable != null)
            {
                _peerTable.Dispose();
                _peerTable = null;
            }
            if (_OnLoaded != null)
            {
                _OnLoaded.Dispose();
                _OnLoaded = null;
            }
            if (_OnPrepared != null)
            {
                _OnPrepared.Dispose();
                _OnPrepared = null;
            }
            if (_OnInitialized != null)
            {
                _OnInitialized.Dispose();
                _OnInitialized = null;
            }
            if (_OnCommand != null)
            {
                _OnCommand.Dispose();
                _OnCommand = null;
            }
            if (_OnRefresh != null)
            {
                _OnRefresh.Dispose();
                _OnRefresh = null;
            }
            if (_OnPreShow != null)
            {
                _OnPreShow.Dispose();
                _OnPreShow = null;
            }
            if (_OnPostShow != null)
            {
                _OnPostShow.Dispose();
                _OnPostShow = null;
            }
            if (_OnUpdate != null)
            {
                _OnUpdate.Dispose();
                _OnUpdate = null;
            }
            if (_OnLateUpdate != null)
            {
                _OnLateUpdate.Dispose();
                _OnLateUpdate = null;
            }
            if (_OnPreHide != null)
            {
                _OnPreHide.Dispose();
                _OnPreHide = null;
            }
            if (_OnPostHide != null)
            {
                _OnPostHide.Dispose();
                _OnPostHide = null;
            }
            if (_OnShutdown != null)
            {
                _OnShutdown.Dispose();
                _OnShutdown = null;
            }
            if (_OnRequestHide != null)
            {
                _OnRequestHide.Dispose();
                _OnRequestHide = null;
            }
            if (_OnRequestShow != null)
            {
                _OnRequestShow.Dispose();
                _OnRequestShow = null;
            }
            if (_OnRequestHideByRollback != null)
            {
                _OnRequestHideByRollback.Dispose();
                _OnRequestHideByRollback = null;
            }
            if (_OnRequestShowByRollback != null)
            {
                _OnRequestShowByRollback.Dispose();
                _OnRequestShowByRollback = null;
            }
        }

        protected override void OnRequestHide()
        {
            if (_OnRequestHide != null)
                _OnRequestHide.Action(_peerTable);
        }

        protected override void OnRequestShow()
        {
            if (_OnRequestShow != null)
                _OnRequestShow.Action(_peerTable);
        }

        protected override object OnRequestHideByRollback()
        {
            if (_OnRequestHideByRollback != null)
                return _OnRequestHideByRollback.Func<LuaTable, object>(_peerTable);
            return null;
        }

        protected override object OnRequestShowByRollback()
        {
            if (_OnRequestShowByRollback != null)
                return _OnRequestShowByRollback.Func<LuaTable, object>(_peerTable);
            return null;
        }
    }
}