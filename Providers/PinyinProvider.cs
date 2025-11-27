// PinyinProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;

using PinyinSeek.Utils;

namespace PinyinSeek.Providers;

public abstract class BasePinyinProvider<T> : ICustomMetadataProvider<T>, IHasOrder
    where T : BaseItem
{
    protected readonly ILogger _logger;
    protected readonly string _typeName;

    public BasePinyinProvider(ILogger logger, string typeName)
    {
        _logger = logger;
        _typeName = typeName;
    }

    public int Order => 0;

    public string Name => $"Pinyin Sort & Search Provider ({_typeName})";

    public abstract Task<ItemUpdateType> FetchAsync(
        MetadataResult<T> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken);

    protected bool TryAddCountryTag(T item, PinyinSeekConfig config)
    {
        if (!config.EnableCountryAsTag)
            return false;

        // 从 ProductionLocations 获取主产国
        // var country = item.ProductionLocations?.FirstOrDefault();
        ////映射英语国家到中文
        var originalCountry = item.ProductionLocations?.FirstOrDefault();
        if (string.IsNullOrEmpty(originalCountry))
            return false;
        
        var displayCountry = CountryMapper.GetLocalizedCountry(originalCountry);
        
        // if (string.IsNullOrEmpty(country))
        //     _logger.Debug($"[PinyinSeek]: 没有OriginalCountry值。")
        //     return false;

        // 避免重复添加
        if (item.Tags?.Contains(displayCountry, StringComparer.OrdinalIgnoreCase) == true)
            return false;

        // 添加国家标签
        var newTags = (item.Tags ?? Array.Empty<string>()).ToList();
        newTags.Add(displayCountry);
        item.Tags = newTags.ToArray();
        // _logger.Debug($"[PinyinSeek] {_typeName}: 已添加国家标签 \"{displayCountry}\"。ID: {item.Id}");
        _logger.Debug($"[PinyinSeek] {_typeName}: 已添加国家标签 \"{displayCountry}\"（原值: {originalCountry}）。ID: {item.Id}");
        return true;
    }
}

// =============== Movie Provider ===============
public class PinyinProviderMovie : BasePinyinProvider<Movie>
{
    public PinyinProviderMovie(ILogger logger) : base(logger, "Movie") { }

    public override async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Movie> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinyinSeek] Movie: 名称为空，跳过处理。Item ID: {0}", item.Id);
            return ItemUpdateType.None;
        }

        var updated = false;

        // === 拼音处理 ===
        if (PinyinHelper.ContainsChinese(nameToProcess))
        {
            string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
            if (!string.IsNullOrEmpty(pinyinInitials))
            {
                var pinyinUpper = pinyinInitials.ToUpper();

                if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
                {
                    item.SetSortNameDirect(pinyinUpper);
                    PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
                    updated = true;
                    _logger.Debug("[PinyinSeek] Movie: 已更新 SortName 为 {0}。名称: {1}, ID: {2}", pinyinUpper, item.Name, item.Id);
                }

                if (config.EnablePinyinSearch)
                {
                    var currentOT = item.OriginalTitle ?? string.Empty;
                    var tag = $" #{pinyinUpper}";
                    if (!currentOT.Contains(tag))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                        updated = true;
                        _logger.Debug("[PinyinSeek] Movie: 已更新 OriginalTitle。新值: {0}, ID: {1}", item.OriginalTitle, item.Id);
                    }
                }
            }
        }

        // === 国家标签处理（无论是否中文）===
        if (TryAddCountryTag(item, config))
        {
            updated = true;
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== Series Provider ===============
public class PinyinProviderSeries : BasePinyinProvider<Series>
{
    public PinyinProviderSeries(ILogger logger) : base(logger, "Series") { }

    public override async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Series> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinyinSeek] Series: 名称为空，跳过处理。Item ID: {0}", item.Id);
            return ItemUpdateType.None;
        }

        var updated = false;

        // === 拼音处理 ===
        if (PinyinHelper.ContainsChinese(nameToProcess))
        {
            string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
            if (!string.IsNullOrEmpty(pinyinInitials))
            {
                var pinyinUpper = pinyinInitials.ToUpper();

                if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
                {
                    item.SetSortNameDirect(pinyinUpper);
                    PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
                    updated = true;
                    _logger.Debug("[PinyinSeek] Series: 已更新 SortName 为 {0}。名称: {1}, ID: {2}", pinyinUpper, item.Name, item.Id);
                }

                if (config.EnablePinyinSearch)
                {
                    var currentOT = item.OriginalTitle ?? string.Empty;
                    var tag = $" #{pinyinUpper}";
                    if (!currentOT.Contains(tag))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                        updated = true;
                        _logger.Debug("[PinyinSeek] Series: 已更新 OriginalTitle。新值: {0}, ID: {1}", item.OriginalTitle, item.Id);
                    }
                }
            }
        }

        // === 国家标签处理（无论是否中文）===
        if (TryAddCountryTag(item, config))
        {
            updated = true;
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== BoxSet Provider ===============
public class PinyinProviderBoxSet : BasePinyinProvider<BoxSet>
{
    public PinyinProviderBoxSet(ILogger logger) : base(logger, "BoxSet") { }

    public override async Task<ItemUpdateType> FetchAsync(
        MetadataResult<BoxSet> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess))
        {
            _logger.Debug("[PinyinSeek] BoxSet: 名称为空，跳过处理。Item ID: {0}", item.Id);
            return ItemUpdateType.None;
        }

        var updated = false;

        // === 拼音处理 ===
        if (PinyinHelper.ContainsChinese(nameToProcess))
        {
            string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
            if (!string.IsNullOrEmpty(pinyinInitials))
            {
                var pinyinUpper = pinyinInitials.ToUpper();

                if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
                {
                    item.SetSortNameDirect(pinyinUpper);
                    PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
                    updated = true;
                    _logger.Debug("[PinyinSeek] BoxSet: 已更新 SortName 为 {0}。名称: {1}, ID: {2}", pinyinUpper, item.Name, item.Id);
                }

                if (config.EnablePinyinSearch)
                {
                    var currentOT = item.OriginalTitle ?? string.Empty;
                    var tag = $" #{pinyinUpper}";
                    if (!currentOT.Contains(tag))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                        updated = true;
                        _logger.Debug("[PinyinSeek] BoxSet: 已更新 OriginalTitle。新值: {0}, ID: {1}", item.OriginalTitle, item.Id);
                    }
                }
            }
        }

        // === 国家标签处理（无论是否中文）===
        if (TryAddCountryTag(item, config))
        {
            updated = true;
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}