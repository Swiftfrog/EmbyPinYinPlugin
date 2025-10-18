// Providers/BasePinyinMetadataProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers; // 包含 ILocalMetadataProvider, ItemInfo, IDirectoryService
using MediaBrowser.Model.Configuration; // 包含 LibraryOptions
using MediaBrowser.Model.Entities; // 包含 MetadataFields (用于 LockedFields)
using System.Threading; // 包含 CancellationToken
using System.Threading.Tasks; // 包含 Task
// 移除 Microsoft.Extensions.Logging 的 using
// using Microsoft.Extensions.Logging; // 包含 ILogger<T>
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
        // 移除 ILogger 字段
        // protected readonly ILogger<BasePinyinMetadataProvider<T>> _logger;

        // 移除 ILogger 参数的构造函数
        // protected BasePinyinMetadataProvider(ILogger<BasePinyinMetadataProvider<T>> logger)
        // {
        //     _logger = logger;
        // }

        // 无参数构造函数
        protected BasePinyinMetadataProvider()
        {
        }

        public abstract string Name { get; }

        public async Task<MetadataResult<T>> GetMetadata(
            ItemInfo info,
            LibraryOptions libraryOptions,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            // --- 最简单的调试代码开始 (仅用于测试) ---
            // 检查 info.Name 是否为空
            var debugNameToProcess = info.Name;
            if (string.IsNullOrEmpty(debugNameToProcess))
            {
                throw new Exception($"DEBUG GetMeta info.Name is null or empty for item with Id: {info.Id}");
            }
            
            // 检查 TinyPinyin 是否能处理名称
            string debugPinyinInitials = PinyinHelper.GetPinyinInitials(debugNameToProcess);
            if (string.IsNullOrEmpty(debugPinyinInitials))
            {
                throw new Exception($"DEBUG GetMeta PinyinHelper returned null or empty for debugNameToProcess: '{debugNameToProcess}', item Id: {info.Id}");
            }
            
            // 确认代码执行到这里
            throw new Exception($"DEBUG GetMeta About to create item and set SortName to '{debugPinyinInitials.ToUpper()}' for item Id: {info.Id}, Name: {info.Name}");
            // --- 最简单的调试代码结束 ---

            // 1. 获取 Name (或 OriginalTitle)
            var nameToProcess = info.Name; // 或 info.OriginalTitle，根据需求
            if (string.IsNullOrEmpty(nameToProcess))
            {
                // 移除日志记录
                // _logger?.LogWarning($"Name is null or empty for item: {info.Name}. Skipping pinyin processing.");
                return new MetadataResult<T>();
            }

            // 2. 计算拼音简写 (同步操作)
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);

            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 // 移除日志记录
                 // _logger?.LogWarning($"Failed to calculate pinyin initials for: {nameToProcess}. Skipping pinyin processing.");
                 return new MetadataResult<T>();
            }

            // 3. 创建或获取媒体项实例，并设置 SortName
            var item = (T)Activator.CreateInstance(typeof(T)); // 创建实例 (可能有更好的方式，取决于具体类型)
            // item.Name = info.Name; // 通常框架会处理基础属性
            item.SetSortNameDirect(pinyinInitials.ToUpper()); // 关键：设置拼音首字母

            // 尝试锁定 SortName 字段，防止被后续提供者覆盖 (需要 using MediaBrowser.Model.Entities;)
            item.LockedFields = new [] { MetadataFields.SortName };

            // 4. 创建 MetadataResult 并返回
            var result = new MetadataResult<T>
            {
                Item = item,
            };

            // 移除日志记录
            // _logger?.LogInformation($"Set SortName to '{pinyinInitials.ToUpper()}' for {typeof(T).Name}: {info.Name}");
            return result;
        }
    }
}
