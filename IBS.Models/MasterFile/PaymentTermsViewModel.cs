namespace IBS.Models.MasterFile
{
    public class PaymentTermsViewModel : Terms
    {
        public PaymentTermsViewModel() { }

        public PaymentTermsViewModel(Terms entity)
        {
            TermsCode = entity.TermsCode;
            NumberOfDays = entity.NumberOfDays;
            NumberOfMonths = entity.NumberOfMonths;
            CreatedBy = entity.CreatedBy;
            CreatedDate = entity.CreatedDate;
            EditedBy = entity.EditedBy;
            EditedDate = entity.EditedDate;
        }
    }
}
