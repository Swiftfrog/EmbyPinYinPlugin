// Providers/BasePinyinMetadataProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers; // 包含 ILocalMetadataProvider, ItemInfo, IDirectoryService
using MediaBrowser.Model.Configuration; // 包含 LibraryOptions
using System.Threading; // 包含 CancellationToken
// 注意：移除了 System.Threading.Tasks，因为方法改为同步
using Microsoft.Extensions.Logging; // 包含 ILogger<T>
using System; // 包含 Activator
using EmbyPinyinPlugin.Utils; // 包含 PinyinHelper

namespace EmbyPinyinPlugin.Providers
{
    /// <summary>
    /// 为元数据提供者提供通用的拼音处理逻辑。
    /// </summary>
    /// <typeparam name="T">具体的媒体项类型，如 Movie, Series 等。</typeparam>
    public abstract class BasePinyinMetadataProvider<T> : ILocalMetadataProvider<T>
        where T : BaseItem
    {
        protected readonly ILogger<BasePinyinMetadataProvider<T>> _logger;

        protected BasePinyinMetadataProvider(ILogger<BasePinyinMetadataProvider<T>> logger)
        {
            _logger = logger;
        }

        public abstract string Name { get; }

        // 修改：移除 async, Task<...>, await
        public MetadataResult<T> GetMetadata(
            ItemInfo info,
            LibraryOptions libraryOptions,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            _logger?.LogDebug($"Processing GetMetadata for {typeof(T).Name}: {info.Name}");

            // 1. 获取 Name (或 OriginalTitle)
            var nameToProcess = info.Name; // 或 info.OriginalTitle，根据需求
            if (string.IsNullOrEmpty(nameToProcess))
            {
                _logger?.LogWarning($"Name is null or empty for item: {info.Name}. Skipping pinyin processing.");
                return new MetadataResult<T>();
            }

            // 2. 计算拼音简写 (同步操作)
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);

            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 _logger?.LogWarning($"Failed to calculate pinyin initials for: {nameToProcess}. Skipping pinyin processing.");
                 return new MetadataResult<T>();
            }

            // 3. 创建或获取媒体项实例，并设置 SortName
            var item = (T)Activator.CreateInstance(typeof(T)); // 创建实例 (可能有更好的方式，取决于具体类型)
            // item.Name = info.Name; // 通常框架会处理基础属性
            item.SetSortNameDirect(pinyinInitials.ToUpper()); // 关键：设置拼音首字母

            // 4. 创建 MetadataResult 并返回
            var result = new MetadataResult<T>
            {
                Item = item,
            };

            _logger?.LogInformation($"Set SortName to '{pinyinInitials.ToUpper()}' for {typeof(T).Name}: {info.Name}");
            return result; // 直接返回结果
        }
    }
}
