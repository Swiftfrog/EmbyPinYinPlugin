// Providers/PinyinCustomMetadataProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies; // Movie
using MediaBrowser.Controller.Entities.TV; // Series, Episode
using MediaBrowser.Controller.Entities.Audio; // MusicAlbum, MusicArtist
using MediaBrowser.Controller.Providers; // ICustomMetadataProvider, ItemInfo, IDirectoryService
using MediaBrowser.Controller.Library; // 包含 ItemUpdateType
using MediaBrowser.Model.Configuration; // 包含 LibraryOptions
using System.Threading; // 包含 CancellationToken
using System.Threading.Tasks; // 包含 Task
// 移除 Microsoft.Extensions.Logging 的 using
// using Microsoft.Extensions.Logging; // 包含 ILogger<T>
using System; // 包含 Activator
using EmbyPinyinPlugin.Utils; // 包含 PinyinHelper

namespace EmbyPinyinPlugin.Providers
{
    /// <summary>
    /// 为电影元数据提供拼音首字母排序功能。
    /// 实现 ICustomMetadataProvider 和 IHasOrder 以在元数据合并后期进行干预，并确保优先级。
    /// </summary>
    public class PinyinCustomMetadataProviderMovie : ICustomMetadataProvider<Movie>, IHasOrder
    {
        // 无参数构造函数 (如果需要依赖注入，请添加相应参数)
        public PinyinCustomMetadataProviderMovie()
        {
        }

        public string Name => "Pinyin Custom Sorter Movie";

        // 实现 IHasOrder 接口，返回较低的数字以获得较高优先级
        public int Order => 0; // 在同类提供者中拥有最高优先级

        // public async Task<ItemUpdateType> FetchAsync(
        //     MetadataResult<Movie> itemResult,
        //     MetadataRefreshOptions options,
        //     LibraryOptions libraryOptions,
        //     CancellationToken cancellationToken)
        // {
        //     var item = itemResult.Item;

        //     // 1. 获取 Name (或 OriginalTitle) 用于计算拼音
        //     var nameToProcess = item.Name; // 或 item.OriginalTitle，根据需求
        //     if (string.IsNullOrEmpty(nameToProcess))
        //     {
        //         return ItemUpdateType.None;
        //     }

        //     // 2. 计算拼音简写
        //     string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
        //     if (string.IsNullOrEmpty(pinyinInitials))
        //     {
        //          return ItemUpdateType.None;
        //     }

        //     // 3. 设置 SortName (保持原有逻辑)
        //     item.SetSortNameDirect(pinyinInitials.ToUpper());

        //     // 4. 更新 OriginalTitle (新增逻辑)
        //     var currentOriginalTitle = item.OriginalTitle ?? string.Empty; // 处理 null 情况
        //     // 只有当 OriginalTitle 不包含我们附加的拼音部分时，才进行更新
        //     // 这可以避免重复附加
        //     if (!currentOriginalTitle.Contains($" #{pinyinInitials.ToUpper()}"))
        //     {
        //         item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
        //             ? pinyinInitials.ToUpper() 
        //             : $"{currentOriginalTitle} #{pinyinInitials.ToUpper()}";
        //     }

        //     // 5. 返回更新类型
        //     return ItemUpdateType.MetadataEdit; 
        //     // 或者更具体的类型，如 MetadataEdit | ImageUpdate (如果也修改了图片相关)
        // }

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Movie> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;

            // 1. 获取 Name (或 OriginalTitle) 用于计算拼音
            var nameToProcess = item.Name; // 或 item.OriginalTitle，根据需求
            if (string.IsNullOrEmpty(nameToProcess))
            {
                return ItemUpdateType.None;
            }

            // 2. 新增：判断是否包含中文字符
            if (!PinyinHelper.ContainsChinese(nameToProcess))
            {
                // 如果不包含中文字符，则不进行拼音处理，直接返回
                return ItemUpdateType.None;
            }

            // 3. 计算拼音简写
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 return ItemUpdateType.None;
            }

            // 4. 设置 SortName
            item.SetSortNameDirect(pinyinInitials.ToUpper());

            // 5. 锁定 SortName 字段，防止被后续流程覆盖
            item.LockedFields = new [] { MetadataFields.SortName };

            // 6. 更新 OriginalTitle
            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            if (!currentOriginalTitle.Contains($" #{pinyinInitials.ToUpper()}"))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
                    ? pinyinInitials.ToUpper() 
                    : $"{currentOriginalTitle} #{pinyinInitials.ToUpper()}";
            }

            // 7. 返回更新类型
            return ItemUpdateType.MetadataEdit;
        }
        
    }

    /// <summary>
    /// 为电视剧元数据提供拼音首字母排序功能。
    /// </summary>
    public class PinyinCustomMetadataProviderSeries : ICustomMetadataProvider<Series>, IHasOrder
    {
        public PinyinCustomMetadataProviderSeries()
        {
        }

        public string Name => "Pinyin Custom Sorter Series";

        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Series> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            // 1.获取name
            var nameToProcess = item.Name;
            if (string.IsNullOrEmpty(nameToProcess))
            {
                return ItemUpdateType.None;
            }

            // 2. 判断是否包含中文字符
            if (!PinyinHelper.ContainsChinese(nameToProcess))
            {
                // 如果不包含中文字符，则不进行拼音处理，直接返回
                return ItemUpdateType.None;
            }
            // 3. 计算拼音简写
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 return ItemUpdateType.None;
            }
            
            // 4. 设置 SortName
            item.SetSortNameDirect(pinyinInitials.ToUpper());

            // 5. 锁定 SortName 字段，防止被后续流程覆盖
            item.LockedFields = new [] { MetadataFields.SortName };
            
            // 6. 更新 OriginalTitle
            var currentOriginalTitle = item.OriginalTitle ?? string.Empty; // 处理 null 情况
            // 只有当 OriginalTitle 不包含我们附加的拼音部分时，才进行更新，这可以避免重复附加
            if (!currentOriginalTitle.Contains($" #{pinyinInitials.ToUpper()}"))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
                    ? pinyinInitials.ToUpper() 
                    : $"{currentOriginalTitle} #{pinyinInitials.ToUpper()}";
            }
            
            // 7. 返回更新类型
            return ItemUpdateType.MetadataEdit;
        }
    }

    /// <summary>
    /// 为剧集元数据提供拼音首字母排序功能。
    /// </summary>
    public class PinyinCustomMetadataProviderEpisode : ICustomMetadataProvider<Episode>, IHasOrder
    {
        public PinyinCustomMetadataProviderEpisode()
        {
        }

        public string Name => "Pinyin Custom Sorter Episode";

        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Episode> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            // 1. 获取 Name (或 OriginalTitle) 用于计算拼音
            var nameToProcess = item.Name; // 或 item.OriginalTitle
            if (string.IsNullOrEmpty(nameToProcess))
            {
                return ItemUpdateType.None;
            }
            // 2. 新增：判断是否包含中文字符
            if (!PinyinHelper.ContainsChinese(nameToProcess))
            {
                // 如果不包含中文字符，则不进行拼音处理，直接返回
                return ItemUpdateType.None;
            }
            
            // 3. 计算拼音简写
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 return ItemUpdateType.None;
            }
            
            // 4. 设置 SortName
            item.SetSortNameDirect(pinyinInitials.ToUpper());
            
            // 5. 锁定 SortName 字段，防止被后续流程覆盖
            item.LockedFields = new [] { MetadataFields.SortName };
            
            // 6. 更新 OriginalTitle，剧集的单集的标题就不添加搜索了，否则大堆第一集DYJ
            
            // 7. 返回更新类型
            return ItemUpdateType.MetadataEdit;
        }
    }

    // ... 为其他类型 (MusicAlbum, MusicArtist, Video, Photo, BoxSet) 创建提供者 ...
    // 例如 MusicAlbum
    // public class PinyinCustomMetadataProviderMusicAlbum : ICustomMetadataProvider<MusicAlbum>, IHasOrder
    // {
    //     public PinyinCustomMetadataProviderMusicAlbum()
    //     {
    //     }

    //     public string Name => "Pinyin Custom Sorter MusicAlbum";

    //     public int Order => 0;

    //     public async Task<ItemUpdateType> FetchAsync(
    //         MetadataResult<MusicAlbum> itemResult,
    //         MetadataRefreshOptions options,
    //         LibraryOptions libraryOptions,
    //         CancellationToken cancellationToken)
    //     {
    //         var item = itemResult.Item;
    //         var nameToProcess = item.Name; // 或 item.OriginalTitle
    //         if (string.IsNullOrEmpty(nameToProcess))
    //         {
    //             return ItemUpdateType.None;
    //         }

    //         string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
    //         if (string.IsNullOrEmpty(pinyinInitials))
    //         {
    //              return ItemUpdateType.None;
    //         }

    //         item.SetSortNameDirect(pinyinInitials.ToUpper());
    //         return ItemUpdateType.MetadataEdit;
    //     }
    // }

    // 例如 MusicArtist
    // public class PinyinCustomMetadataProviderMusicArtist : ICustomMetadataProvider<MusicArtist>, IHasOrder
    // {
    //     public PinyinCustomMetadataProviderMusicArtist()
    //     {
    //     }

    //     public string Name => "Pinyin Custom Sorter MusicArtist";

    //     public int Order => 0;

    //     public async Task<ItemUpdateType> FetchAsync(
    //         MetadataResult<MusicArtist> itemResult,
    //         MetadataRefreshOptions options,
    //         LibraryOptions libraryOptions,
    //         CancellationToken cancellationToken)
    //     {
    //         var item = itemResult.Item;
    //         var nameToProcess = item.Name; // 或 item.OriginalTitle
    //         if (string.IsNullOrEmpty(nameToProcess))
    //         {
    //             return ItemUpdateType.None;
    //         }

    //         string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
    //         if (string.IsNullOrEmpty(pinyinInitials))
    //         {
    //              return ItemUpdateType.None;
    //         }

    //         item.SetSortNameDirect(pinyinInitials.ToUpper());
    //         return ItemUpdateType.MetadataEdit;
    //     }
    // }

    // // 例如 Video (独立视频文件)
    // public class PinyinCustomMetadataProviderVideo : ICustomMetadataProvider<Video>, IHasOrder
    // {
    //     public PinyinCustomMetadataProviderVideo()
    //     {
    //     }

    //     public string Name => "Pinyin Custom Sorter Video";

    //     public int Order => 0;

    //     public async Task<ItemUpdateType> FetchAsync(
    //         MetadataResult<Video> itemResult,
    //         MetadataRefreshOptions options,
    //         LibraryOptions libraryOptions,
    //         CancellationToken cancellationToken)
    //     {
    //         var item = itemResult.Item;
    //         var nameToProcess = item.Name; // 或 item.OriginalTitle
    //         if (string.IsNullOrEmpty(nameToProcess))
    //         {
    //             return ItemUpdateType.None;
    //         }

    //         string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
    //         if (string.IsNullOrEmpty(pinyinInitials))
    //         {
    //              return ItemUpdateType.None;
    //         }

    //         item.SetSortNameDirect(pinyinInitials.ToUpper());
    //         return ItemUpdateType.MetadataEdit;
    //     }
    // }

    // 例如 Photo (独立图片文件)
    // public class PinyinCustomMetadataProviderPhoto : ICustomMetadataProvider<Photo>, IHasOrder
    // {
    //     public PinyinCustomMetadataProviderPhoto()
    //     {
    //     }

    //     public string Name => "Pinyin Custom Sorter Photo";

    //     public int Order => 0;

    //     public async Task<ItemUpdateType> FetchAsync(
    //         MetadataResult<Photo> itemResult,
    //         MetadataRefreshOptions options,
    //         LibraryOptions libraryOptions,
    //         CancellationToken cancellationToken)
    //     {
    //         var item = itemResult.Item;
    //         var nameToProcess = item.Name; // 或 item.OriginalTitle
    //         if (string.IsNullOrEmpty(nameToProcess))
    //         {
    //             return ItemUpdateType.None;
    //         }

    //         string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
    //         if (string.IsNullOrEmpty(pinyinInitials))
    //         {
    //              return ItemUpdateType.None;
    //         }

    //         item.SetSortNameDirect(pinyinInitials.ToUpper());
    //         return ItemUpdateType.MetadataEdit;
    //     }
    // }

    // 例如 BoxSet (合集)
    public class PinyinCustomMetadataProviderBoxSet : ICustomMetadataProvider<BoxSet>, IHasOrder
    {
        public PinyinCustomMetadataProviderBoxSet()
        {
        }

        public string Name => "Pinyin Custom Sorter BoxSet";

        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<BoxSet> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            var nameToProcess = item.Name; // 或 item.OriginalTitle
            if (string.IsNullOrEmpty(nameToProcess))
            {
                return ItemUpdateType.None;
            }

            // 2. 新增：判断是否包含中文字符
            if (!PinyinHelper.ContainsChinese(nameToProcess))
            {
                // 如果不包含中文字符，则不进行拼音处理，直接返回
                return ItemUpdateType.None;
            }

            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 return ItemUpdateType.None;
            }

            item.SetSortNameDirect(pinyinInitials.ToUpper());

            // 5. 锁定 SortName 字段，防止被后续流程覆盖
            item.LockedFields = new [] { MetadataFields.SortName };
            
            // 6. 更新 OriginalTitle
            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            if (!currentOriginalTitle.Contains($" #{pinyinInitials.ToUpper()}"))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle) 
                    ? pinyinInitials.ToUpper() 
                    : $"{currentOriginalTitle} #{pinyinInitials.ToUpper()}";
            }
            
            return ItemUpdateType.MetadataEdit;
        }
    }
}
