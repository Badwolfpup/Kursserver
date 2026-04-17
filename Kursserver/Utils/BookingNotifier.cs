using Kursserver.Models;

namespace Kursserver.Utils
{
    public class BookingNotifier
    {
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _db;

        public BookingNotifier(EmailService emailService, ApplicationDbContext db)
        {
            _emailService = emailService;
            _db = db;
        }

        public async Task NotifyBookingCreated(Booking booking, BookingActor creator)
        {
            var timeStr = $"{booking.StartTime:g}–{booking.EndTime:t}";

            if (creator == BookingActor.Admin)
            {
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Ny mötesförfrågan",
                            $"Du har fått en mötesförfrågan {timeStr}. Logga in för att bekräfta.");
                }
            }
            else
            {
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Ny mötesförfrågan från coach",
                        $"Du har fått en mötesförfrågan {timeStr}. Logga in för att bekräfta.");
            }
        }

        public async Task NotifyStatusChanged(Booking booking, BookingActor respondent, BookingStatus newStatus, string? reason)
        {
            var timeStr = $"{booking.StartTime:g}";

            if (respondent == BookingActor.Coach)
            {
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                {
                    var verb = newStatus == BookingStatus.Accepted ? "bekräftat" : "nekat";
                    _emailService.SendEmailFireAndForget(admin.Email, "Svar på bokning",
                        $"Coach har {verb} bokning {timeStr}.");
                }
            }
            else
            {
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                    {
                        if (newStatus == BookingStatus.Accepted)
                            _emailService.SendEmailFireAndForget(coach.Email, "Bokning bekräftad",
                                $"Din bokning {timeStr} har bekräftats.");
                        else
                            _emailService.SendEmailFireAndForget(coach.Email, "Bokning nekad",
                                $"Din bokning {timeStr} har nekats. Anledning: {reason}");
                    }
                }
            }
        }

        public async Task NotifyCancelled(Booking booking, BookingActor canceller, string? reason)
        {
            var timeStr = $"{booking.StartTime:g}";

            if (canceller == BookingActor.Coach)
            {
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Bokning avbokad",
                        $"Coach har avbokat bokning {timeStr}. Anledning: {reason}");
            }
            else
            {
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Bokning avbokad",
                            $"Din bokning {timeStr} har avbokats. Anledning: {reason}");
                }
            }
        }

        /// <summary>
        /// SCENARIO: Booking transferred to another teacher — notifies coach
        /// CALLS: PUT /api/bookings/{id}/transfer (BookingEndpoints.cs)
        /// SIDE EFFECTS: Sends email to coach about teacher change
        /// </summary>
        public async Task NotifyTransferred(Booking booking, int oldAdminId)
        {
            var oldAdmin = await _db.Users.FindAsync(oldAdminId);
            var newAdmin = await _db.Users.FindAsync(booking.AdminId);
            var oldName = oldAdmin != null ? $"{oldAdmin.FirstName} {oldAdmin.LastName}" : "Okänd";
            var newName = newAdmin != null ? $"{newAdmin.FirstName} {newAdmin.LastName}" : "Okänd";
            var dateStr = booking.StartTime.ToString("yyyy-MM-dd");
            var timeStr = booking.StartTime.ToString("HH:mm");
            var message = $"{newName} kommer att delta på mötet {dateStr} kl.{timeStr} istället för {oldName}.";

            if (booking.CoachId.HasValue)
            {
                var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                if (coach != null)
                    _emailService.SendEmailFireAndForget(coach.Email, "Ändring av mötesdeltagare", message);
            }
        }

        public async Task NotifyRescheduled(Booking booking, BookingActor rescheduler)
        {
            var timeStr = $"{booking.StartTime:g}–{booking.EndTime:t}";

            if (rescheduler == BookingActor.Coach)
            {
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Bokning ombokas",
                        $"Coach begär ombokning till {timeStr}. Logga in för att godkänna.");
            }
            else
            {
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Bokning ombokas",
                            $"Din bokning har ombokats till {timeStr}. Logga in för att godkänna.");
                }
            }
        }
    }
}
