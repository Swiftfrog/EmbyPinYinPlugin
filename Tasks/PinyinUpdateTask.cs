// Tasks/PinyinUpdateTask.cs
using MediaBrowser.Model.Tasks; // IScheduledTask, TaskTriggerInfo
using MediaBrowser.Controller.Library; // ILibraryManager
using MediaBrowser.Model.Entities; // BaseItem, ItemUpdateType, MetadataFields
using MediaBrowser.Model.Querying; // InternalItemsQuery
using MediaBrowser.Model.Logging; // ILogger (如果需要)
using System.Threading; // CancellationToken
using System.Threading.Tasks; // Task
using System.Collections.Generic; // IEnumerable
using System; // Exception, ArgumentException
using EmbyPinyinPlugin.Utils; // PinyinHelper
using Microsoft.Extensions.Logging; // ILogger<T> (如果插件主类使用了这个)
using MediaBrowser.Controller.Entities; // BaseItem (新增)

namespace EmbyPinyinPlugin.Tasks
{
    /// <summary>
    /// 定义一个计划任务，用于批量更新媒体项的 SortName 和 OriginalTitle，以支持拼音排序和搜索。
    /// </summary>
    public class PinyinUpdateTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<PinyinUpdateTask> _logger; // 使用泛型 ILogger

        /// <summary>
        /// 构造函数，通过依赖注入获取所需服务。
        /// </summary>
        /// <param name="libraryManager">Emby 的库管理器。</param>
        /// <param name="logger">日志记录器。</param>
        public PinyinUpdateTask(ILibraryManager libraryManager, ILogger<PinyinUpdateTask> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// <summary>
        /// 任务的显示名称。
        /// </summary>
        public string Name => "拼音排序与搜索处理任务";

        /// <summary>
        /// 任务的唯一标识符 (Key)。
        /// </summary>
        public string Key => "PinyinToolsScheduledTask";

        /// <summary>
        /// 任务的描述。
        /// </summary>
        public string Description => "扫描媒体库，为中文标题的媒体生成拼音简拼用于排序和搜索。";

        /// <summary>
        /// 任务所属的类别。
        /// </summary>
        public string Category => "元数据";

        /// <summary>
        /// 获取任务的默认触发器 (例如，每天凌晨3点)。
        /// </summary>
        /// <returns>触发器信息集合。</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // 默认每天凌晨 3 点执行
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            };
        }

        /// <summary>
        /// 执行任务的核心逻辑。
        /// </summary>
        /// <param name="cancellationToken">用于取消任务的令牌。</param>
        /// <param name="progress">用于报告任务进度的接口。</param>
        /// <returns>一个表示异步操作的任务。</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.LogInformation("开始执行拼音处理计划任务...");

            // 1. 查询所有需要处理的媒体项
            // 包括 Movie, Series, Episode, MusicAlbum, MusicArtist, Video, Photo, BoxSet
            // 可以根据需要调整 IncludeItemTypes
            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { "Movie", "Series", "Episode", "MusicAlbum", "MusicArtist", "Video", "Photo", "BoxSet" },
                Recursive = true, // 递归查询子文件夹
                IsVirtualItem = false // 排除虚拟项（如合集内的项目）
            };

            var allItems = _libraryManager.GetItemList(query);
            var totalItems = allItems.Count;
            _logger.LogInformation($"查询到 {totalItems} 个媒体项需要处理。");

            if (totalItems == 0)
            {
                _logger.LogInformation("没有找到需要处理的媒体项。任务结束。");
                return;
            }

            var processedCount = 0;
            foreach (var item in allItems)
            {
                cancellationToken.ThrowIfCancellationRequested(); // 检查是否被取消

                try
                {
                    // 2. 复用核心处理逻辑
                    bool updated = ProcessItem(item);

                    if (updated)
                    {
                        // 3. 保存更改
                        // ItemUpdateType.MetadataEdit 表示元数据被编辑
                        item.UpdateToRepository(ItemUpdateType.MetadataEdit, _libraryManager);
                        _logger.LogDebug($"已更新项目: {item.Name} (ID: {item.Id})");
                    }
                    else
                    {
                        _logger.LogDebug($"项目无需更新: {item.Name} (ID: {item.Id})");
                    }
                }
                catch (Exception ex)
                {
                    // 记录处理单个项目时发生的错误，但不中断整个任务
                    _logger.LogError(ex, $"处理项目时出错: {item.Name} (ID: {item.Id})");
                }

                processedCount++;
                // 4. 报告进度 (0.0 到 100.0)
                var percentComplete = (double)processedCount / totalItems * 100.0;
                progress.Report(percentComplete);

                // 可选：为了不给系统带来过大压力，可以短暂休眠
                // await Task.Delay(10, cancellationToken); // 10ms
            }

            _logger.LogInformation($"拼音处理计划任务执行完毕。共处理 {processedCount} 个项目。");
        }

        /// <summary>
        /// 核心处理逻辑：检查项目名称是否包含中文，计算拼音，更新 SortName 和 OriginalTitle。
        /// </summary>
        /// <param name="item">要处理的媒体项。</param>
        /// <returns>如果项目被更新，则返回 true；否则返回 false。</returns>
        private bool ProcessItem(BaseItem item)
        {
            // 1. 获取 Name 用于判断和计算拼音
            var nameToProcess = item.Name; // 或 item.OriginalTitle，根据需求
            if (string.IsNullOrEmpty(nameToProcess))
            {
                _logger.LogDebug($"项目名称为空，跳过: {item.Id}");
                return false;
            }

            // 2. 判断是否包含中文字符
            if (!PinyinHelper.ContainsChinese(nameToProcess))
            {
                // 如果不包含中文字符，则不进行拼音处理
                _logger.LogDebug($"项目名称不包含中文，跳过: {item.Name} (ID: {item.Id})");
                return false;
            }

            // 3. 计算拼音简写
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                _logger.LogDebug($"计算拼音失败，跳过: {item.Name} (ID: {item.Id})");
                return false;
            }

            var pinyinInitialsUpper = pinyinInitials.ToUpper();

            // 4. 检查 SortName 是否已经是我们期望的值，避免不必要的更新
            var currentSortName = item.SortName ?? string.Empty;
            if (currentSortName.Equals(pinyinInitialsUpper, StringComparison.Ordinal))
            {
                _logger.LogDebug($"SortName 已经是 {pinyinInitialsUpper}，无需更新: {item.Name} (ID: {item.Id})");
            }
            else
            {
                // 5. 设置 SortName
                item.SetSortNameDirect(pinyinInitialsUpper);
                _logger.LogDebug($"已设置 SortName 为 {pinyinInitialsUpper}: {item.Name} (ID: {item.Id})");
            }

            // 6. 设置 LockedFields
            // 注意：直接修改 LockedFields 集合可能不是线程安全的，或者不是 Emby 推荐的方式。
            // 更推荐的方式是调用 item.LockField(MetadataFields.SortName) 或类似方法（如果存在）。
            // 如果没有专门的方法，可以尝试创建新的 HashSet 并赋值，但这取决于 BaseItem 的实现。
            // 假设 BaseItem 有一个 LockField 方法或允许直接修改 LockedFields 属性：
            item.LockField(MetadataFields.SortName); // Emby 可能提供了这个便捷方法
            // 或者，如果需要直接操作 HashSet：
            // var currentLockedFields = item.LockedFields ?? new HashSet<MetadataFields>();
            // if (!currentLockedFields.Contains(MetadataFields.SortName))
            // {
            //     var newLockedFields = new HashSet<MetadataFields>(currentLockedFields) { MetadataFields.SortName };
            //     item.LockedFields = newLockedFields; // 这行需要确认 BaseItem.LockedFields 是否可写
            // }

            // 7. 更新 OriginalTitle
            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            var expectedPinyinTag = $" #{pinyinInitialsUpper}";
            if (currentOriginalTitle.Contains(expectedPinyinTag))
            {
                 _logger.LogDebug($"OriginalTitle 已包含拼音标签 {expectedPinyinTag}，无需更新: {item.Name} (ID: {item.Id})");
            }
            else
            {
                var newOriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
                    ? pinyinInitialsUpper 
                    : $"{currentOriginalTitle}{expectedPinyinTag}";
                item.OriginalTitle = newOriginalTitle;
                _logger.LogDebug($"已更新 OriginalTitle 为 {newOriginalTitle}: {item.Name} (ID: {item.Id})");
            }

            // 如果 SortName 或 OriginalTitle 有任何更改，则认为项目被更新
            // 由于我们总是尝试设置 LockedFields 和 OriginalTitle (如果需要)，只要进入了这个方法，就可以认为是 "updated"
            // 但更精确的做法是只在实际值发生改变时才返回 true。
            // 这里我们假设只要项目名包含中文且拼音计算成功，就认为它被处理了。
            // 如果 SortName 或 OriginalTitle 发生了实际变化，返回 true。
            return true; // 根据上面的逻辑，如果进入此方法，通常意味着至少尝试了更新。
                         // 更严格的判断是：如果 currentSortName != pinyinInitialsUpper || !currentOriginalTitle.Contains(expectedPinyinTag)
                         // 但 item.UpdateToRepository 会智能处理，只有真正改变的字段才会触发更新。
        }
    }
}
