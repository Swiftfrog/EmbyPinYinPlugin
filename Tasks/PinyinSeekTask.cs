// Tasks/PinyinSeekTask.cs
using MediaBrowser.Model.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Common.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Net.Http;
// using System.Text.Json;
using PinyinSeek.Utils;

#nullable enable

namespace PinyinSeek.Tasks;

/// <summary>
/// 统一处理拼音排序、拼音搜索、国家标签、IMDb Top 250 标签的计划任务。
/// </summary>
public class PinyinSeekTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private readonly IApplicationPaths _appPaths;

    public PinyinSeekTask(ILibraryManager libraryManager, ILogger logger, IApplicationPaths appPaths)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _appPaths = appPaths;
    }

    public string Name => "PinyinSeek Update";
    public string Key => "PinyinSeekTask";
    public string Description => "统一更新拼音排序、拼音搜索标签、国家标签、IMDb Top 250 标签。";
    public string Category => "PinyinSeek";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // 默认每天凌晨 3:30 执行（介于原两个任务之间）
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerDaily,
            TimeOfDayTicks = TimeSpan.FromHours(3.5).Ticks
        };
    }

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        _logger.Info("[PinyinSeekTask]: 开始执行元数据更新计划任务...");

        var config = Plugin.Instance.Configuration;

        // 检查是否启用计划任务（全局开关）
        if (!config.EnableScheduledTask)
        {
            _logger.Info("[PinyinSeekTask]: 计划任务未启用（EnableScheduledTask = false），跳过执行。");
            return;
        }

        // 检查是否有至少一个功能启用
        bool hasWork =
            config.EnablePinyinSort ||
            config.EnablePinyinSearch ||
            config.EnableCountryAsTag ||
            config.EnableImdbTopTag;

        if (!hasWork)
        {
            _logger.Info("[PinyinSeekTask]: 所有功能（拼音排序/搜索、国家标签、IMDb Top 标签）均未启用，跳过任务。");
            return;
        }

        // 仅当需要 IMDb Top 标签时才更新缓存
        HashSet<string> imdbTop250Ids = new(StringComparer.OrdinalIgnoreCase);
        if (config.EnableImdbTopTag)
        {
            await TryUpdateImdbTop250Json();
            imdbTop250Ids = LoadImdbTop250IdsFromLocal();
        }

        // 查询项目类型：与原拼音任务一致（Movie/Series/BoxSet），足够覆盖标签需求
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
                bool updated = await Task.Run(() => ProcessItem(item, config, imdbTop250Ids), cancellationToken);

                if (updated)
                {
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                    updatedCount++;
                    _logger.Debug($"[PinyinSeekTask]: 已更新项目: {item.Name} (ID: {item.Id})");
                }
                else
                {
                    _logger.Debug($"[PinyinSeekTask]: 项目无需更新: {item.Name} (ID: {item.Id})");
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

    private bool ProcessItem(BaseItem item, PinyinSeekConfig config, HashSet<string> imdbTop250Ids)
    {
        bool updated = false;

        // === 1. 拼音排序 & 搜索 ===
        if (config.EnablePinyinSort || config.EnablePinyinSearch)
        {
            updated |= ProcessPinyin(item, config);
        }

        // === 2. 国家标签 ===
        if (config.EnableCountryAsTag)
        {
            if (CountryTagHelper.TryAddCountryTag(item, config, _logger, "PinyinSeekTask"))
            {
                updated = true;
            }
        }

        // === 3. IMDb Top 250 标签（仅 Movie）===
        if (config.EnableImdbTopTag && item is Movie movie)
        {
            var imdbId = movie.ProviderIds?.GetValueOrDefault("Imdb");
            if (!string.IsNullOrEmpty(imdbId) && imdbTop250Ids.Contains(imdbId))
            {
                if (!HasTag(movie, "IMDb Top 250"))
                {
                    AddTag(movie, "IMDb Top 250");
                    updated = true;
                    _logger.Debug($"[PinyinSeekTask] 添加 IMDb Top 250 标签: {movie.Name} (ID: {imdbId})");
                }
            }
        }

        return updated;
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

        // --- 锁定 SortName 字段（无论是否更新，只要启用了拼音功能就应锁定）---
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

    // === 以下为 IMDb 缓存相关方法（与原 PinyinTagsTask 一致）===

    private async Task TryUpdateImdbTop250Json()
    {
        var jsonPath = Path.Combine(_appPaths.ConfigurationDirectoryPath, "imdb_top250.json");
        var url = "https://raw.githubusercontent.com/theapache64/top250/master/top250_min.json";

        try
        {
            using var httpClient = new HttpClient();
            var jsonContent = await httpClient.GetStringAsync(url);

            if (jsonContent.TrimStart().StartsWith("["))
            {
                await File.WriteAllTextAsync(jsonPath, jsonContent);
                _logger.Info("[PinyinSeekTask] 成功从网络更新 IMDb Top 250 缓存。");
            }
            else
            {
                _logger.Warn("[PinyinSeekTask] 下载的 IMDb Top 250 数据格式无效，未更新本地缓存。");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("[PinyinSeekTask] 从网络更新 IMDb Top 250 失败，将使用本地缓存（如果存在）。", ex);
        }
    }

    private HashSet<string> LoadImdbTop250IdsFromLocal()
    {
        var jsonPath = Path.Combine(_appPaths.ConfigurationDirectoryPath, "imdb_top250.json");

        if (!File.Exists(jsonPath))
        {
            _logger.Debug("[PinyinSeekTask] 本地 IMDb Top 250 缓存不存在。");
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
            _logger.Debug($"[PinyinSeekTask] 成功加载 {ids.Count} 个 IMDb Top 250 ID。");
            return ids;
        }
        catch (Exception ex)
        {
            _logger.Error("[PinyinSeekTask] 解析本地 IMDb Top 250 缓存失败。", ex);
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool HasTag(BaseItem item, string tag)
        => item.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true;

    private static void AddTag(BaseItem item, string tag)
    {
        var tags = (item.Tags ?? Array.Empty<string>()).ToList();
        tags.Add(tag);
        item.Tags = tags.ToArray();
    }

    private class ImdbMovie
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("year")]
        public int Year { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("imdb_url")]
        public string? ImdbUrlRaw { get; set; }

        public string? Id
        {
            get
            {
                if (string.IsNullOrEmpty(ImdbUrlRaw)) return null;
                var parts = ImdbUrlRaw.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.FirstOrDefault(p => p.StartsWith("tt", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
