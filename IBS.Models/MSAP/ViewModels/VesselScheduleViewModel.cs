using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MSAP.ViewModels
{
    public class VesselScheduleViewModel
    {
        public int VesselScheduleId { get; set; }

        [Required(ErrorMessage = "Please select a customer")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a customer")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Please select a vessel")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a vessel")]
        [Display(Name = "Vessel")]
        public int VesselId { get; set; }

        [Required(ErrorMessage = "Please select a port")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a port")]
        [Display(Name = "Port")]
        public int PortId { get; set; }

        [Required(ErrorMessage = "Please select a terminal")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a terminal")]
        [Display(Name = "Terminal")]
        public int TerminalId { get; set; }

        [Required(ErrorMessage = "Please enter planned start")]
        [Display(Name = "Planned Start")]
        public DateTime PlannedStart { get; set; }

        [Required(ErrorMessage = "Please enter planned end")]
        [Display(Name = "Planned End")]
        public DateTime PlannedEnd { get; set; }

        [Display(Name = "Assigned Tugboats")]
        public List<int>? SelectedTugboatIds { get; set; }

        [Display(Name = "Voyage Number")]
        [StringLength(50)]
        public string? VoyageNumber { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Tentative";

        public string? Notes { get; set; }

        public bool AllowConflicts { get; set; }

        #region ---Select Lists---

        public List<SelectListItem> Vessels { get; set; } = new();

        public List<SelectListItem> Ports { get; set; } = new();

        public List<SelectListItem> Terminals { get; set; } = new();

        public List<SelectListItem> Tugboats { get; set; } = new();

        public List<SelectListItem> Customers { get; set; } = new();

        #endregion
    }
}
