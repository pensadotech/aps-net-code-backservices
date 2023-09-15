using System.Linq;

namespace TennisBookings.Areas.Admin.Models;

public class BookingListerViewModel
{
	public bool CancelSuccessful { get; set; }

	public IEnumerable<IGrouping<DateTime, CourtBookingViewModel>> CourtBookings { get; set; } = Array.Empty<IGrouping<DateTime, CourtBookingViewModel>>();

	public DateTime EndOfWeek { get; set; }
}
