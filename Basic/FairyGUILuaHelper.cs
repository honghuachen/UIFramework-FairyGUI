using System;
using FairyGUI;
using FairyGUI.Utils;
using XLua;

namespace Core.UIFramework
{
    /// <summary>
    /// 
    /// </summary>
	[LuaCallCSharp]
    public sealed class FairyGUILuaHelper
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="url"></param>
		/// <param name="luaClass"></param>
		public static void SetExtension(string url, System.Type baseType, LuaFunction extendFunction)
		{
			UIObjectFactory.SetPackageItemExtension(url, () =>
			{
				GComponent gcom = (GComponent)Activator.CreateInstance(baseType);
				gcom.data = extendFunction;
				return gcom;
			});
		}

		[BlackList]
		public static LuaTable ConnectLua(GComponent gcom)
		{
			LuaTable peerTable = null;
			LuaFunction extendFunction = gcom.data as LuaFunction;
			if (extendFunction != null)
			{
				gcom.data = null;
                peerTable = extendFunction.Func<GComponent, LuaTable>(gcom);
            }

			return peerTable;
		}
	}

	[LuaCallCSharp]
	public class GLuaComponent : GComponent
	{
		LuaTable _peerTable;

        [BlackList]
        public override void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_peerTable = FairyGUILuaHelper.ConnectLua(this);
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
		}
	}

	[LuaCallCSharp]
	public class GLuaLabel : GLabel
	{
		public LuaTable _peerTable;

        [BlackList]
        public override void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_peerTable = FairyGUILuaHelper.ConnectLua(this);
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
		}
	}

	[LuaCallCSharp]
	public class GLuaButton : GButton
	{
		public LuaTable _peerTable;

        [BlackList]
        public override void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_peerTable = FairyGUILuaHelper.ConnectLua(this);
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
		}
	}

	[LuaCallCSharp]
	public class GLuaProgressBar : GProgressBar
	{
		public LuaTable _peerTable;

        [BlackList]
        public override void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_peerTable = FairyGUILuaHelper.ConnectLua(this);
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
		}
	}

	[LuaCallCSharp]
	public class GLuaSlider : GSlider
	{
		public LuaTable _peerTable;

        [BlackList]
        public override void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_peerTable = FairyGUILuaHelper.ConnectLua(this);
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
		}
	}

	[LuaCallCSharp]
	public class GLuaComboBox : GComboBox
	{
		public LuaTable _peerTable;

        [BlackList]
        public override void ConstructFromXML(XML xml)
		{
			base.ConstructFromXML(xml);

			_peerTable = FairyGUILuaHelper.ConnectLua(this);
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
		}
	}

	[LuaCallCSharp]
	public class GLuaWindow : Window
	{
		public LuaTable _peerTable;
		LuaFunction _OnInit;
		LuaFunction _DoHideAnimation;
		LuaFunction _DoShowAnimation;
		LuaFunction _OnShown;
		LuaFunction _OnHide;

		public void ConnectLua(LuaTable peerTable)
		{
			_peerTable = peerTable;
			_OnInit = peerTable.Get<string,LuaFunction>("OnInit");
			_DoHideAnimation = peerTable.Get<string,LuaFunction>("DoHideAnimation");
			_DoShowAnimation = peerTable.Get<string,LuaFunction>("DoShowAnimation");
			_OnShown = peerTable.Get<string,LuaFunction>("OnShown");
			_OnHide = peerTable.Get<string,LuaFunction>("OnHide");
		}

		public override void Dispose()
		{
			base.Dispose();

			if (_peerTable != null)
			{
				_peerTable.Dispose();
				_peerTable = null;
			}
			if (_OnInit != null)
			{
				_OnInit.Dispose();
				_OnInit = null;
			}
			if (_DoHideAnimation != null)
			{
				_DoHideAnimation.Dispose();
				_DoHideAnimation = null;
			}
			if (_DoShowAnimation != null)
			{
				_DoShowAnimation.Dispose();
				_DoShowAnimation = null;
			}
			if (_OnShown != null)
			{
				_OnShown.Dispose();
				_OnShown = null;
			}
			if (_OnHide != null)
			{
				_OnHide.Dispose();
				_OnHide = null;
			}
		}

		protected override void OnInit()
		{
			if (_OnInit != null)
			{
                _OnInit.Action(_peerTable);
			}
		}

		protected override void DoHideAnimation()
		{
			if (_DoHideAnimation != null)
			{
                _DoHideAnimation.Action(_peerTable);
			}
			else
				base.DoHideAnimation();
		}

		protected override void DoShowAnimation()
		{
			if (_DoShowAnimation != null)
			{
                _DoShowAnimation.Action(_peerTable);
            }
            else
				base.DoShowAnimation();
		}

		protected override void OnShown()
		{
			base.OnShown();

			if (_OnShown != null)
			{
                _OnShown.Action(_peerTable);
            }
        }

		protected override void OnHide()
		{
			base.OnHide();

			if (_OnHide != null)
			{
                _OnHide.Action(_peerTable);
            }
        }
	}
}