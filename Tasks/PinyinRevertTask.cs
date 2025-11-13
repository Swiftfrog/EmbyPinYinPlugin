// Tasks/PinyinRevertTask.cs
using MediaBrowser.Model.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

// 确保引用 PinyinHelper
using PinyinSeek.Utils; 

namespace PinyinSeek.Tasks;

/// 批量恢复媒体库的 SortName 和 OriginalTitle。
/// 这是一个手动任务，用于撤销 PinyinSeek 所做的更改。
public class PinyinRevertTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    public PinyinRevertTask(ILibraryManager libraryManager, ILogger logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "PinyinSeek Revert";
    public string Key => "PinyinToolsRevertTask";
    public string Description => "移除 PinyinSeek 添加的拼音 SortName 和 OriginalTitle 标签，将字段恢复到默认状态。";
    public string Category => "PinyinSeek";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // 这是一个手动恢复任务，不应该有默认的自动触发器。
        // 用户需要时在“计划任务”面板手动运行。
        return Enumerable.Empty<TaskTriggerInfo>();
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        _logger.Info("[PinyinSeek]: 开始执行拼音数据恢复计划任务...");

        var query = new InternalItemsQuery
        {
            // 查询与 UpdateTask 相同的项目类型
            IncludeItemTypes = new[] { "Movie", "Series", "Episode", "MusicAlbum", "MusicArtist", "Video", "Photo", "BoxSet" },
            Recursive = true,
            IsVirtualItem = false
        };

        var allItems = _libraryManager.GetItemList(query);
        var totalItems = allItems.Length;
        _logger.Info($"[PinyinSeek]: 查询到 {totalItems} 个媒体项需要检查。");

        if (totalItems == 0)
        {
            _logger.Info("[PinyinSeek]: 没有找到需要处理的媒体项。任务结束。");
            return;
        }

        var processedCount = 0;
        var revertedCount = 0; // 新增计数器：记录实际被恢复的项目数量
        
        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 注意：这里我们不需要传递 config，恢复任务应该无条件执行
                bool updated = await Task.Run(() => ProcessRevertItem(item), cancellationToken);

                if (updated)
                {
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    _logger.Debug($"[PinyinSeek]: 已恢复项目: {item.Name} (ID: {item.Id})");
                    revertedCount++; // 只有当项目被更新时，才增加恢复计数器
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[PinyinSeek]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
            }

            processedCount++;
            progress.Report((double)processedCount / totalItems * 100.0);
        }

        // _logger.Info($"[PinyinSeek]: 拼音数据恢复任务执行完毕。共处理 {processedCount} 个项目。");
        // 修正最终日志：明确区分“检查”和“恢复”的项目数量
        _logger.Info($"[PinyinSeek Revert]: 拼音数据恢复任务执行完毕。共检查 {processedCount} 个项目，成功恢复 {revertedCount} 个项目。");
        
        
    }

    /// 处理单个项目，移除 PinyinSeek 添加的字段。
    private bool ProcessRevertItem(BaseItem item)
    {
        var nameToProcess = item.Name;
        // 如果没有中文名，PinyinSeek 当初就不会处理它，我们也跳过
        if (string.IsNullOrEmpty(nameToProcess) || !PinyinHelper.ContainsChinese(nameToProcess))
        {
            return false;
        }

        // 必须重新计算拼音，才能知道要移除什么
        string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            return false;
        }

        var pinyinUpper = pinyinInitials.ToUpper();
        bool itemUpdated = false;

        // --- 1. 恢复 SortName ---
        // 仅当 SortName 正好等于我们计算的拼音时，才将其清除。
        // 这避免了误删用户自定义的 SortName。
        if (string.Equals(item.SortName, pinyinUpper, StringComparison.Ordinal))
        {
            item.SortName = null; // 设置为 null，Emby 将自动回退使用 Name 排序
            itemUpdated = true;
            _logger.Debug($"[PinyinSeek]: 已清除 SortName: {item.Name} (ID: {item.Id})");
        }

        // --- 2. 恢复 LockedFields ---
        var currentLocked = item.LockedFields ?? Array.Empty<MetadataFields>();
        if (currentLocked.Contains(MetadataFields.SortName))
        {
            item.LockedFields = currentLocked.Where(f => f != MetadataFields.SortName).ToArray();
            itemUpdated = true;
            _logger.Debug($"[PinyinSeek]: 已解锁 SortName 字段: {item.Name} (ID: {item.Id})");
        }

        // --- 3. 恢复 OriginalTitle ---
        string currentOT = item.OriginalTitle;
        if (!string.IsNullOrEmpty(currentOT))
        {
            string tag = $" #{pinyinUpper}";

            if (currentOT.EndsWith(tag))
            {
                // 情况A: "Some Title #DLR" -> "Some Title"
                item.OriginalTitle = currentOT.Substring(0, currentOT.Length - tag.Length).TrimEnd();
                // 如果移除标签后为空，则设为 null
                if (string.IsNullOrEmpty(item.OriginalTitle))
                {
                    item.OriginalTitle = null;
                }
                itemUpdated = true;
                _logger.Debug($"[PinyinSeek]: 已从 OriginalTitle 移除标签: {item.Name} (ID: {item.Id})");
            }
            else if (string.Equals(currentOT, pinyinUpper, StringComparison.Ordinal))
            {
                // 情况B: "DLR" -> null (原始标题为空时被插件设置)
                item.OriginalTitle = null;
                itemUpdated = true;
                _logger.Debug($"[PinyinSeek]: 已清除 OriginalTitle (原为空): {item.Name} (ID: {item.Id})");
            }
        }

        return itemUpdated;
    }
}
