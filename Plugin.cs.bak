// Plugin.cs
using MediaBrowser.Common.Configuration; // IConfigurationManager
using MediaBrowser.Common.Plugins; // IPlugin, BasePlugin
using MediaBrowser.Model.Plugins; // BasePluginConfiguration
using MediaBrowser.Model.Serialization; // IJsonSerializer
using System;
// IApplicationHost 在 MediaBrowser.Common 命名空间下
using MediaBrowser.Common; 
// 引入你的提供者命名空间
using EmbyPinyinPlugin.Providers;

namespace EmbyPinyinPlugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IPlugin
    {
        // 保留需要的字段
        private readonly IApplicationPaths _applicationPaths;
        private readonly IXmlSerializer _xmlSerializer;
        // 移除 IConfigurationManager 字段，因为构造函数不再接收它
        // private readonly IConfigurationManager _configurationManager;

        // 修正构造函数，只接收 BasePlugin 所需的参数
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            _applicationPaths = applicationPaths;
            _xmlSerializer = xmlSerializer;
            // _configurationManager = configurationManager; // 移除这一行
        }

        public override string Name => "PinyinSorter";

        public override Guid Id => Guid.Parse("B250C7F4-4E2B-4E7C-8B9A-123456789ABC"); // 请替换成你生成的全新 GUID

        public override string Description => "Adds pinyin initials to media items for sorting and searching.";

        // 如果需要访问 ConfigurationManager，可以通过 ApplicationPaths 或其他服务获取
        // 但通常在 Plugin 类中直接使用它的情况不多
        // public IConfigurationManager ConfigurationManager => _configurationManager; // 如果之前有这个属性
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // 可以在这里添加插件配置选项，例如是否处理 OriginalTitle 等
        // public bool ProcessOriginalTitle { get; set; } = false;
    }
}
