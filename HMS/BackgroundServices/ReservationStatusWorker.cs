using HMS.Application.Contracts.Presistence;
using HMS.Domain.Enum;

namespace HMS.BackgroundServices
{
    public class ReservationStatusWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationStatusWorker> _logger;

        public ReservationStatusWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationStatusWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateReservationStatusesAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to update reservation statuses");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromHours(1),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task UpdateReservationStatusesAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var reservationRepository =
                scope.ServiceProvider
                    .GetRequiredService<IReservationRepository>();

            var today = DateTime.UtcNow.Date;

            var reservations =
                await reservationRepository.GetAllAsync(
                    filter: reservation =>
                        reservation.Status !=
                            ReservationStatusFilter.Cancelled

                        && reservation.Status !=
                            ReservationStatusFilter.Complete,

                    cancellationToken: cancellationToken,
                    tracikng: true);

            var changed = false;

            foreach (var reservation in reservations.Items)
            {
                ReservationStatusFilter newStatus;

                if (reservation.CheckOutDate.Date <= today)
                {
                    newStatus =
                        ReservationStatusFilter.Past;
                }
                else if (
                    reservation.CheckInDate.Date <= today)
                {
                    newStatus =
                        ReservationStatusFilter.Active;
                }
                else
                {
                    newStatus =
                        ReservationStatusFilter.Reserved;
                }

                if (reservation.Status == newStatus)
                    continue;

                reservation.Status = newStatus;

                changed = true;
            }

            if (changed)
            {
                await reservationRepository.SaveAsync(
                    cancellationToken);
            }
        }
    }
}
