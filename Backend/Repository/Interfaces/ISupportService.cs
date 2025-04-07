using Backend.DTOs;

namespace Backend.Repository.Interfaces
{
    public interface ISupportService
    {
        Task HandleSupportMessageAsync(SupportMessageDto dto);
    }
}
