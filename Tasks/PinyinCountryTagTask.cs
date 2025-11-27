// Tasks/PinyinCountryTagTask.cs
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
/// 批量处理媒体库的国家标签。
/// 为媒体项添加原产国家作为标签，使用"中文 [代码]"格式。
/// 仅当插件功能启用且任务开关开启时执行。
/// </summary>
public class PinyinCountryTagTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;

    public PinyinCountryTagTask(ILibraryManager libraryManager, ILogger logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "PinyinSeek CountryTag";
    public string Key => "PinyinToolsCountyTagTask";
    public string Description => "扫描媒体库，为所有项目添加原产国家标签（格式：中文 [代码]）。";
    public string Category => "PinyinSeek";

    // 默认设置：每天凌晨 4 点执行（比拼音任务晚 1 小时）
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
        };
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        _logger.Info("[PinyinSeek CountryTag]: 开始执行国家标签更新计划任务...");
        var config = Plugin.Instance.Configuration;

        // 检查是否应执行任务
        if (!config.EnableCountryAsTag)
        {
            _logger.Info("[PinyinSeek CountryTag]: 国家标签功能未启用，跳过计划任务。");
            return;
        }

        if (!config.EnableCountryTagTask)
        {
            _logger.Info("[PinyinSeek CountryTag]: 国家标签计划任务开关未启用，跳过执行。您可在插件设置中开启。");
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

        _logger.Info($"[PinyinSeek CountryTag]: 查询到 {totalItems} 个媒体项需要处理。");

        if (totalItems == 0)
        {
            _logger.Info("[PinyinSeek CountryTag]: 没有找到需要处理的媒体项。任务结束。");
            return;
        }

        var processedCount = 0;
        var updatedCount = 0;

        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool updated = await Task.Run(() => ProcessItem(item, config), cancellationToken);
                if (updated)
                {
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    _logger.Debug($"[PinyinSeek CountryTag]: 已更新项目: {item.Name} (ID: {item.Id})");
                    updatedCount++;
                }
                else
                {
                    _logger.Debug($"[PinyinSeek CountryTag]: 项目无需更新: {item.Name} (ID: {item.Id})");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[PinyinSeek CountryTag]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
            }

            processedCount++;
            // 降低进度报告频率，避免过于频繁
            if (processedCount % 100 == 0 || processedCount == totalItems)
            {
                progress.Report((double)processedCount / totalItems * 100.0);
            }
        }

        _logger.Info($"[PinyinSeek CountryTag]: 国家标签更新计划任务执行完毕。共处理 {processedCount} 个项目，成功更新 {updatedCount} 个项目。");
    }

    /// <summary>
    /// 处理单个项目，添加国家标签。
    /// </summary>
    private bool ProcessItem(BaseItem item, PinyinSeekConfig config)
    {
        // 复用 CountryTagHelper 中的逻辑
        return CountryTagHelper.TryAddCountryTag(item, config, _logger, "CountryTagTask");
    }
}
