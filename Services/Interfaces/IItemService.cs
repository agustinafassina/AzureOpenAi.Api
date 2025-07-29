using OpenAi.Api.Services.Dto;

namespace OpenAi.Api.Services.Interfaces
{
    public interface IItemService
    {
        IEnumerable<ItemDto> GetAllItems();
        ItemDto GetItemById(int id);
        ItemDto CreateItem(ItemCreateDto newItem);
    }
}