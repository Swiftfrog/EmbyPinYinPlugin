// Providers/PinyinUpdateTask.cs
using MediaBrowser.Common.Configuration; // IApplicationPaths
using MediaBrowser.Common.Plugins; // IPlugin
using MediaBrowser.Controller.Entities; // BaseItem
using MediaBrowser.Controller.Library; // ILibraryManager, IProgress, BaseItemKind
using MediaBrowser.Model.Tasks; // IScheduledTask, TaskResult
using System; // Guid
using System.Collections.Generic; // List
using System.Threading; // CancellationToken
using System.Threading.Tasks; // Task
using Microsoft.Extensions.Logging; // ILogger (如果需要日志)
using EmbyPinyinPlugin.Utils; // PinyinHelper
using MediaBrowser.Model.Entities; // MetadataFields

namespace EmbyPinyinPlugin.Providers
{
    public class PinyinUpdateTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<PinyinUpdateTask> _logger; // 可选，用于记录任务进度

        public PinyinUpdateTask(ILibraryManager libraryManager, ILogger<PinyinUpdateTask> logger) // 构造函数注入 ILibraryManager 和 ILogger
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public string Name => "更新中文项目拼音排序和搜索信息";

        public string Key => "PinyinUpdateTask"; // 唯一标识符

        public string Description => "为媒体库中已有的中文标题项目批量更新拼音 SortName 和 OriginalTitle。";

        public string Category => "EmbyPinyinPlugin"; // 任务分类

        // 定义任务执行的频率等（可选，这里留空，表示需要手动运行）
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

        // 修正：方法名改为 Execute，参数顺序调整，返回类型为 Task
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger?.LogInformation("PinyinUpdateTask 开始执行。");

            // 1. 获取所有需要处理的媒体项 (例如，所有 Movie 和 Series)
            // 你可以根据需要调整范围，例如只处理特定库
            var itemsToUpdate = new List<BaseItem>();

            // 获取所有库
            var libraries = _libraryManager.RootFolder.Children;

            foreach (var library in libraries)
            {
                // 检查是否是媒体库 (Folder 类型)
                if (library is Folder folder)
                {
                    _logger?.LogInformation($"扫描媒体库: {folder.Name}");

                    // 获取库中的所有项目
                    // 注意：对于大型库，这种方法可能会加载大量项目到内存。更高效的方式是分页查询或使用内部 API。
                    // 这里为了简化，先获取所有项目。
                    var allItems = folder.GetItemList(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Episode, BaseItemKind.MusicAlbum, BaseItemKind.MusicArtist, BaseItemKind.Video, BaseItemKind.Photo, BaseItemKind.BoxSet }, // 包含你插件支持的所有类型
                        Recursive = true,
                        IsVirtualItem = false // 排除虚拟项目
                    });

                    foreach (var item in allItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested(); // 允许任务被取消

                        // 2. 检查项目名称是否包含中文
                        var nameToCheck = item.Name; // 或 item.OriginalTitle，根据你的逻辑
                        if (!string.IsNullOrEmpty(nameToCheck) && PinyinHelper.ContainsChinese(nameToCheck))
                        {
                            itemsToUpdate.Add(item);
                        }
                    }
                }
            }

            var totalItems = itemsToUpdate.Count;
            _logger?.LogInformation($"找到 {totalItems} 个包含中文的项目需要更新。");

            if (totalItems == 0)
            {
                progress?.Report(100); // 报告完成度
                return; // 空任务结果
            }

            // 3. 遍历并更新项目
            int processedCount = 0;
            foreach (var item in itemsToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // 3a. 计算拼音
                    var nameToProcess = item.Name; // 或 item.OriginalTitle
                    if (string.IsNullOrEmpty(nameToProcess))
                    {
                        continue; // 跳过名称为空的项目
                    }

                    string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
                    if (string.IsNullOrEmpty(pinyinInitials))
                    {
                        continue; // 跳过拼音计算失败的项目
                    }

                    // 3b. 设置 SortName
                    item.SetSortNameDirect(pinyinInitials.ToUpper());

                    // 3c. 锁定 SortName 字段
                    item.LockedFields = new [] { MetadataFields.SortName };

                    // 3d. 更新 OriginalTitle
                    var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
                    if (!currentOriginalTitle.Contains($" #{pinyinInitials.ToUpper()}"))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
                            ? pinyinInitials.ToUpper() 
                            : $"{currentOriginalTitle} #{pinyinInitials.ToUpper()}";
                    }

                    // 3e. 保存项目更改
                    // 通常调用 _libraryManager.UpdateItems 或 item.UpdateToRepository
                    // item.UpdateToRepository(ItemUpdateType.MetadataEdit); // 这个方法可能更直接
                    // 或者使用 _libraryManager.UpdateItems(new[] { item }, ItemUpdateType.MetadataEdit);
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit, _libraryManager.GetLibraryOptions(item));

                    processedCount++;
                    var percentComplete = (double)processedCount / totalItems * 100;
                    progress?.Report(percentComplete);

                    _logger?.LogDebug($"已更新项目: {item.Name} (ID: {item.Id})");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"更新项目失败: {item.Name} (ID: {item.Id})");
                    // 可以选择继续处理下一个项目，或者记录错误并抛出异常
                    // 这里选择记录错误并继续
                }
            }

            _logger?.LogInformation($"PinyinUpdateTask 执行完成。更新了 {processedCount} 个项目。");
            progress?.Report(100); // 报告完成度
        }
    }
}
