// Plugin.cs
using MediaBrowser.Common.Configuration; // IConfigurationManager
using MediaBrowser.Common.Plugins; // IPlugin, BasePlugin
using MediaBrowser.Model.Plugins; // BasePluginConfiguration
using MediaBrowser.Model.Serialization; // IJsonSerializer
using System;
using MediaBrowser.Common; 
using MediaBrowser.Model.Tasks; // IScheduledTask
using EmbyPinyinPlugin.Providers; // PinyinUpdateTask
using Microsoft.Extensions.Logging; // ILogger

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

        // 重写 GetTasks 方法，返回你的计划任务
        public override IEnumerable<IScheduledTask> GetTasks()
        {
            // 使用依赖注入容器获取 ILibraryManager 和 ILogger
            // 这通常由 Emby 的框架自动处理
            yield return new PinyinUpdateTask(
                _applicationPaths.ServiceProvider.GetService<ILibraryManager>(),
                _applicationPaths.ServiceProvider.GetService<ILogger<PinyinUpdateTask>>()
            );
        }
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        // ... (保持不变)
    }
}
