// Tasks/PinyinSeekTask.cs
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
using PinyinSeek.Utils;

namespace PinyinSeek.Tasks;

/// <summary>
/// 批量处理拼音排序和拼音搜索的计划任务。
/// </summary>
public class PinyinSeekTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    public PinyinSeekTask(ILibraryManager libraryManager, ILogger logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "PinyinSeek Update";
    public string Key => "PinyinSeekTask";
    public string Description => "批量更新拼音排序和拼音搜索。";
    public string Category => "PinyinSeek";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // 默认每天凌晨 3:30 执行
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = TimeSpan.FromHours(3.5).Ticks
        };
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        _logger.Info("[PinyinSeekTask]: 开始执行拼音更新计划任务...");

        var config = Plugin.Instance.Configuration;

        // 检查是否启用计划任务（全局开关）
        if (!config.EnableScheduledTask)
        {
            _logger.Info("[PinyinSeekTask]: 计划任务未启用（EnableScheduledTask = false），跳过执行。");
            return;
        }

        // 检查是否有至少一个功能启用
        if (!config.EnablePinyinSort && !config.EnablePinyinSearch)
        {
            _logger.Info("[PinyinSeekTask]: 拼音排序和搜索均未启用，跳过任务。");
            return;
        }

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { "Movie", "Series", "BoxSet" },
            Recursive = true,
            IsVirtualItem = false
        };

        var allItems = _libraryManager.GetItemList(query);
        var totalItems = allItems.Length;

        _logger.Info($"[PinyinSeekTask]: 查询到 {totalItems} 个媒体项需要处理。");

        if (totalItems == 0)
        {
            _logger.Info("[PinyinSeekTask]: 没有找到需要处理的媒体项。任务结束。");
            return;
        }

        var processedCount = 0;
        var updatedCount = 0;

        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool updated = await Task.Run(() => ProcessPinyin(item, config), cancellationToken);

                if (updated)
                {
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    updatedCount++;
                    _logger.Debug($"[PinyinSeekTask]: 已更新项目: {item.Name} (ID: {item.Id})");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[PinyinSeekTask]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
            }

            processedCount++;
            if (processedCount % 100 == 0 || processedCount == totalItems)
            {
                progress.Report((double)processedCount / totalItems * 100.0);
            }
        }

        _logger.Info($"[PinyinSeekTask]: 任务执行完毕。共处理 {processedCount} 个项目，成功更新 {updatedCount} 个。");
    }

    private bool ProcessPinyin(BaseItem item, PinyinSeekConfig config)
    {
        var nameToProcess = item.Name;
        if (string.IsNullOrEmpty(nameToProcess) || !PinyinHelper.ContainsChinese(nameToProcess))
        {
            return false;
        }

        string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
        if (string.IsNullOrEmpty(pinyinInitials))
        {
            return false;
        }

        var pinyinUpper = pinyinInitials.ToUpper();
        bool itemUpdated = false;

        // --- SortName 更新 ---
        if (config.EnablePinyinSort && PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            itemUpdated = true;
        }

        // --- 锁定 SortName 字段 ---
        if (config.EnablePinyinSort)
        {
            var currentLocked = item.LockedFields ?? Array.Empty<MetadataFields>();
            if (!currentLocked.Contains(MetadataFields.SortName))
            {
                item.LockedFields = currentLocked.Concat(new[] { MetadataFields.SortName }).ToArray();
                itemUpdated = true;
            }
        }

        // --- OriginalTitle 拼音搜索标签 ---
        if (config.EnablePinyinSearch)
        {
            string currentOT = item.OriginalTitle ?? string.Empty;
            string tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                itemUpdated = true;
            }
        }

        return itemUpdated;
    }
}
