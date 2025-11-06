//PinyinSortProvider.cs
// using MediaBrowser.Controller.Entities;
// using MediaBrowser.Controller.Entities.Movies; // Movie
// using MediaBrowser.Controller.Entities.TV; // Series, Episode
// using MediaBrowser.Controller.Entities.Audio; // MusicAlbum, MusicArtist
// using MediaBrowser.Controller.Providers; // ICustomMetadataProvider, ItemInfo, IDirectoryService
// using MediaBrowser.Controller.Library; // 包含 ItemUpdateType
// using MediaBrowser.Model.Configuration; // 包含 LibraryOptions
// using System.Threading; // 包含 CancellationToken
// using System.Threading.Tasks; // 包含 Task
// using MediaBrowser.Model.Entities; // 包含 MetadataFields
// using System; // 包含 Activator
// using PinYinSort.Utils; // 包含 PinyinHelper

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using PinYinSort.Utils;

namespace PinYinSort.Providers;

/// <summary>
/// 通用辅助方法
/// </summary>
public static class PinyinProviderHelper
{
    public static bool ShouldUpdateSortName(BaseItem item, string expectedPinyin, PinYinSortConfig config)
    {
        var current = item.SortName;

        // 1. 如果禁用排序功能，不更新
        if (!config.EnablePinyinSort)
            return false;

        // 2. 保守模式：仅当为空时填充
        if (config.OnlyFillWhenEmpty && !string.IsNullOrEmpty(current))
            return false;

        // 3. 字段被锁定 → 插件全权负责
        if (item.LockedFields?.Contains(MetadataFields.SortName) == true)
        {
            return !string.Equals(current, expectedPinyin, StringComparison.Ordinal);
        }

        // 4. 当前为空 → 需要填充
        if (string.IsNullOrEmpty(current))
            return true;

        // 5. 当前含中文 → 不适合排序 → 应更新
        if (PinyinHelper.ContainsChinese(current))
            return true;

        // 6. 其他情况（如英文、符号）→ 用户自定义 → 不覆盖
        return false;
    }

    public static void SafeAddLockedField(BaseItem item, MetadataFields field)
    {
        var current = item.LockedFields ?? System.Array.Empty<MetadataFields>();
        if (!current.Contains(field))
        {
            item.LockedFields = current.Concat(new[] { field }).ToArray();
        }
    }
}

// =============== Movie Provider ===============
public class PinYinSortProviderMovie : ICustomMetadataProvider<Movie>, IHasOrder
{
    public string Name => "Pinyin Sort & Search Provider (Movie)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Movie> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess) || !PinyinHelper.ContainsChinese(nameToProcess))
            return ItemUpdateType.None;

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
            return ItemUpdateType.None;

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        // --- 处理 SortName ---
        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
        }

        // --- 处理 OriginalTitle（拼音搜索）---
        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== Series Provider ===============
public class PinYinSortProviderSeries : ICustomMetadataProvider<Series>, IHasOrder
{
    public string Name => "Pinyin Sort & Search Provider (Series)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Series> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess) || !PinyinHelper.ContainsChinese(nameToProcess))
            return ItemUpdateType.None;

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
            return ItemUpdateType.None;

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
        }

        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}

// =============== Episode Provider ===============
public class PinYinSortProviderEpisode : ICustomMetadataProvider<Episode>, IHasOrder
{
    public string Name => "Pinyin Sort Provider (Episode)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<Episode> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess) || !PinyinHelper.ContainsChinese(nameToProcess))
            return ItemUpdateType.None;

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
            return ItemUpdateType.None;

        var pinyinUpper = pinyinInitials.ToUpper();

        // Episode 通常不处理 OriginalTitle（避免每集都加标签）
        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            return ItemUpdateType.MetadataEdit;
        }

        return ItemUpdateType.None;
    }
}

// =============== BoxSet Provider ===============
public class PinYinSortProviderBoxSet : ICustomMetadataProvider<BoxSet>, IHasOrder
{
    public string Name => "Pinyin Sort & Search Provider (BoxSet)";
    public int Order => 0;

    public async Task<ItemUpdateType> FetchAsync(
        MetadataResult<BoxSet> itemResult,
        MetadataRefreshOptions options,
        LibraryOptions libraryOptions,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var item = itemResult.Item;
        var nameToProcess = item.Name;

        if (string.IsNullOrEmpty(nameToProcess) || !PinyinHelper.ContainsChinese(nameToProcess))
            return ItemUpdateType.None;

        string pinyinInitials = await Task.Run(() => PinyinHelper.GetPinyinInitials(nameToProcess), cancellationToken);
        if (string.IsNullOrEmpty(pinyinInitials))
            return ItemUpdateType.None;

        var pinyinUpper = pinyinInitials.ToUpper();
        bool updated = false;

        if (PinyinProviderHelper.ShouldUpdateSortName(item, pinyinUpper, config))
        {
            item.SetSortNameDirect(pinyinUpper);
            PinyinProviderHelper.SafeAddLockedField(item, MetadataFields.SortName);
            updated = true;
        }

        if (config.EnablePinyinSearch)
        {
            var currentOT = item.OriginalTitle ?? string.Empty;
            var tag = $" #{pinyinUpper}";
            if (!currentOT.Contains(tag))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOT) ? pinyinUpper : $"{currentOT}{tag}";
                updated = true;
            }
        }

        return updated ? ItemUpdateType.MetadataEdit : ItemUpdateType.None;
    }
}