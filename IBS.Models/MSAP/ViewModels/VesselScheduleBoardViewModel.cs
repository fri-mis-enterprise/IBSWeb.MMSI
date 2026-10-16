using IBS.Models.MSAP.MasterFile;

namespace IBS.Models.MSAP.ViewModels
{
    public class VesselScheduleBoardViewModel
    {
        public DateTime Date { get; set; }
        public List<VesselSchedule> Schedules { get; set; } = [];
        public Dictionary<DateTime, HashSet<int>> ConflictsByDay { get; set; } = [];
        public List<Tugboat> Tugboats { get; set; } = [];
    }
}
