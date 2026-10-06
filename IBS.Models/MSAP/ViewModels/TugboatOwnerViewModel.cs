using IBS.Models.MSAP.MasterFile;

namespace IBS.Models.MSAP.ViewModels
{
    public class TugboatOwnerViewModel : TugboatOwner
    {
        public TugboatOwnerViewModel() { }

        public TugboatOwnerViewModel(TugboatOwner entity)
        {
            TugboatOwnerId = entity.TugboatOwnerId;
            TugboatOwnerNumber = entity.TugboatOwnerNumber;
            TugboatOwnerName = entity.TugboatOwnerName;
            FixedRate = entity.FixedRate;
            MsapRecId = entity.MsapRecId;
        }
    }
}
