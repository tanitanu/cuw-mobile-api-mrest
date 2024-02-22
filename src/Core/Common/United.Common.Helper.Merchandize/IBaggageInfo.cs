using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using Reservation = United.Service.Presentation.ReservationModel.Reservation;

namespace United.Common.Helper.Merchandize
{
    public interface IBaggageInfo
    {
        Task<DOTBaggageInfo> GetBaggageInfo(MOBSHOPReservation reservation);

        Task<DOTBaggageInfo> GetBaggageInfo(MOBPNR pnr);

        Task<DOTBaggageInfo> GetBaggageInfo(Reservation reservation);
    }
}