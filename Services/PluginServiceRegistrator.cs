// Services/PluginServiceRegistrator.cs
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection; // 需要这个命名空间
using EmbyPinyinPlugin.Providers; // PinyinUpdateTask
using Microsoft.Extensions.Logging; // ILogger

namespace EmbyPinyinPlugin.Services
{
    /// <summary>
    /// 用于向 Emby 服务容器注册插件服务（如计划任务）。
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            // 注册计划任务
            serviceCollection.AddScoped<IScheduledTask, PinyinUpdateTask>();

            // 如果任务需要 ILogger，通常框架会自动注入
            // serviceCollection.AddScoped<ILogger<PinyinUpdateTask>, ...>(); // 通常不需要手动注册 ILogger
        }
    }
}
