// Plugin.cs
using MediaBrowser.Common.Configuration; // IConfigurationManager
using MediaBrowser.Common.Plugins; // IPlugin, BasePlugin
using MediaBrowser.Model.Plugins; // BasePluginConfiguration
using MediaBrowser.Model.Serialization; // IJsonSerializer
using System;
// IApplicationHost 在 MediaBrowser.Common 命名空间下
using MediaBrowser.Common; 

namespace EmbyPinyinPlugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IPlugin
    {
        public Plugin(IApplicationHost applicationHost) : base(applicationHost) // 只接受 IApplicationHost
        {
        }

        public override string Name => "PinyinSorter";

        public override Guid Id => Guid.Parse("71C46E3B-3EB8-0D38-E047-19952D0508B2"); // 请替换成你生成的全新 GUID

        public override string Description => "Adds pinyin initials to media items for sorting and searching.";
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // 可以在这里添加插件配置选项，例如是否处理 OriginalTitle 等
        // public bool ProcessOriginalTitle { get; set; } = false;
    }
}
