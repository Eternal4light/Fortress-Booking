namespace UserService.Services;

/// <summary>
/// Раз в минуту переводит в статус Expired приглашения, на которые клиент не ответил
/// за 24 часа (автоматическое отклонение). Освобождённые места снова доступны для новых
/// приглашений. Корректность лимита от этой очистки не зависит: просроченные приглашения
/// и так не учитываются как занятые места, очистка лишь приводит данные в порядок.
/// </summary>
public sealed class InviteExpirationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IUserInviteService _inviteService;
    private readonly ILogger<InviteExpirationWorker> _logger;

    public InviteExpirationWorker(IUserInviteService inviteService, ILogger<InviteExpirationWorker> logger)
    {
        _inviteService = inviteService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                try
                {
                    var expired = await _inviteService.ExpireOverdueAsync(stoppingToken);
                    if (expired > 0)
                    {
                        _logger.LogInformation("Автоматически отклонено просроченных приглашений: {Count}", expired);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // MongoDB может быть временно недоступна — просто попробуем на следующем тике
                    _logger.LogWarning(ex, "Не удалось отклонить просроченные приглашения, повтор через минуту");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // сервис останавливается
        }
    }
}
