// PinyinUpdateTask.cs
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
/// 批量处理媒体库的拼音排序与搜索标签。
/// 仅当插件功能启用且任务开关开启时执行。
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

    public string Name => "PinyinSeek for Chinese";
    public string Key => "PinyinToolsScheduledTask";
    public string Description => "扫描媒体库，为中文标题的媒体生成拼音简拼用于排序和搜索。";
    public string Category => "PinyinSeek";

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
        _logger.Info("[PinyinSeek]: 开始执行拼音处理计划任务...");

        var config = Plugin.Instance.Configuration;

        // 🔑 检查是否应执行任务
        bool pluginEnabled = config.EnablePinyinSort || config.EnablePinyinSearch;
        if (!pluginEnabled)
        {
            _logger.Info("[PinyinSeek]: 插件功能已关闭（排序和搜索均未启用），跳过计划任务。");
            return;
        }

        if (!config.EnableScheduledTask)
        {
            _logger.Info("[PinyinSeek]: 计划任务开关未启用，跳过执行。您可在插件设置中开启“启用计划任务”。");
            return;
        }

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { "Movie", "Series", "Episode", "MusicAlbum", "MusicArtist", "Video", "Photo", "BoxSet" },
            Recursive = true,
            IsVirtualItem = false
        };

        var allItems = _libraryManager.GetItemList(query);
        var totalItems = allItems.Length;
        _logger.Info($"[PinyinSeek]: 查询到 {totalItems} 个媒体项需要处理。");

        if (totalItems == 0)
        {
            _logger.Info("[PinyinSeek]: 没有找到需要处理的媒体项。任务结束。");
            return;
        }

        var processedCount = 0;
        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool updated = await Task.Run(() => ProcessItem(item, config), cancellationToken);

                if (updated)
                {
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    _logger.Debug($"[PinyinSeek]: 已更新项目: {item.Name} (ID: {item.Id})");
                }
                else
                {
                    _logger.Debug($"[PinyinSeek]: 项目无需更新: {item.Name} (ID: {item.Id})");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[PinyinSeek]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
            }

            processedCount++;
            progress.Report((double)processedCount / totalItems * 100.0);
        }

        _logger.Info($"[PinyinSeek]: 拼音处理计划任务执行完毕。共处理 {processedCount} 个项目。");
    }

    /// <summary>
    /// 处理单个项目，根据配置决定是否更新字段。
    /// </summary>
    private bool ProcessItem(BaseItem item, PinyinSeekConfig config)
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

        // --- SortName 更新逻辑（与 Provider 一致）---
        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            itemUpdated = true;
            _logger.Debug($"[PinyinSeek]: 已设置 SortName 为 {pinyinUpper}: {item.Name} (ID: {item.Id})");
        }
        else
        {
            _logger.Debug($"[PinyinSeek]: SortName 无需更新: {item.Name} (ID: {item.Id})");
        }

        // --- LockedFields 安全更新 ---
        var currentLocked = item.LockedFields ?? Array.Empty<MetadataFields>();
        if (!currentLocked.Contains(MetadataFields.SortName))
        {
            item.LockedFields = currentLocked.Concat(new[] { MetadataFields.SortName }).ToArray();
            itemUpdated = true;
            _logger.Debug($"[PinyinSeek]: 已锁定 SortName 字段: {item.Name} (ID: {item.Id})");
        }
        else
        {
            _logger.Debug($"[PinyinSeek]: SortName 字段已被锁定，无需重复锁定: {item.Name} (ID: {item.Id})");
        }

        // --- OriginalTitle（拼音搜索标签）---
        if (config.EnablePinyinSearch)
        {
            string currentOT = item.OriginalTitle ?? string.Empty;
            string tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                itemUpdated = true;
                _logger.Debug($"[PinyinSeek]: 已更新 OriginalTitle: {item.Name} (ID: {item.Id})");
            }
            else
            {
                _logger.Debug($"[PinyinSeek]: OriginalTitle 已包含拼音标签，无需更新: {item.Name} (ID: {item.Id})");
            }
        }

        return itemUpdated;
    }
}
