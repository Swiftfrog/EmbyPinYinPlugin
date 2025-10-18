// Providers/PinyinCustomMetadataProvider.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies; // Movie
using MediaBrowser.Controller.Entities.TV; // Series, Episode
using MediaBrowser.Controller.Providers; // ICustomMetadataProvider, ItemInfo, IDirectoryService
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
    public class PinyinCustomMetadataProvider : ICustomMetadataProvider<Movie>, IHasOrder
    {
        // 无参数构造函数 (如果需要依赖注入，请添加相应参数)
        public PinyinCustomMetadataProvider()
        {
        }

        public string Name => "Pinyin Custom Sorter";

        // 实现 IHasOrder 接口，返回较低的数字以获得较高优先级
        public int Order => 0; // 在同类提供者中拥有最高优先级

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Movie> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            // 1. 获取媒体项
            var item = itemResult.Item;

            // 2. 获取 Name (或 OriginalTitle)
            var nameToProcess = item.Name; // 或 item.OriginalTitle，根据需求
            if (string.IsNullOrEmpty(nameToProcess))
            {
                // 如果没有名称，则不进行处理
                return ItemUpdateType.None;
            }

            // 3. 计算拼音简写 (同步操作)
            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);

            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 // 如果计算失败，则不进行处理
                 return ItemUpdateType.None;
            }

            // 4. 设置 SortName
            item.SetSortNameDirect(pinyinInitials.ToUpper()); // 关键：设置拼音首字母

            // 5. 返回更新类型，告知 Emby 我们更新了元数据
            return ItemUpdateType.Metadata;
        }
    }

    // 你可以为其他类型创建类似的提供者
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
            var nameToProcess = item.Name;
            if (string.IsNullOrEmpty(nameToProcess))
            {
                return ItemUpdateType.None;
            }

            string pinyinInitials = PinyinHelper.GetPinyinInitials(nameToProcess);
            if (string.IsNullOrEmpty(pinyinInitials))
            {
                 return ItemUpdateType.None;
            }

            item.SetSortNameDirect(pinyinInitials.ToUpper());
            return ItemUpdateType.Metadata;
        }
    }

    // ... 为其他类型 (Episode, MusicAlbum, MusicArtist, Video, Photo, BoxSet) 创建提供者 ...
}
