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

        public async Task NotifyBookingCreated(Booking booking, string creatorRole)
        {
            var timeStr = $"{booking.StartTime:g}–{booking.EndTime:t}";

            if (creatorRole == "Admin" || creatorRole == "Teacher")
            {
                // Admin created → notify coach
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Ny mötesförfrågan",
                            $"Du har fått en mötesförfrågan {timeStr}. Logga in för att bekräfta.");
                }
                // Admin created with student → notify student
                if (booking.StudentId.HasValue && booking.CoachId == null)
                {
                    var student = await _db.Users.FindAsync(booking.StudentId.Value);
                    if (student?.EmailNotifications == true)
                        _emailService.SendEmailFireAndForget(student.Email, "Ny bokning",
                            $"Du har blivit inbokad {timeStr}. Mötestyp: {booking.MeetingType}.");
                }
            }
            else if (creatorRole == "Coach")
            {
                // Coach created → notify admin
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Ny mötesförfrågan från coach",
                        $"Du har fått en mötesförfrågan {timeStr}. Logga in för att bekräfta.");
            }
            else if (creatorRole == "Student")
            {
                // Student created → notify admin
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Ny bokningsförfrågan från elev",
                        $"En elev har begärt handledning {timeStr}. Logga in för att bekräfta.");
            }
        }

        public async Task NotifyStatusChanged(Booking booking, string respondentRole, string newStatus, string? reason)
        {
            var timeStr = $"{booking.StartTime:g}";

            if (respondentRole == "Coach")
            {
                // Coach responded → notify admin
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                {
                    var verb = newStatus == "accepted" ? "bekräftat" : "nekat";
                    _emailService.SendEmailFireAndForget(admin.Email, "Svar på bokning",
                        $"Coach har {verb} bokning {timeStr}.");
                }

                // Followup accepted by coach → notify student
                if (newStatus == "accepted" && booking.MeetingType == "followup" && booking.StudentId.HasValue)
                {
                    var student = await _db.Users.FindAsync(booking.StudentId.Value);
                    if (student?.EmailNotifications == true)
                        _emailService.SendEmailFireAndForget(student.Email, "Uppföljningsmöte bekräftat",
                            $"Ett uppföljningsmöte har bokats in {timeStr}.");
                }
            }
            else if (respondentRole == "Student")
            {
                // Student responded to reschedule → notify admin
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                {
                    var verb = newStatus == "accepted" ? "godkänt" : "nekat";
                    _emailService.SendEmailFireAndForget(admin.Email, "Svar på ombokning",
                        $"Elev har {verb} ombokning {timeStr}.");
                }
            }
            else
            {
                // Admin responded → notify coach
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                    {
                        if (newStatus == "accepted")
                            _emailService.SendEmailFireAndForget(coach.Email, "Bokning bekräftad",
                                $"Din bokning {timeStr} har bekräftats.");
                        else
                            _emailService.SendEmailFireAndForget(coach.Email, "Bokning nekad",
                                $"Din bokning {timeStr} har nekats. Anledning: {reason}");
                    }
                }
            }
        }

        public async Task NotifyCancelled(Booking booking, string cancellerRole, string? reason)
        {
            var timeStr = $"{booking.StartTime:g}";

            if (cancellerRole == "Student")
            {
                // Student cancelled → notify admin
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Bokning avbokad av elev",
                        $"En elev har avbokat bokning {timeStr}. Anledning: {reason}");

                // If followup, also notify coach
                if (booking.MeetingType == "followup" && booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Uppföljning avbokad av elev",
                            $"En elev har avbokat uppföljningsmöte {timeStr}. Anledning: {reason}");
                }
            }
            else if (cancellerRole == "Coach")
            {
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Bokning avbokad",
                        $"Coach har avbokat bokning {timeStr}. Anledning: {reason}");
            }
            else
            {
                // Admin cancelled → notify coach
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Bokning avbokad",
                            $"Din bokning {timeStr} har avbokats. Anledning: {reason}");
                }
            }
        }

        public async Task NotifyRescheduled(Booking booking, string reschedulerRole)
        {
            var timeStr = $"{booking.StartTime:g}–{booking.EndTime:t}";

            if (reschedulerRole == "Coach")
            {
                var admin = await _db.Users.FindAsync(booking.AdminId);
                if (admin?.EmailNotifications == true)
                    _emailService.SendEmailFireAndForget(admin.Email, "Bokning ombokas",
                        $"Coach begär ombokning till {timeStr}. Logga in för att godkänna.");
            }
            else if (reschedulerRole == "Admin" || reschedulerRole == "Teacher")
            {
                if (booking.CoachId.HasValue)
                {
                    var coach = await _db.Users.FindAsync(booking.CoachId.Value);
                    if (coach != null)
                        _emailService.SendEmailFireAndForget(coach.Email, "Bokning ombokas",
                            $"Din bokning har ombokats till {timeStr}. Logga in för att godkänna.");
                }
                // If student booking with no coach, notify student
                if (!booking.CoachId.HasValue && booking.StudentId.HasValue)
                {
                    var student = await _db.Users.FindAsync(booking.StudentId.Value);
                    if (student?.EmailNotifications == true)
                        _emailService.SendEmailFireAndForget(student.Email, "Bokning ombokas",
                            $"Din bokning har ombokats till {timeStr}. Logga in för att godkänna.");
                }
            }
        }
    }
}
