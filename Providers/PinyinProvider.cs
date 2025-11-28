// PinyinProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
using MediaBrowser.Model.Providers; // 添加这个命名空间

namespace PinyinSeek.Providers;

// =============== 基础提供者类（复用代码）==============
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

    /// <summary>
    /// 通用方法：从 ItemLookupInfo 中提取 origin_country（TMDb 的 origin_country）
    /// </summary>
    protected string? GetOriginCountryFromLookupInfo(ItemLookupInfo lookupInfo)
    {
        // 正确获取 ProductionLocations
        if (lookupInfo.ProductionLocations?.Count > 0)
        {
            return lookupInfo.ProductionLocations[0];
        }
        return null;
    }

    /// <summary>
    /// 通用方法：尝试添加国家标签
    /// </summary>
    protected bool TryAddCountryTag(BaseItem item, string? originCountry, ILogger logger, string typeName)
    {
        if (string.IsNullOrEmpty(originCountry))
            return false;

        string? displayCountry = CountryMapper.GetLocalizedCountry(originCountry);
        if (string.IsNullOrEmpty(displayCountry))
            return false;

        if (item.Tags?.Contains(displayCountry, StringComparer.OrdinalIgnoreCase) == true)
            return false;

        var tags = (item.Tags ?? Array.Empty<string>()).ToList();
        tags.Add(displayCountry);
        item.Tags = tags.ToArray();

        logger.Debug($"[PinyinSeek] {typeName}: 已添加国家标签 \"{displayCountry}\"（原值: {originCountry}）。ID: {item.Id}");
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
            _logger.Debug($"[PinyinSeek] Movie: 名称为空，跳过处理。Item ID: {item.Id}");
            return ItemUpdateType.None;
        }

        bool updated = false;

        // 处理拼音
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
                    _logger.Debug($"[PinyinSeek] Movie: 已更新 SortName 为 {pinyinUpper}。名称: {item.Name}, ID: {item.Id}");
                }

                if (config.EnablePinyinSearch)
                {
                    var currentOT = item.OriginalTitle ?? string.Empty;
                    var tag = $" #{pinyinUpper}";
                    if (!currentOT.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                        updated = true;
                        _logger.Debug($"[PinyinSeek] Movie: 已更新 OriginalTitle。新值: {item.OriginalTitle}, ID: {item.Id}");
                    }
                }
            }
        }

        // 处理国家标签
        if (config.EnableCountryAsTag && itemResult.Result != null)
        {
            string? originCountry = GetOriginCountryFromLookupInfo(itemResult.Result);
            if (TryAddCountryTag(item, originCountry, _logger, _typeName))
            {
                updated = true;
            }
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
            _logger.Debug($"[PinyinSeek] Series: 名称为空，跳过处理。Item ID: {item.Id}");
            return ItemUpdateType.None;
        }

        bool updated = false;

        // 处理拼音
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
                    _logger.Debug($"[PinyinSeek] Series: 已更新 SortName 为 {pinyinUpper}。名称: {item.Name}, ID: {item.Id}");
                }

                if (config.EnablePinyinSearch)
                {
                    var currentOT = item.OriginalTitle ?? string.Empty;
                    var tag = $" #{pinyinUpper}";
                    if (!currentOT.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                        updated = true;
                        _logger.Debug($"[PinyinSeek] Series: 已更新 OriginalTitle。新值: {item.OriginalTitle}, ID: {item.Id}");
                    }
                }
            }
        }

        // 处理国家标签
        if (config.EnableCountryAsTag && itemResult.Result != null)
        {
            string? originCountry = GetOriginCountryFromLookupInfo(itemResult.Result);
            if (TryAddCountryTag(item, originCountry, _logger, _typeName))
            {
                updated = true;
            }
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
            _logger.Debug($"[PinyinSeek] BoxSet: 名称为空，跳过处理。Item ID: {item.Id}");
            return ItemUpdateType.None;
        }

        bool updated = false;

        // 处理拼音
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
                    _logger.Debug($"[PinyinSeek] BoxSet: 已更新 SortName 为 {pinyinUpper}。名称: {item.Name}, ID: {item.Id}");
                }

                if (config.EnablePinyinSearch)
                {
                    var currentOT = item.OriginalTitle ?? string.Empty;
                    var tag = $" #{pinyinUpper}";
                    if (!currentOT.Contains(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                        updated = true;
                        _logger.Debug($"[PinyinSeek] BoxSet: 已更新 OriginalTitle。新值: {item.OriginalTitle}, ID: {item.Id}");
                    }
                }
            }
        }

        // 处理国家标签
        if (config.EnableCountryAsTag && itemResult.Result != null)
        {
            string? originCountry = GetOriginCountryFromLookupInfo(itemResult.Result);
            if (TryAddCountryTag(item, originCountry, _logger, _typeName))
            {
                updated = true;
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}