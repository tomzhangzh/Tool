using Newtonsoft.Json.Linq;

namespace VueLibV4.Web.Core;

/// <summary>
/// 外壳页面共享模型：DynWebPage 运行时统一共享上下文。
/// 外壳视图（DynTemplate.ViewPath 指向的 Razor）以 @model DynSharedModel 接收，
/// 前端 data-dyn-shared-model 序列化后同源读取，消除 ViewBag 零散传参。
/// 字段：
///  - RawParams：用户在 DynWebPage.ConfigJson 保存的原始实例参数
///  - EffectiveParams：TableName 自动补齐 dyndata Url 之后最终生效的参数
///  - PassThrough：模板 ConfigJson 顶层 mPassThrough（透传属性）
///  - TemplateConfig：模板 TemplateJson（可实例化的页面配置树，外壳可选使用）
///  - FilterConfig / ListConfig / DetailConfig：引用 PageSetting 的三屏配置树
/// </summary>
public class DynSharedModel
{
    public int DynWebPageId { get; set; }
    public int DynTemplateId { get; set; }
    public string PageTitle { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;

    public Dictionary<string, object> RawParams { get; set; } = new();
    public Dictionary<string, object> EffectiveParams { get; set; } = new();
    public JObject PassThrough { get; set; } = new JObject();
    public JObject TemplateConfig { get; set; } = new JObject();

    public JObject? FilterConfig { get; set; }
    public JObject? ListConfig { get; set; }
    public JObject? DetailConfig { get; set; }

    /// <summary>三屏 PageSetting 的 Vue model 默认骨架（DefaultJson），与 ConfigJson 配套</summary>
    public string? FilterDefaultJson { get; set; }
    public string? ListDefaultJson { get; set; }
    public string? DetailDefaultJson { get; set; }

    /// <summary>三屏渲染方式：Front=前端DynCom动态控件 / Back=后端Razor局部视图</summary>
    public string? FilterRenderMode { get; set; }
    public string? ListRenderMode { get; set; }
    public string? DetailRenderMode { get; set; }

    /// <summary>三屏后端局部视图路径（RenderMode=Back 时使用）</summary>
    public string? FilterPartialPath { get; set; }
    public string? ListPartialPath { get; set; }
    public string? DetailPartialPath { get; set; }
}

/// <summary>
/// 详情/表单页模板模型：独立 Detail 页面（/Platform/Page/DynDetail）专用。
/// 由列表页 open 动作以 fragment 弹窗拉取；也可独立访问。
/// Detail 的 PageSetting（ConfigJson=表单UI / DefaultJson=表单model骨架）由本页单独按 settingId 加载，
/// 不随列表页预加载——Filter/List/Detail 各自独立、按需取用。
/// </summary>
public class DetailTemplateModel
{
    public int SettingId { get; set; }
    public string Table { get; set; } = string.Empty;
    public long BizId { get; set; }
    public string PageTitle { get; set; } = string.Empty;

    /// <summary>表单 UI 渲染配置（来自 PageSetting.ConfigJson，DynCom 组件树）</summary>
    public JObject? DetailConfig { get; set; }
    /// <summary>表单 model 默认骨架（来自 PageSetting.DefaultJson）</summary>
    public string? DetailDefaultJson { get; set; }
    /// <summary>渲染方式：Front=DynCom动态控件 / Back=Razor局部视图</summary>
    public string? RenderMode { get; set; }
    public string? PartialPath { get; set; }

    /// <summary>弹窗窗口参数（open 动作经 query 传入，供 setwin 应用：标题/宽/高/全屏）</summary>
    public string? WinTitle { get; set; }
    public string? WinWidth { get; set; }
    public string? WinHeight { get; set; }
    public bool WinMax { get; set; }

    /// <summary>数据访问库：platform=平台库(默认) / business=业务库（缺省项目库）</summary>
    public string? Db { get; set; }
    /// <summary>新增时预填字段 JSON（主从联动：子表新增预填外键，如 {"OrderId":3}）</summary>
    public string? PrefillJson { get; set; }
}
