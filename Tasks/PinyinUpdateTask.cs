// Tasks/PinyinUpdateTask.cs
using MediaBrowser.Model.Tasks; // IScheduledTask, TaskTriggerInfo
using MediaBrowser.Controller.Library; // ILibraryManager
using MediaBrowser.Model.Entities; // BaseItem, ItemUpdateType, MetadataFields
using MediaBrowser.Model.Querying; // InternalItemsQuery
using MediaBrowser.Model.Logging; // ILogger (注意：这里是 Emby 的 ILogger)
using MediaBrowser.Controller.Entities; // BaseItem
using System.Threading; // CancellationToken
using System.Threading.Tasks; // Task
using System.Collections.Generic; // IEnumerable, List
using System; // Exception, ArgumentException
using PinYinSort.Utils; // PinyinHelper
using System.Linq; // 用于 ToList() 和 Contains() 扩展方法

namespace PinYinSort.Tasks
{
    /// 定义一个计划任务，用于批量更新媒体项的 SortName 和 OriginalTitle，以支持拼音排序和搜索。
    public class PinyinUpdateTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger; // 使用 Emby 的 ILogger

        /// 构造函数，通过依赖注入获取所需服务。
        /// <param name="libraryManager">Emby 的库管理器。</param>
        /// <param name="logger">日志记录器 (Emby 的 ILogger)。</param>
        public PinyinUpdateTask(ILibraryManager libraryManager, ILogger logger) // 使用 Emby 的 ILogger
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// 任务的显示名称。
        public string Name => "PinYinSort for Chinese";

        /// 任务的唯一标识符 (Key)。
        public string Key => "PinyinToolsScheduledTask";

        /// 任务的描述。
        public string Description => "扫描媒体库，为中文标题的媒体生成拼音简拼用于排序和搜索。";

        /// 任务所属的类别。
        public string Category => "PinYinSort";

        /// 获取任务的默认触发器 (例如，每天凌晨3点)。
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

        /// 执行任务的核心逻辑。
        /// <param name="cancellationToken">用于取消任务的令牌。</param>
        /// <param name="progress">用于报告任务进度的接口。</param>
        /// <returns>一个表示异步操作的任务。</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // 修正：使用 Emby 的 ILogger.Info 方法
            _logger.Info("[PinYinSort]: 开始执行拼音处理计划任务...");

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
            // 修正 1: 使用 Length 而不是 Count
            var totalItems = allItems.Length; 
            // 修正：使用 Emby 的 ILogger.Info 方法
            _logger.Info($"[PinYinSort]: 查询到 {totalItems} 个媒体项需要处理。");

            if (totalItems == 0)
            {
                // 修正：使用 Emby 的 ILogger.Info 方法
                _logger.Info("[PinYinSort]: 没有找到需要处理的媒体项。任务结束。");
                return;
            }

            var processedCount = 0;
            // 修正 1: 遍历数组
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
                        // 修正 2: 移除 _libraryManager 参数
                        item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                        // 修正：使用 Emby 的 ILogger.Debug 方法
                        _logger.Debug($"[PinYinSort]: 已更新项目: {item.Name} (ID: {item.Id})");
                    }
                    else
                    {
                        // 修正：使用 Emby 的 ILogger.Debug 方法
                        _logger.Debug($"[PinYinSort]: 项目无需更新: {item.Name} (ID: {item.Id})");
                    }
                }
                catch (Exception ex)
                {
                    // 记录处理单个项目时发生的错误，但不中断整个任务
                    // 修正：使用 Emby 的 ILogger.Error 方法
                    _logger.Error($"[PinYinSort]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
                }

                processedCount++;
                // 4. 报告进度 (0.0 到 100.0)
                var percentComplete = (double)processedCount / totalItems * 100.0;
                progress.Report(percentComplete);

                // 可选：为了不给系统带来过大压力，可以短暂休眠
                // await Task.Delay(10, cancellationToken); // 10ms
            }

            // 修正：使用 Emby 的 ILogger.Info 方法
            _logger.Info($"[PinYinSort]: 拼音处理计划任务执行完毕。共处理 {processedCount} 个项目。");
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
                // 修正：使用 Emby 的 ILogger.Debug 方法
                _logger.Debug($"[PinYinSort]: 项目名称为空，跳过: {item.Id}");
                return false;
            }

            // 2. 判断是否包含中文字符
            if (!PinyinHelper.ContainsChinese(nameToProcess))
            {
                // 如果不包含中文字符，则不进行拼音处理
                // 修正：使用 Emby 的 ILogger.Debug 方法
                _logger.Debug($"[PinYinSort]: 项目名称不包含中文，跳过: {item.Name} (ID: {item.Id})");
                return false;
            }

            // 3. 计算拼音简写
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                // 修正：使用 Emby 的 ILogger.Debug 方法
                _logger.Debug($"[PinYinSort]: 计算拼音失败，跳过: {item.Name} (ID: {item.Id})");
                return false;
            }

            var pinyinInitialsUpper = pinyinInitials.ToUpper();

            // 4. 检查 SortName 是否已经是我们期望的值，避免不必要的更新
            var currentSortName = item.SortName ?? string.Empty;
            if (currentSortName.Equals(pinyinInitialsUpper, StringComparison.Ordinal))
            {
                // 修正：使用 Emby 的 ILogger.Debug 方法
                _logger.Debug($"[PinYinSort]: SortName 已经是 {pinyinInitialsUpper}，无需更新: {item.Name} (ID: {item.Id})");
            }
            else
            {
                // 5. 设置 SortName
                item.SetSortNameDirect(pinyinInitialsUpper);
                // 修正：使用 Emby 的 ILogger.Debug 方法
                _logger.Debug($"[PinYinSort]: 已设置 SortName 为 {pinyinInitialsUpper}: {item.Name} (ID: {item.Id})");
            }

            // 6. 设置 LockedFields (修正错误 CS0019)
            // LockedFields 是 MetadataFields 枚举的集合，不是字符串集合。
            try
            {
                // 1. 获取当前已锁定的字段列表 (MetadataFields[])。
                //    使用 ?.ToArray() ?? Array.Empty<MetadataFields>() 来安全地处理 null 的情况。
                var currentLockedFields = item.LockedFields?.ToArray() ?? Array.Empty<MetadataFields>();

                // 2. 检查 MetadataFields.SortName 是否已经存在于列表中。
                if (!currentLockedFields.Contains(MetadataFields.SortName))
                {
                    // 3. 如果不存在，则创建一个新的列表，添加 MetadataFields.SortName，并重新赋值给 item。
                    var newLockedFieldsList = currentLockedFields.ToList(); // 转换为 List 以便添加
                    newLockedFieldsList.Add(MetadataFields.SortName);
                    item.LockedFields = newLockedFieldsList.ToArray(); // 转换回数组并赋值
                    // 修正：使用 Emby 的 ILogger.Debug 方法
                    _logger.Debug($"[PinYinSort]: 已锁定 SortName 字段: {item.Name} (ID: {item.Id})");
                }
                else
                {
                    // 修正：使用 Emby 的 ILogger.Debug 方法
                     _logger.Debug($"[PinYinSort]: SortName 字段已被锁定，无需重复锁定: {item.Name} (ID: {item.Id})");
                }
            }
            catch (Exception ex)
            {
                // 修正：使用 Emby 的 ILogger.Error 方法
                _logger.Error($"[PinYinSort]: 尝试锁定 SortName 字段时出错: {item.Name} (ID: {item.Id})", ex);
                // 即使锁定失败，也继续处理 OriginalTitle
            }

            // 7. 更新 OriginalTitle
            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            var expectedPinyinTag = $" #{pinyinInitialsUpper}";
            if (currentOriginalTitle.Contains(expectedPinyinTag))
            {
                // 修正：使用 Emby 的 ILogger.Debug 方法
                 _logger.Debug($"[PinYinSort]: OriginalTitle 已包含拼音标签 {expectedPinyinTag}，无需更新: {item.Name} (ID: {item.Id})");
            }
            else
            {
                var newOriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
                    ? pinyinInitialsUpper 
                    : $"{currentOriginalTitle}{expectedPinyinTag}";
                item.OriginalTitle = newOriginalTitle;
                // 修正：使用 Emby 的 ILogger.Debug 方法
                _logger.Debug($"[PinYinSort]: 已更新 OriginalTitle 为 {newOriginalTitle}: {item.Name} (ID: {item.Id})");
            }

            // 如果 SortName 或 OriginalTitle 有任何更改，则认为项目被更新
            return true; 
        }
    }
}
