namespace PartnerService.WebHost.Models
{
    /// <summary>
    /// Запрос на добавление коворкинга (партнёра)
    /// </summary>
    public class CreatePartnerRequest
    {
        /// <summary>
        /// Название коворкинга
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Специализация коворкинга (например: бьюти, офисный)
        /// </summary>
        public string? Specialization { get; set; }

        /// <summary>
        /// Вместимость коворкинга — сколько клиентов максимум можно пригласить (не меньше 1)
        /// </summary>
        public int Capacity { get; set; }
    }
}
