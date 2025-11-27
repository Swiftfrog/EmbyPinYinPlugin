// Tasks/PinyinTagsTask.cs
using MediaBrowser.Model.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies; // 👈 修复 Movie 类型
using MediaBrowser.Common.Configuration; // 👈 【关键】添加这个引用
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using PinyinSeek.Utils; // 👈 修复 CountryTagHelper

namespace PinyinSeek.Tasks;

/// <summary>
/// 批量处理媒体库的国家标签和 IMDb Top 标签。
/// 仅当插件功能启用且任务开关开启时执行。
/// </summary>
public class PinyinTagsTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private readonly IApplicationPaths _appPaths; // 👈 【关键】定义字段

    public PinyinTagsTask(ILibraryManager libraryManager, ILogger logger, IApplicationPaths appPaths)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _appPaths = appPaths;
    }

    public string Name => "Update Pinyin & Country & IMDb Tags";
    public string Key => "PinyinTagsTask";
    public string Description => "扫描媒体库，为项目添加拼音、国家和 IMDb Top 标签。";
    public string Category => "PinyinSeek";

    // 默认每天凌晨 4 点执行
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
        _logger.Info("[PinyinTags]: 开始执行标签更新计划任务...");

        var config = Plugin.Instance.Configuration;

        // 检查是否应执行任务
        if (!config.EnableCountryAsTag && !config.EnableImdbTopTag)
        {
            _logger.Info("[PinyinTags]: 国家标签和 IMDb Top 标签功能均未启用，跳过计划任务。");
            return;
        }

        // 尝试更新 IMDb Top 250 缓存
        await TryUpdateImdbTop250Json();

        // 加载 IMDb Top 250 ID 列表
        var imdbTop250Ids = LoadImdbTop250IdsFromLocal();

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { "Movie", "Series", "Episode", "MusicAlbum", "MusicArtist", "Video", "Photo", "BoxSet" },
            Recursive = true,
            IsVirtualItem = false
        };

        var allItems = _libraryManager.GetItemList(query);
        var totalItems = allItems.Length;

        _logger.Info($"[PinyinTags]: 查询到 {totalItems} 个媒体项需要处理。");

        if (totalItems == 0)
        {
            _logger.Info("[PinyinTags]: 没有找到需要处理的媒体项。任务结束。");
            return;
        }

        var processedCount = 0;
        var updatedCount = 0;

        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool updated = await Task.Run(() => ProcessItem(item, config, imdbTop250Ids), cancellationToken);

                if (updated)
                {
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    _logger.Debug($"[PinyinTags]: 已更新项目: {item.Name} (ID: {item.Id})");
                    updatedCount++;
                }
                else
                {
                    _logger.Debug($"[PinyinTags]: 项目无需更新: {item.Name} (ID: {item.Id})");
                }
            }
            catch (Exception ex)
            {
                // 👇 修复 ILogger.Error 签名
                _logger.Error($"[PinyinTags]: 处理项目时出错: {item.Name} (ID: {item.Id})", ex);
            }

            processedCount++;
            if (processedCount % 100 == 0 || processedCount == totalItems)
            {
                progress.Report((double)processedCount / totalItems * 100.0);
            }
        }

        _logger.Info($"[PinyinTags]: 标签更新计划任务执行完毕。共处理 {processedCount} 个项目，成功更新 {updatedCount} 个项目。");
    }

    /// <summary>
    /// 尝试从网络下载 IMDb Top 250 数据并保存到 configurations 目录下的 imdb_top250.json。
    /// </summary>
    private async Task TryUpdateImdbTop250Json()
    {
        // 👇 【关键】修复路径获取方式，使用注入的 _appPaths
        var jsonPath = Path.Combine(
            _appPaths.ConfigurationDirectoryPath, 
            "imdb_top250.json"
        );
        var url = "https://raw.githubusercontent.com/theapache64/top250/master/top250_min.json";

        try
        {
            using var httpClient = new HttpClient();
            var jsonContent = await httpClient.GetStringAsync(url);

            // 简单验证 JSON 结构
            if (jsonContent.TrimStart().StartsWith("["))
            {
                await File.WriteAllTextAsync(jsonPath, jsonContent);
                _logger.Info("[PinyinTags] 成功从网络更新 IMDb Top 250 缓存。");
            }
            else
            {
                _logger.Warn("[PinyinTags] 下载的 IMDb Top 250 数据格式无效，未更新本地缓存。");
            }
        }
        catch (Exception ex)
        {
            // 👇 修复 ILogger.Error 签名
            _logger.Error("[PinyinTags] 从网络更新 IMDb Top 250 失败，将使用本地缓存（如果存在）。", ex);
        }
    }

    /// <summary>
    /// 从本地文件加载 IMDb Top 250 ID 列表。
    /// </summary>
    private HashSet<string> LoadImdbTop250IdsFromLocal()
    {
        // 👇 【关键】这里也要改成使用 _appPaths
        var jsonPath = Path.Combine(
            _appPaths.ConfigurationDirectoryPath,
            "imdb_top250.json"
        );
        
        if (!File.Exists(jsonPath))
        {
            _logger.Debug("[PinyinTags] 本地 IMDb Top 250 缓存不存在。");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            var movies = JsonSerializer.Deserialize<List<ImdbMovie>>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (movies != null)
            {
                foreach (var m in movies)
                {
                    if (!string.IsNullOrEmpty(m.Id))
                        ids.Add(m.Id);
                }
            }
            _logger.Debug($"[PinyinTags] 成功加载 {ids.Count} 个 IMDb Top 250 ID。");
            return ids;
        }
        catch (Exception ex)
        {
            // 👇 修复 ILogger.Error 签名
            _logger.Error("[PinyinTags] 解析本地 IMDb Top 250 缓存失败。", ex);
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 处理单个项目，添加国家标签和 IMDb Top 标签。
    /// </summary>
    private bool ProcessItem(BaseItem item, PinyinSeekConfig config, HashSet<string> imdbTop250Ids)
    {
        bool updated = false;

        // --- 1. 添加国家标签 ---
        if (config.EnableCountryAsTag)
        {
            // 👇 CountryTagHelper 已通过 using 引入
            if (CountryTagHelper.TryAddCountryTag(item, config, _logger, "PinyinTagsTask"))
            {
                updated = true;
            }
        }

        // --- 2. 添加 IMDb Top 标签 ---
        if (config.EnableImdbTopTag && item is Movie movie)
        {
            var imdbId = movie.ProviderIds?.GetValueOrDefault("Imdb");
            if (!string.IsNullOrEmpty(imdbId) && imdbTop250Ids.Contains(imdbId))
            {
                if (!HasTag(movie, "IMDb Top"))
                {
                    AddTag(movie, "IMDb Top");
                    updated = true;
                    _logger.Debug($"[PinyinTags] 添加 IMDb Top 标签: {movie.Name} (ID: {imdbId})");
                }
            }
        }

        return updated;
    }

    /// <summary>
    /// 检查项目是否已包含指定标签。
    /// </summary>
    private static bool HasTag(BaseItem item, string tag)
        => item.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 为项目添加新标签。
    /// </summary>
    private static void AddTag(BaseItem item, string tag)
    {
        var tags = (item.Tags ?? Array.Empty<string>()).ToList();
        tags.Add(tag);
        item.Tags = tags.ToArray();
    }

    /// <summary>
    /// IMDb Top 250 JSON 数据结构。
    /// </summary>
    private class ImdbMovie
    {
        public string? Id { get; set; }
        public int Rank { get; set; }
        public string? Title { get; set; }
        public int Year { get; set; }
    }
}