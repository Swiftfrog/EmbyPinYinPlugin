// Plugin.cs
using MediaBrowser.Common.Configuration; // IConfigurationManager
using MediaBrowser.Common.Plugins; // IPlugin, BasePlugin
using MediaBrowser.Model.Plugins; // BasePluginConfiguration
using MediaBrowser.Model.Serialization; // IJsonSerializer
using System;
using MediaBrowser.Common; 
// 不需要 using System.Collections.Generic; // 因为 Plugin.cs 不再使用 IEnumerable<IScheduledTask>
// 不需要 using MediaBrowser.Model.Tasks; // 因为 Plugin.cs 不再直接处理 IScheduledTask

namespace EmbyPinyinPlugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IPlugin
    {
        private readonly IApplicationPaths _applicationPaths;
        private readonly IXmlSerializer _xmlSerializer;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            _applicationPaths = applicationPaths;
            _xmlSerializer = xmlSerializer;
        }

        public override string Name => "PinyinSorter";

        public override Guid Id => Guid.Parse("F8C84F7C-8EE8-A1C9-70C1-FF7087015BA7"); // 请替换成你生成的全新 GUID

        public override string Description => "Adds pinyin initials to media items for sorting and searching.";

        // 移除 GetTasks 方法
        // public override IEnumerable<IScheduledTask> GetTasks()
        // {
        //     // ...
        // }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // ... (保持不变)
    }
}
