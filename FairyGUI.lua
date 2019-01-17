
--[[
    @desc: fairygui帮助类
    author:陈德怀
    time:2019-01-08 14:55:13
]]
local fgui = {
    EventContext = CS.FairyGUI.EventContext,
    EventListener = CS.FairyGUI.EventListener,
    EventDispatcher = CS.FairyGUI.EventDispatcher,
    InputEvent = CS.FairyGUI.InputEvent,
    NTexture = CS.FairyGUI.NTexture,
    Container = CS.FairyGUI.Container,
    Image = CS.FairyGUI.Image,
    Stage = CS.FairyGUI.Stage,
    Controller = CS.FairyGUI.Controller,
    GObject = CS.FairyGUI.GObject,
    GGraph = CS.FairyGUI.GGraph,
    GGroup = CS.FairyGUI.GGroup,
    GImage = CS.FairyGUI.GImage,
    GLoader = CS.FairyGUI.GLoader,
    PlayState = CS.FairyGUI.PlayState,
    GMovieClip = CS.FairyGUI.GMovieClip,
    TextFormat = CS.FairyGUI.TextFormat,
    GTextField = CS.FairyGUI.GTextField,
    GRichTextField = CS.FairyGUI.GRichTextField,
    GTextInput = CS.FairyGUI.GTextInput,
    GComponent = CS.FairyGUI.GComponent,
    GList = CS.FairyGUI.GList,
    GRoot = CS.FairyGUI.GRoot,
    GLabel = CS.FairyGUI.GLabel,
    GButton = CS.FairyGUI.GButton,
    GComboBox = CS.FairyGUI.GComboBox,
    GProgressBar = CS.FairyGUI.GProgressBar,
    GSlider = CS.FairyGUI.GSlider,
    PopupMenu = CS.FairyGUI.PopupMenu,
    ScrollPane = CS.FairyGUI.ScrollPane,
    Transition = CS.FairyGUI.Transition,
    UIPackage = CS.FairyGUI.UIPackage,
    Window = CS.FairyGUI.Window,
    GObjectPool = CS.FairyGUI.GObjectPool,
    Relations = CS.FairyGUI.Relations,
    RelationType = CS.FairyGUI.RelationType,
    UIPanel = CS.FairyGUI.UIPanel,
    UIPainter = CS.FairyGUI.UIPainter,
    TypingEffect = CS.FairyGUI.TypingEffect,
    GTween = CS.FairyGUI.GTween,
    GTweener = CS.FairyGUI.GTweener,
    EaseType = CS.FairyGUI.EaseType,

    TimerCallback = CS.FairyGUI.TimerCallback,
    Timers = CS.FairyGUI.Timers,
    EventCallback1 = CS.FairyGUI.EventCallback1,

    GLuaWindow = CS.Core.UIFramework.FairyGUI.GLuaWindow,
    GLuaComponent = CS.Core.UIFramework.GLuaComponent,
    GLuaLabel = CS.Core.UIFramework.GLuaLabel,
    GLuaButton = CS.Core.UIFramework.GLuaButton,
    GLuaSlider = CS.Core.UIFramework.GLuaSlider,
    GLuaProgressBar = CS.Core.UIFramework.GLuaProgressBar,
    GLuaComboBox = CS.Core.UIFramework.GLuaComboBox,

    LuaHelper = CS.Core.UIFramework.FairyGUILuaHelper,
}

--[[
注册组件扩展，例如
fgui.register_extension(UIPackage.GetItemURL("包名","组件名"), my_extension)
my_extension的定义方式见fgui.extension_class
]]
function fgui.register_extension(url, extension)
    local base = extension.base
    if base == fgui.GComponent then
        base = fgui.GLuaComponent
    elseif base == fgui.GLabel then
        base = fgui.GLuaLabel
    elseif base == fgui.GButton then
        base = fgui.GLuaButton
    elseif base == fgui.GSlider then
        base = fgui.GLuaSlider
    elseif base == fgui.GProgressBar then
        base = fgui.GLuaProgressBar
    elseif base == fgui.GComboBox then
        base = fgui.GLuaComboBox
    else
        print("invalid extension base: " .. base)
        return
	end
	base = typeof(base)
    fgui.LuaHelper.SetExtension(url, base, extension.Extend)
end

--[[
用于继承FairyGUI原来的组件类，例如
MyComponent = fgui.extension_class(GComponent)
function MyComponent:ctor() --当组件构建完成时此方法被调用
	print(self:GetChild("n1"))
end
]]
function fgui.extension_class(base)
    local o = {}
    o.__index = o

    o.base = base or fgui.GComponent

    --@ins: [GComponent]
    o.Extend = function(ins)
        local t = {}
        setmetatable(t, o)
        if t.ctor then
            t:ctor(ins)
        end

        return t
    end

    return o
end

return fgui
