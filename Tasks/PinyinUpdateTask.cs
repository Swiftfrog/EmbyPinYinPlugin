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
using PinYinSort.Utils;
using System.Linq;

namespace PinYinSort.Tasks;

/// <summary>
/// 定义一个计划任务，用于批量更新媒体项的 SortName 和 OriginalTitle，以支持拼音排序和搜索。
/// </summary>
public class PinyinUpdateTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    public PinyinUpdateTask(ILibraryManager libraryManager, ILogger logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "PinYinSort for Chinese";
    public string Key => "PinyinToolsScheduledTask";
    public string Description => "扫描媒体库，为中文标题的媒体生成拼音简拼用于排序和搜索。";
    public string Category => "PinYinSort";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
        };
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        _logger.Info("[PinYinSort]: 开始执行拼音处理计划任务...");

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { "Movie", "Series", "Episode", "MusicAlbum", "MusicArtist", "Video", "Photo", "BoxSet" },
            Recursive = true,
            IsVirtualItem = false
        };

        var allItems = _libraryManager.GetItemList(query);
        var totalItems = allItems.Length;
        _logger.Info($"[PinYinSort]: 查询到 {totalItems} 个媒体项需要处理。");

        if (totalItems == 0)
        {
            _logger.Info("[PinYinSort]: 没有找到需要处理的媒体项。任务结束。");
            return;
        }

        var processedCount = 0;
        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 在后台线程执行 CPU-bound 工作
                bool updated = await Task.Run(() => ProcessItem(item), cancellationToken);

                if (updated)
                {
                    // 仅当字段真正被修改时才保存
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    _logger.Debug($"[PinYinSort]: 已更新项目: {item.Name} (ID: {item.Id})");
                }
                else
                {
                    _logger.Debug($"[PinYinSort]: 项目无需更新: {item.Name} (ID: {item.Id})");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[PinYinSort]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
            }

            processedCount++;
            progress.Report((double)processedCount / totalItems * 100.0);
        }

        _logger.Info($"[PinYinSort]: 拼音处理计划任务执行完毕。共处理 {processedCount} 个项目。");
    }

    /// <summary>
    /// 处理单个项目，仅在字段实际变化时才修改并返回 true。
    /// </summary>
    private bool ProcessItem(BaseItem item)
    {
        var nameToProcess = item.Name;
        if (string.IsNullOrEmpty(nameToProcess))
        {
            return false;
        }

        if (!PinyinHelper.ContainsChinese(nameToProcess))
        {
            return false;
        }

        string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            return false;
        }

        var pinyinInitialsUpper = pinyinInitials.ToUpper();
        bool itemUpdated = false;

        // --- 检查并更新 SortName ---
        string currentSortName = item.SortName ?? string.Empty;
        if (!string.Equals(currentSortName, pinyinInitialsUpper, StringComparison.Ordinal))
        {
            item.SetSortNameDirect(pinyinInitialsUpper);
            itemUpdated = true;
            _logger.Debug($"[PinYinSort]: 已设置 SortName 为 {pinyinInitialsUpper}: {item.Name} (ID: {item.Id})");
        }
        else
        {
            _logger.Debug($"[PinYinSort]: SortName 已经是 {pinyinInitialsUpper}，无需更新: {item.Name} (ID: {item.Id})");
        }

        // --- 检查并更新 LockedFields ---
        var currentLockedFields = item.LockedFields ?? Array.Empty<MetadataFields>();
        if (!currentLockedFields.Contains(MetadataFields.SortName))
        {
            var newLockedFields = currentLockedFields.Concat(new[] { MetadataFields.SortName }).ToArray();
            item.LockedFields = newLockedFields;
            itemUpdated = true;
            _logger.Debug($"[PinYinSort]: 已锁定 SortName 字段: {item.Name} (ID: {item.Id})");
        }
        else
        {
            _logger.Debug($"[PinYinSort]: SortName 字段已被锁定，无需重复锁定: {item.Name} (ID: {item.Id})");
        }

        // --- 检查并更新 OriginalTitle ---
        string currentOriginalTitle = item.OriginalTitle ?? string.Empty;
        string expectedTag = $" #{pinyinInitialsUpper}";
        if (!currentOriginalTitle.Contains(expectedTag))
        {
            string newOriginalTitle = string.IsNullOrEmpty(currentOriginalTitle)
                ? pinyinInitialsUpper
                : $"{currentOriginalTitle}{expectedTag}";
            item.OriginalTitle = newOriginalTitle;
            itemUpdated = true;
            _logger.Debug($"[PinYinSort]: 已更新 OriginalTitle 为 {newOriginalTitle}: {item.Name} (ID: {item.Id})");
        }
        else
        {
            _logger.Debug($"[PinYinSort]: OriginalTitle 已包含拼音标签 {expectedTag}，无需更新: {item.Name} (ID: {item.Id})");
        }

        return itemUpdated; // ✅ 仅当至少一个字段被修改时返回 true
    }
}